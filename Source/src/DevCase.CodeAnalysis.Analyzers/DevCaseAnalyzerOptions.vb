#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics

#End Region

#Region " DevCaseAnalyzerOptions "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Provides typed access to the <c>.editorconfig</c> (or global analyzer configuration) options
    ''' that control the behavior of the DevCase analyzers and code fixes.
    ''' </summary>
    '''
    ''' <example> This is an <c>.editorconfig</c> example.
    ''' <code>
    ''' [*.vb]
    ''' # Enables rule DCA010 (disabled by default because of legacy P/Invoke flag enums).
    ''' devcase_analyzers.dca010_enabled = true
    ''' # Injects ConfigureAwait(continueOnCapturedContext:=False) instead of ConfigureAwait(False).
    ''' devcase_analyzers.configure_await_named_argument = true
    ''' # The match timeout (milliseconds) added by the DCA017 code fix.
    ''' devcase_analyzers.dca017_timeout_ms = 2000
    ''' 
    ''' # The namespace used by the DCA006 code fix (default: the namespace already used in the file, or the assembly name).
    ''' devcase_analyzers.default_namespace = Contoso.Tools
    ''' # The conditional compilation symbol used by the DCA011 code fix.
    ''' devcase_analyzers.dca011_condition = NET8_0_OR_GREATER
    ''' </code>
    ''' </example>
    Public NotInheritable Class DevCaseAnalyzerOptions

#Region " Constants "

        ''' <summary>
        ''' The option key that enables rule <see cref="DiagnosticIds.FlagsEnumWithoutBitShift"/> (default: <see langword="False"/>).
        ''' </summary>
        Public Const FlagsEnumBitShiftEnabledKey As String = "devcase_analyzers.dca010_enabled"

        ''' <summary>
        ''' The option key that makes the <see cref="DiagnosticIds.MissingConfigureAwait"/> code fix
        ''' emit the <c>continueOnCapturedContext:=</c> named argument (default: <see langword="False"/>).
        ''' </summary>
        Public Const ConfigureAwaitNamedArgumentKey As String = "devcase_analyzers.configure_await_named_argument"

        ''' <summary>
        ''' The option key that defines the namespace used by the <see cref="DiagnosticIds.TypeOutsideNamespace"/> code fix. 
        ''' When it is not set, the fix uses the namespace already declared in the file, or else a name derived from the assembly name.
        ''' </summary>
        Public Const DefaultNamespaceKey As String = "devcase_analyzers.default_namespace"

        ''' <summary>
        ''' The option key that defines the conditional compilation symbol used by the 
        ''' <see cref="DiagnosticIds.ThrowHelperMigration"/> code fix (default: <see cref="ThrowHelperConditionValue"/>).
        ''' </summary>
        Public Const ThrowHelperConditionKey As String = "devcase_analyzers.dca011_condition"

        ''' <summary>
        ''' The conditional compilation symbol used by the <see cref="DiagnosticIds.ThrowHelperMigration"/> code fix when no option is configured.
        ''' </summary>
        Public Const ThrowHelperConditionValue As String = "NETCOREAPP"

        ''' <summary>
        ''' The option key that defines, in milliseconds, the match timeout added by the 
        ''' <see cref="DiagnosticIds.RegexWithoutTimeout"/> code fix (default: <see cref="RegexMatchTimeoutValue"/>).
        ''' </summary>
        Public Const RegexMatchTimeoutKey As String = "devcase_analyzers.dca017_timeout_ms"

        ''' <summary>
        ''' The match timeout, in milliseconds, added by the <see cref="DiagnosticIds.RegexWithoutTimeout"/> code fix when no option is configured.
        ''' </summary>
        Public Const RegexMatchTimeoutValue As Integer = 1000

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Prevents a default instance of the <see cref="DevCaseAnalyzerOptions"/> class from being created.
        ''' </summary>
        Private Sub New()
        End Sub

#End Region

#Region " Public Methods "

        ''' <summary>
        ''' Reads a Boolean option for the specified syntax tree.
        ''' </summary>
        '''
        ''' <param name="provider">
        ''' The analyzer configuration options provider.
        ''' </param>
        '''
        ''' <param name="tree">
        ''' The syntax tree whose applicable options are evaluated.
        ''' </param>
        '''
        ''' <param name="key">
        ''' The option key.
        ''' </param>
        '''
        ''' <param name="defaultValue">
        ''' The value returned when the option is absent or cannot be parsed as a Boolean.
        ''' </param>
        '''
        ''' <returns>
        ''' The configured Boolean value, or <paramref name="defaultValue"/>.
        ''' </returns>
        '''
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="provider"/>, <paramref name="tree"/> or <paramref name="key"/> is <see langword="Nothing"/>.
        ''' </exception>
        Public Shared Function GetBoolean(provider As AnalyzerConfigOptionsProvider, tree As SyntaxTree,
                                          key As String, defaultValue As Boolean) As Boolean

            Dim rawValue As String = DevCaseAnalyzerOptions.GetRawValue(provider, tree, key)
            If rawValue Is Nothing Then
                Return defaultValue
            End If

            Dim parsedValue As Boolean
            Return If(Boolean.TryParse(rawValue, parsedValue), parsedValue, defaultValue)
        End Function

        ''' <summary>
        ''' Reads a String option for the specified syntax tree.
        ''' </summary>
        '''
        ''' <param name="provider">
        ''' The analyzer configuration options provider.
        ''' </param>
        '''
        ''' <param name="tree">
        ''' The syntax tree whose applicable options are evaluated.
        ''' </param>
        '''
        ''' <param name="key">
        ''' The option key.
        ''' </param>
        '''
        ''' <param name="defaultValue">
        ''' The value returned when the option is absent or empty.
        ''' </param>
        '''
        ''' <returns>
        ''' The configured (trimmed) value, or <paramref name="defaultValue"/>.
        ''' </returns>
        '''
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="provider"/>, <paramref name="tree"/> or <paramref name="key"/> is <see langword="Nothing"/>.
        ''' </exception>
        Public Shared Function GetString(provider As AnalyzerConfigOptionsProvider, tree As SyntaxTree,
                                         key As String, defaultValue As String) As String

            Dim rawValue As String = DevCaseAnalyzerOptions.GetRawValue(provider, tree, key)
            Return If(String.IsNullOrEmpty(rawValue), defaultValue, rawValue)
        End Function

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Reads the raw (trimmed) text value of an option for the specified syntax tree.
        ''' </summary>
        '''
        ''' <param name="provider">
        ''' The analyzer configuration options provider.
        ''' </param>
        '''
        ''' <param name="tree">
        ''' The syntax tree whose applicable options are evaluated.
        ''' </param>
        '''
        ''' <param name="key">
        ''' The option key.
        ''' </param>
        '''
        ''' <returns>
        ''' The trimmed raw value, or <see langword="Nothing"/> when the option is not defined.
        ''' </returns>
        '''
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="provider"/>, <paramref name="tree"/> or <paramref name="key"/> is <see langword="Nothing"/>.
        ''' </exception>
        Private Shared Function GetRawValue(provider As AnalyzerConfigOptionsProvider, tree As SyntaxTree, key As String) As String

            If provider Is Nothing Then
                Throw New ArgumentNullException(NameOf(provider))
            End If
            If tree Is Nothing Then
                Throw New ArgumentNullException(NameOf(tree))
            End If
            If String.IsNullOrWhiteSpace(key) Then
                Throw New ArgumentNullException(NameOf(key))
            End If

            Dim options As AnalyzerConfigOptions = provider.GetOptions(tree)
            Dim rawValue As String = Nothing
            If options.TryGetValue(key, rawValue) AndAlso rawValue IsNot Nothing Then
                Return rawValue.Trim()
            End If

            ' Global options (e.g. .globalconfig) are also exposed through the provider.
            Return If(provider.GlobalOptions.TryGetValue(key, rawValue) AndAlso rawValue IsNot Nothing, rawValue.Trim(), Nothing)
        End Function

#End Region

    End Class

End Namespace

#End Region
