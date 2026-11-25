#Requires -Version 5.1
[CmdletBinding()]
param(
    [Parameter()]
    [ValidatePattern('^\S+$')]
    [string]$RootSuffix = 'DCARuleTesting',

    [Parameter()]
    [string]$InstanceId = '',

    [Parameter()]
    [switch]$ForceReinstall
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

# ==============================================================================
# Configuration
# ==============================================================================
$SolutionPath  = Join-Path -Path $PSScriptRoot -ChildPath 'DCA Rule Testing Project.slnx'
$RepoRoot      = Split-Path -Path $PSScriptRoot -Parent
$TargetDllName = 'DevCase.CodeAnalysis.Analyzers.dll'
$VsixFilter    = '*DevCase*CodeAnalysis*.vsix'
$VsLocalRoot   = Join-Path -Path $env:LOCALAPPDATA -ChildPath 'Microsoft\VisualStudio'

# ==============================================================================
# Helper Functions
# ==============================================================================
function Test-IsAdministrator {
    $Identity  = [System.Security.Principal.WindowsIdentity]::GetCurrent()
    $Principal = New-Object System.Security.Principal.WindowsPrincipal($Identity)
    return $Principal.IsInRole([System.Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Get-VsWherePath {
    $CandidateRoots = @(${env:ProgramFiles(x86)}, $env:ProgramFiles) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
    foreach ($Root in $CandidateRoots) {
        $Path = Join-Path -Path $Root -ChildPath 'Microsoft Visual Studio\Installer\vswhere.exe'
        if (Test-Path -LiteralPath $Path -PathType Leaf) {
            return $Path
        }
    }
    throw 'vswhere.exe was not found. Ensure Visual Studio Installer is installed on this machine.'
}

function Get-VisualStudioInstance {
    param(
        [Parameter(Mandatory = $true)]
        [string]$VsWherePath,

        [Parameter()]
        [string]$PreferredInstanceId
    )

    $JsonText = (& $VsWherePath -all -prerelease -products * -format json) -join [Environment]::NewLine
    if ([string]::IsNullOrWhiteSpace($JsonText)) {
        throw 'vswhere.exe returned no Visual Studio installations.'
    }

    # Avoid PS 5.1 ConvertFrom-Json double-array nesting bug
    $ParsedJson = $JsonText | ConvertFrom-Json
    if ($ParsedJson -is [System.Array]) {
        $AllInstances = $ParsedJson
    } else {
        $AllInstances = @($ParsedJson)
    }

    $ValidInstances = @(
        foreach ($Inst in $AllInstances) {
            if ($null -eq $Inst) { continue }

            $HasProductPath = $null -ne $Inst.PSObject.Properties['productPath'] -and -not [string]::IsNullOrWhiteSpace($Inst.productPath)
            if (-not $HasProductPath) { continue }

            $DevenvPath = [System.IO.Path]::GetFullPath($Inst.productPath)
            if ($DevenvPath -notmatch '(?i)devenv\.exe$' -or -not (Test-Path -LiteralPath $DevenvPath -PathType Leaf)) {
                continue
            }

            $IdeDir = Split-Path -Path $DevenvPath -Parent
            $VsixInstallerPath = Join-Path -Path $IdeDir -ChildPath 'VSIXInstaller.exe'
            if (-not (Test-Path -LiteralPath $VsixInstallerPath -PathType Leaf)) {
                continue
            }

            $IsPrerelease = ($null -ne $Inst.PSObject.Properties['isPrerelease']) -and [bool]$Inst.isPrerelease

            [PSCustomObject]@{
                InstanceId          = [string]$Inst.instanceId
                DisplayName         = [string]$Inst.displayName
                InstallationVersion = [System.Version]$Inst.installationVersion
                MajorVersion        = ([System.Version]$Inst.installationVersion).Major
                IsPrerelease        = $IsPrerelease
                DevenvPath          = $DevenvPath
                VsixInstallerPath   = $VsixInstallerPath
            }
        }
    )

    if ($ValidInstances.Count -eq 0) {
        throw 'No valid Visual Studio IDE installation (devenv.exe + VSIXInstaller.exe) was found.'
    }

    if (-not [string]::IsNullOrWhiteSpace($PreferredInstanceId)) {
        $Matched = @($ValidInstances | Where-Object { $_.InstanceId -ieq $PreferredInstanceId })
        if ($Matched.Count -eq 0) {
            throw "No Visual Studio installation matches InstanceId '$PreferredInstanceId'."
        }
        return $Matched[0]
    }

    # Prefer highest Visual Studio version; prefer stable over prerelease on tie
    return $ValidInstances |
        Sort-Object -Property @(
            @{ Expression = { $_.InstallationVersion }; Descending = $true },
            @{ Expression = { $_.IsPrerelease };        Descending = $false }
        ) |
        Select-Object -First 1
}

function Get-VsixPackageMetadata {
    param(
        [Parameter(Mandatory = $true)]
        [System.IO.FileInfo]$VsixFile,

        [Parameter(Mandatory = $true)]
        [string]$TargetDllName
    )

    $Archive = [System.IO.Compression.ZipFile]::OpenRead($VsixFile.FullName)
    try {
        $ManifestEntry = $Archive.Entries | Where-Object { $_.FullName -ieq 'extension.vsixmanifest' } | Select-Object -First 1
        if ($null -eq $ManifestEntry) {
            return $null
        }

        $ManifestStream = $ManifestEntry.Open()
        $Reader = New-Object System.IO.StreamReader($ManifestStream)
        try {
            [xml]$ManifestXml = $Reader.ReadToEnd()
        }
        finally {
            $Reader.Dispose()
            $ManifestStream.Dispose()
        }

        $IdentityNode = $ManifestXml.PackageManifest.Metadata.Identity
        $ExtId        = [string]$IdentityNode.Id
        $ExtVerString = [string]$IdentityNode.Version
        if ([string]::IsNullOrWhiteSpace($ExtId) -or [string]::IsNullOrWhiteSpace($ExtVerString)) {
            return $null
        }

        $DllEntry = $Archive.Entries |
            Where-Object { [System.IO.Path]::GetFileName($_.FullName) -ieq $TargetDllName } |
            Select-Object -First 1

        if ($null -eq $DllEntry) {
            return $null
        }

        $DllStream = $DllEntry.Open()
        $Sha256    = [System.Security.Cryptography.SHA256]::Create()
        try {
            $HashBytes = $Sha256.ComputeHash($DllStream)
            $DllHash   = [System.BitConverter]::ToString($HashBytes).Replace('-', '')
        }
        finally {
            $Sha256.Dispose()
            $DllStream.Dispose()
        }

        return [PSCustomObject]@{
            Path             = $VsixFile.FullName
            Id               = $ExtId
            Version          = [System.Version]$ExtVerString
            VersionString    = $ExtVerString
            DllHash          = $DllHash
            LastWriteTimeUtc = $VsixFile.LastWriteTimeUtc
        }
    }
    finally {
        $Archive.Dispose()
    }
}

# ==============================================================================
# 1. Fail-Fast Validation & Dynamic Visual Studio Discovery
# ==============================================================================
if (-not (Test-Path -LiteralPath $SolutionPath -PathType Leaf)) {
    throw "Solution file not found: '$SolutionPath'"
}

$IsAdmin        = Test-IsAdministrator
$VsWherePath    = Get-VsWherePath
$TargetInstance = Get-VisualStudioInstance -VsWherePath $VsWherePath -PreferredInstanceId $InstanceId

$TargetInstanceId    = $TargetInstance.InstanceId
$IdeExePath          = $TargetInstance.DevenvPath
$VsixInstallerExe    = $TargetInstance.VsixInstallerPath
$SuffixVsDirName     = "$($TargetInstance.MajorVersion).0_$TargetInstanceId$RootSuffix"
$SuffixVsDir         = Join-Path -Path $VsLocalRoot -ChildPath $SuffixVsDirName
$SuffixExtensionsDir = Join-Path -Path $SuffixVsDir -ChildPath 'Extensions'
$PrivateRegistryFile = Join-Path -Path $SuffixVsDir -ChildPath 'privateregistry.bin'
$UninstallLogFile    = Join-Path -Path $env:TEMP -ChildPath "VSIXInstaller_${RootSuffix}_Uninstall.log"
$InstallLogFile      = Join-Path -Path $env:TEMP -ChildPath "VSIXInstaller_${RootSuffix}_Install.log"

Write-Host "[+] Target VS instance : $($TargetInstance.MajorVersion).0_$TargetInstanceId ($($TargetInstance.DisplayName))" -ForegroundColor Cyan
Write-Host "[+] Target hive folder : $SuffixVsDir" -ForegroundColor Cyan
Write-Host "[+] Running elevated   : $IsAdmin" -ForegroundColor Cyan

# ==============================================================================
# 2. Discover VSIX Packages (Sort by Manifest Version DESC, then Timestamp DESC)
# ==============================================================================
$VsixFiles = @(Get-ChildItem -LiteralPath $RepoRoot -Filter $VsixFilter -File -Recurse -ErrorAction SilentlyContinue)
if ($VsixFiles.Count -eq 0) {
    throw "No '$VsixFilter' package found under '$RepoRoot'. Build the VSIX project first."
}

$VsixCandidates = @(
    foreach ($File in $VsixFiles) {
        try {
            $Meta = Get-VsixPackageMetadata -VsixFile $File -TargetDllName $TargetDllName
            if ($null -ne $Meta) { $Meta }
        }
        catch {
            Write-Warning "Skipping unreadable VSIX '$($File.FullName)': $_"
        }
    }
)

if ($VsixCandidates.Count -eq 0) {
    throw "No valid VSIX containing '$TargetDllName' was found under '$RepoRoot'."
}

$SelectedVsix = $VsixCandidates |
    Sort-Object -Property @(
        @{ Expression = { $_.Version };          Descending = $true },
        @{ Expression = { $_.LastWriteTimeUtc }; Descending = $true }
    ) |
    Select-Object -First 1

Write-Host "[+] Selected VSIX      : $($SelectedVsix.Path)" -ForegroundColor Cyan
Write-Host "[+] VSIX Identity      : $($SelectedVsix.Id) v$($SelectedVsix.VersionString) (SHA256: $($SelectedVsix.DllHash.Substring(0, 12))...)" -ForegroundColor Cyan

# ==============================================================================
# 3. Initialize Suffixed Hive if Missing
# ==============================================================================
$HiveJustInitialized = $false
if (-not (Test-Path -LiteralPath $PrivateRegistryFile -PathType Leaf)) {
    Write-Host "[+] Initializing hive '$RootSuffix' (/updateconfiguration)..." -ForegroundColor Yellow
    $InitProc = Start-Process -FilePath $IdeExePath `
                              -ArgumentList @('/RootSuffix', $RootSuffix, '/updateconfiguration') `
                              -Wait `
                              -PassThru

    if ($InitProc.ExitCode -ne 0) {
        throw "Initial devenv.exe /updateconfiguration failed with exit code $($InitProc.ExitCode)."
    }
    $HiveJustInitialized = $true
}

# ==============================================================================
# 4. Compare Installed Analyzer DLL Hash vs Selected VSIX DLL Hash
# ==============================================================================
$InstalledDll = $null
$InstalledHash = ''
if (Test-Path -LiteralPath $SuffixExtensionsDir -PathType Container) {
    $InstalledDll = Get-ChildItem -LiteralPath $SuffixExtensionsDir -Filter $TargetDllName -File -Recurse -ErrorAction SilentlyContinue |
        Sort-Object -Property LastWriteTimeUtc -Descending |
        Select-Object -First 1

    if ($null -ne $InstalledDll) {
        $InstalledHash = (Get-FileHash -LiteralPath $InstalledDll.FullName -Algorithm SHA256).Hash
    }
}

$NeedsInstall = $ForceReinstall.IsPresent -or
                $HiveJustInitialized -or
                ($null -eq $InstalledDll) -or
                ($InstalledHash -ine $SelectedVsix.DllHash)

# ==============================================================================
# 5. Clean Uninstall + Reinstall + Optional Admin Registry Patch + Cache Rebuild
# ==============================================================================
if ($NeedsInstall) {
    foreach ($LogPath in @($UninstallLogFile, $InstallLogFile)) {
        if (Test-Path -LiteralPath $LogPath -PathType Leaf) {
            Remove-Item -LiteralPath $LogPath -Force
        }
    }

    # 5a. Uninstall any previous version (or same-version rebuild) from the target hive
    Write-Host "[+] Uninstalling previous VSIX from /rootSuffix:$RootSuffix (if present)..." -ForegroundColor Yellow
    $UninstallArgs = @(
        '/q',
        "/u:`"$($SelectedVsix.Id)`"",
        "/instanceIds:$TargetInstanceId",
        "/rootSuffix:$RootSuffix",
        "/logFile:`"$UninstallLogFile`""
    )
    $UninstallProc = Start-Process -FilePath $VsixInstallerExe `
                                   -ArgumentList $UninstallArgs `
                                   -Wait `
                                   -PassThru

    # Exit codes: 0 = Uninstalled, 1002 = Not installed
    if ($UninstallProc.ExitCode -eq 2003) {
        throw "VSIXInstaller uninstall failed (2003): close all Visual Studio instances running with /RootSuffix $RootSuffix first."
    } elseif ($UninstallProc.ExitCode -ne 0 -and $UninstallProc.ExitCode -ne 1002) {
        throw "VSIXInstaller.exe uninstall failed with exit code $($UninstallProc.ExitCode). Check log: '$UninstallLogFile'"
    }

    # 5b. Install the selected VSIX into the target hive
    Write-Host "[+] Installing VSIX into /instanceIds:$TargetInstanceId /rootSuffix:$RootSuffix..." -ForegroundColor Yellow
    $InstallArgs = @(
        '/q',
        "/instanceIds:$TargetInstanceId",
        "/rootSuffix:$RootSuffix",
        "/logFile:`"$InstallLogFile`"",
        "`"$($SelectedVsix.Path)`""
    )
    $InstallProc = Start-Process -FilePath $VsixInstallerExe `
                                 -ArgumentList $InstallArgs `
                                 -Wait `
                                 -PassThru

    if ($InstallProc.ExitCode -eq 2003) {
        throw "VSIXInstaller install failed (2003): close all Visual Studio instances running with /RootSuffix $RootSuffix first."
    } elseif ($InstallProc.ExitCode -ne 0 -and $InstallProc.ExitCode -ne 1001) {
        throw "VSIXInstaller.exe install failed with exit code $($InstallProc.ExitCode). Check log: '$InstallLogFile'"
    }

    # 5c. Fail-Fast verification on disk (existence + exact SHA-256 match)
    $InstalledDll = Get-ChildItem -LiteralPath $SuffixExtensionsDir -Filter $TargetDllName -File -Recurse -ErrorAction SilentlyContinue |
        Sort-Object -Property LastWriteTimeUtc -Descending |
        Select-Object -First 1

    if ($null -eq $InstalledDll) {
        throw "Post-install verification failed: '$TargetDllName' was not found under '$SuffixExtensionsDir'."
    }

    $VerifiedHash = (Get-FileHash -LiteralPath $InstalledDll.FullName -Algorithm SHA256).Hash
    if ($VerifiedHash -ine $SelectedVsix.DllHash) {
        throw "Post-install hash mismatch. Expected '$($SelectedVsix.DllHash)', found '$VerifiedHash' at '$($InstalledDll.FullName)'."
    }

    $ExtFolder = $InstalledDll.Directory
    while ($null -ne $ExtFolder.Parent -and
           $ExtFolder.Parent.FullName.TrimEnd('\') -ine $SuffixExtensionsDir.TrimEnd('\')) {
        $ExtFolder = $ExtFolder.Parent
    }

    $ExtEntryName       = "$($SelectedVsix.Id),$($SelectedVsix.VersionString)"
    $ExtFolderWithSlash = "$($ExtFolder.FullName.TrimEnd('\'))\"

    Write-Host "[+] Verified on disk   : $ExtEntryName ($ExtFolderWithSlash)" -ForegroundColor Green

    # 5d. Elevated users only: enable per-user extensions in privateregistry.bin
    if ($IsAdmin) {
        $TempHiveName = "VS_Temp_$RootSuffix"
        Write-Host "[+] Elevated session detected: patching privateregistry.bin (EnableAdminExtensions=1)..." -ForegroundColor Yellow

        $LoadOutput = & reg.exe load "HKU\$TempHiveName" "$PrivateRegistryFile" 2>&1
        if ($LASTEXITCODE -ne 0) {
            throw "Failed to load privateregistry.bin (close any running VS instance for '$RootSuffix' first): $LoadOutput"
        }

        try {
            $ExtMgrKey = "HKU\$TempHiveName\Software\Microsoft\VisualStudio\$SuffixVsDirName\ExtensionManager"

            & reg.exe add "$ExtMgrKey" /v EnableAdminExtensions /t REG_DWORD /d 1 /f | Out-Null
            if ($LASTEXITCODE -ne 0) { throw 'Failed to set EnableAdminExtensions=1.' }

            & reg.exe add "$ExtMgrKey\EnabledExtensions" /v "$ExtEntryName" /t REG_SZ /d "$ExtFolderWithSlash" /f | Out-Null
            if ($LASTEXITCODE -ne 0) { throw "Failed to register '$ExtEntryName' in EnabledExtensions." }

            $PreviousEap = $ErrorActionPreference
            $ErrorActionPreference = 'Continue'
            try {
                & reg.exe delete "$ExtMgrKey\DisabledExtensions" /v "$ExtEntryName" /f 2>&1 | Out-Null
                & reg.exe delete "$ExtMgrKey\PendingDeletions" /v "$ExtEntryName" /f 2>&1 | Out-Null
            }
            finally {
                $ErrorActionPreference = $PreviousEap
            }
        }
        finally {
            [System.GC]::Collect()
            [System.GC]::WaitForPendingFinalizers()
            $UnloadOutput = & reg.exe unload "HKU\$TempHiveName" 2>&1
            if ($LASTEXITCODE -ne 0) {
                Write-Warning "Failed to unload temporary hive HKU\$TempHiveName : $UnloadOutput"
            }
        }
    }

    # 5e. Purge MEF/Extension caches and rebuild VS configuration for Roslyn
    $MefCacheDir = Join-Path -Path $SuffixVsDir -ChildPath 'ComponentModelCache'
    if (Test-Path -LiteralPath $MefCacheDir -PathType Container) {
        Remove-Item -LiteralPath $MefCacheDir -Recurse -Force
    }

    Get-ChildItem -LiteralPath $SuffixExtensionsDir -Filter '*.cache' -File -ErrorAction SilentlyContinue |
        Remove-Item -Force

    Write-Host "[+] Rebuilding VS configuration (/updateconfiguration)..." -ForegroundColor Yellow
    $UpdateProc = Start-Process -FilePath $IdeExePath `
                                -ArgumentList @('/RootSuffix', $RootSuffix, '/updateconfiguration') `
                                -Wait `
                                -PassThru

    if ($UpdateProc.ExitCode -ne 0) {
        throw "Post-install devenv.exe /updateconfiguration failed with exit code $($UpdateProc.ExitCode)."
    }
} else {
    Write-Host "[=] Installed analyzer DLL matches selected VSIX (SHA256 identical). Skipping reinstall." -ForegroundColor Green
}

# ==============================================================================
# 6. Launch Visual Studio with /RootSuffix
# ==============================================================================
Write-Host "[+] Launching Visual Studio (/RootSuffix $RootSuffix)..." -ForegroundColor Cyan
Start-Process -FilePath $IdeExePath -ArgumentList @('/RootSuffix', $RootSuffix, "`"$SolutionPath`"")

PAUSE