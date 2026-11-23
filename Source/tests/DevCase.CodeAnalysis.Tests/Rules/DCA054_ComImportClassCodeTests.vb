#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Generic
Imports System.Threading.Tasks

Imports Microsoft.CodeAnalysis.CodeActions

Imports DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers
Imports DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.CodeFixes

Imports Xunit

#End Region

#Region " DCA054_ComImportClassCodeTests "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Tests

    ''' <summary>
    ''' Tests of rule DCA054 (code in a ComImport class never runs) and its code fix.
    ''' </summary>
    Public NotInheritable Class DCA054_ComImportClassCodeTests

#Region " Constants "

        ''' <summary>
        ''' A ComImport class whose constructors are surrounded by preprocessor directives.
        ''' </summary>
        Private Const DirectivesSource As String = "
Imports System.Runtime.InteropServices
<ComImport>
<Guid(""F8383852-FCD3-11d1-A6B9-006097DF5BD4"")>
<ClassInterface(ClassInterfaceType.None)>
Public Class ProgressDialog

#Region "" Fields ""
#End Region

#Region "" Shared Constructors ""

    ''' <summary>
    ''' Initializes the type.
    ''' </summary>
    Shared Sub New()
#If DEBUG Then
        Console.WriteLine(""Debug"")
#End If
    End Sub

#End Region

#Region "" Constructors ""

#If True Then
    ''' <summary>
    ''' Initializes a new instance.
    ''' </summary>
    Public Sub New()
    End Sub
#End If

#End Region

End Class
"

        ''' <summary>
        ''' The expected result of fixing <see cref="DirectivesSource"/>: the constructors are removed, the directives around them stay.
        ''' </summary>
        Private Const DirectivesFixedSource As String = "
Imports System.Runtime.InteropServices
<ComImport>
<Guid(""F8383852-FCD3-11d1-A6B9-006097DF5BD4"")>
<ClassInterface(ClassInterfaceType.None)>
Public Class ProgressDialog

#Region "" Fields ""
#End Region

#Region "" Shared Constructors ""

#End Region

#Region "" Constructors ""

#If True Then
#End If

#End Region

End Class
"

#End Region

#Region " Fields "

        ''' <summary>
        ''' The analyzer under test.
        ''' </summary>
        Private Shared ReadOnly Analyzer As New ComImportClassCodeAnalyzer()

        ''' <summary>
        ''' The code fix provider under test.
        ''' </summary>
        Private Shared ReadOnly Provider As New ComImportClassCodeCodeFixProvider()

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="DCA054_ComImportClassCodeTests"/> class.
        ''' </summary>
        Public Sub New()
        End Sub

#End Region

#Region " Test Methods "

        ''' <summary>
        ''' Verifies that rule DCA054 reports constructors (instance and Shared), methods, operators, properties (also auto-implemented), 
        ''' custom events and field initializers of a ComImport class, on modern .NET and on .NET Framework.
        ''' </summary>
        '''
        ''' <param name="targetNetFramework">
        ''' <see langword="True"/> to compile against .NET Framework 4.8.
        ''' </param>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Theory>
        <InlineData(False)>
        <InlineData(True)>
        Public Async Function Reports_CodeInComImportClass(targetNetFramework As Boolean) As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Imports System.Runtime.InteropServices
<ComImport>
<Guid(""F8383852-FCD3-11d1-A6B9-006097DF5BD4"")>
<ClassInterface(ClassInterfaceType.None)>
Public Class ProgressDialog
    Private [|counter|] As Integer = 1
    Private ReadOnly [|items|] As New List(Of Integer)
    Private plain As Integer
    Shared Sub [|New|]()
    End Sub
    Public Sub [|New|]()
        Console.WriteLine(""never runs"")
    End Sub
    Public Sub [|Start|]()
    End Sub
    Public Shared Function [|Create|]() As Integer
        Return 1
    End Function
    Public Property [|Title|] As String
    Public ReadOnly Property [|Count|] As Integer
        Get
            Return Me.counter + Me.items.Count + Me.plain
        End Get
    End Property
    Public Shared Operator [|+|](a As ProgressDialog, b As ProgressDialog) As ProgressDialog
        Return a
    End Operator
    Public Custom Event [|Changed|] As EventHandler
        AddHandler(value As EventHandler)
        End AddHandler
        RemoveHandler(value As EventHandler)
        End RemoveHandler
        RaiseEvent(sender As Object, e As EventArgs)
        End RaiseEvent
    End Event
End Class
", targetNetFramework:=targetNetFramework).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that rule DCA054 does not report empty ComImport classes, ComImport interfaces, 
        ''' or classes without the attribute.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function DoesNotReport_OtherTypes() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Imports System.Runtime.InteropServices
<ComImport>
<Guid(""F8383852-FCD3-11d1-A6B9-006097DF5BD4"")>
<ClassInterface(ClassInterfaceType.None)>
Public Class ProgressDialog
End Class
<ComImport>
<Guid(""EBBC7C04-315E-11d2-B62F-006097DF5BD4"")>
<InterfaceType(ComInterfaceType.InterfaceIsIUnknown)>
Public Interface IProgressDialog
    Sub StartProgressDialog(hwndParent As IntPtr)
End Interface
<Guid(""F8383852-FCD3-11d1-A6B9-006097DF5BD5"")>
Public Class NotImported
    Public Sub New()
    End Sub
    Public Sub Start()
    End Sub
End Class
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA054 removes a public parameterless constructor and a Shared constructor 
        ''' (with their comments), and that the result compiles.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_RemovesRemovableConstructors() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
Imports System.Runtime.InteropServices
<ComImport>
<Guid(""F8383852-FCD3-11d1-A6B9-006097DF5BD4"")>
<ClassInterface(ClassInterfaceType.None)>
Public Class ProgressDialog

    ''' <summary>
    ''' Initializes the type.
    ''' </summary>
    Shared Sub New()
    End Sub

    ''' <summary>
    ''' Initializes a new instance.
    ''' </summary>
    Public Sub New()
    End Sub

End Class
", "
Imports System.Runtime.InteropServices
<ComImport>
<Guid(""F8383852-FCD3-11d1-A6B9-006097DF5BD4"")>
<ClassInterface(ClassInterfaceType.None)>
Public Class ProgressDialog

End Class
", targetNetFramework:=True).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA054 is not offered for members whose removal would change the class: 
        ''' constructors with parameters or another accessibility, constructors of MustInherit classes, and methods.
        ''' </summary>
        '''
        ''' <param name="member">
        ''' The member declared in the ComImport class.
        ''' </param>
        '''
        ''' <param name="modifiers">
        ''' The modifiers of the class.
        ''' </param>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Theory>
        <InlineData("Private Sub New()" & vbLf & "    End Sub", "Public")>
        <InlineData("Public Sub New(handle As IntPtr)" & vbLf & "    End Sub", "Public")>
        <InlineData("Public Sub New()" & vbLf & "    End Sub", "Public MustInherit")>
        <InlineData("Public Sub Start()" & vbLf & "    End Sub", "Public")>
        Public Async Function NoFix_ForMembersThatChangeTheClass(member As String, modifiers As String) As Task
            Dim actions As List(Of CodeAction) = Await AnalyzerTestHost.GetRegisteredActionsAsync(Analyzer, Provider, $"
<Runtime.InteropServices.ComImport>
<Runtime.InteropServices.Guid(""F8383852-FCD3-11d1-A6B9-006097DF5BD4"")>
{modifiers} Class ProgressDialog
    {member}
End Class
").ConfigureAwait(True)
            Assert.Empty(actions)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA054 keeps the region of the removed constructor (the case of the <c>DCA054.vb</c> 
        ''' file of the <c>RuleTesting</c> solution): the region stays, empty, instead of losing its <c>#Region</c> line.
        ''' </summary>
        '''
        ''' <param name="targetNetFramework">
        ''' <see langword="True"/> to compile against the .NET Framework 4.8 reference assemblies.
        ''' </param>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Theory>
        <InlineData(True)>
        <InlineData(False)>
        Public Async Function Fix_KeepsTheRegionOfTheConstructor(targetNetFramework As Boolean) As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
Imports System.Runtime.InteropServices

Namespace DCA_Rule_Testing

    ''' <summary>
    ''' DCA054: Code in a ComImport class never runs.
    ''' </summary>
    <ComImport>
    <Guid(""F8383852-FCD3-11D1-A6B9-006097DF5BD4"")>
    <ClassInterface(ClassInterfaceType.None)>
    Public Class DCA054

#Region "" Constructors ""

        ''' <summary>
        ''' Initializes a new instance of the <see cref=""DCA054""/> class.
        ''' </summary>
        Public Sub New() ' DCA054

            Console.WriteLine(""This code never runs."")
        End Sub

#End Region

    End Class

End Namespace
", "
Imports System.Runtime.InteropServices

Namespace DCA_Rule_Testing

    ''' <summary>
    ''' DCA054: Code in a ComImport class never runs.
    ''' </summary>
    <ComImport>
    <Guid(""F8383852-FCD3-11D1-A6B9-006097DF5BD4"")>
    <ClassInterface(ClassInterfaceType.None)>
    Public Class DCA054

#Region "" Constructors ""

#End Region

    End Class

End Namespace
", targetNetFramework:=targetNetFramework).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA054, applied to each constructor, keeps the preprocessor directives around them: 
        ''' a complete region before the constructor, the region of each constructor, and an <c>#If</c> block around the 
        ''' constructor. The <c>#If</c> block inside the body of the constructor is removed with it.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_KeepsTheDirectivesAroundTheConstructors() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, DCA054_ComImportClassCodeTests.DirectivesSource,
                                                      DCA054_ComImportClassCodeTests.DirectivesFixedSource, targetNetFramework:=True).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the "Fix All" action of rule DCA054 (all the constructors at once) keeps the same preprocessor directives 
        ''' as the code fix applied to each constructor.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function FixAll_KeepsTheDirectivesAroundTheConstructors() As Task
            Dim fixedText As String = Await AnalyzerTestHost.ApplyFixAllInDocumentAsync(Analyzer, Provider, DCA054_ComImportClassCodeTests.DirectivesSource,
                                                                                        NameOf(ComImportClassCodeCodeFixProvider), targetNetFramework:=True).ConfigureAwait(True)
            AnalyzerTestHost.AssertTextEqual(DCA054_ComImportClassCodeTests.DirectivesFixedSource, fixedText)
        End Function

#End Region

    End Class

End Namespace

#End Region
