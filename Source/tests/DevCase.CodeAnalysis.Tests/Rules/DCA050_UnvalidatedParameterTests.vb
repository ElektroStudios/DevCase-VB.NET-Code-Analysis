#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Threading
Imports System.Threading.Tasks

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeFixes

Imports DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers
Imports DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.CodeFixes

Imports Xunit

#End Region

#Region " DCA050_UnvalidatedParameterTests "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Tests

    ''' <summary>
    ''' Tests of rule DCA050 (parameter of an externally visible method is not validated) and its code fixes.
    ''' </summary>
    Public NotInheritable Class DCA050_UnvalidatedParameterTests

#Region " Fields "

        ''' <summary>
        ''' The analyzer under test.
        ''' </summary>
        Private Shared ReadOnly Analyzer As New UnvalidatedParameterAnalyzer()

        ''' <summary>
        ''' The code fix provider under test.
        ''' </summary>
        Private Shared ReadOnly Provider As New UnvalidatedParameterCodeFixProvider()

        ''' <summary>
        ''' The declarations used by the sources of the tests.
        ''' </summary>
        Private Const Declarations As String = "
Public Enum RegistryScope
    CurrentUser
    LocalMachine
End Enum
Public Enum BigScope As Long
    Small
    Big = 5000000000
End Enum
<Flags>
Public Enum Options
    None = 0
    First = 1 << 0
End Enum
Friend NotInheritable Class UtilEnvironmentPath
    Friend Shared Function GetPathData(scope As RegistryScope) As String()
        Return {scope.ToString()}
    End Function
End Class
"

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="DCA050_UnvalidatedParameterTests"/> class.
        ''' </summary>
        Public Sub New()
        End Sub

#End Region

#Region " Test Methods "

        ''' <summary>
        ''' Verifies that rule DCA050 reports unvalidated Enum, String and reference parameters of public and protected methods and constructors.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Reports_UnvalidatedParameters() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, $"{Declarations}
Public Class C
    Public Sub New([|items|] As IList(Of String))
        Me.Count = items.Count
    End Sub
    Public ReadOnly Property Count As Integer
    Public Shared Function ContainsDirectory([|scope|] As RegistryScope,
                                             [|directory|] As String) As Boolean

        Return Array.Exists(UtilEnvironmentPath.GetPathData(scope), Function(dir As String) dir.Equals(directory, StringComparison.OrdinalIgnoreCase))

    End Function
    Protected Sub Process([|action|] As Action, [|values|] As Integer(), [|big|] As BigScope)
        action()
        Console.WriteLine(values.Length.ToString() & big.ToString())
    End Sub
    Public Function Convert(Of T As Class)([|value|] As T) As String
        Return value.ToString()
    End Function
    Public Sub Forward([|text|] As String)
        C.Write(text)
    End Sub
    Private Shared Sub Write(value As String)
        Console.WriteLine(value.Trim())
    End Sub
End Class
Public MustInherit Class Base
    Public MustOverride Sub Run(text As String)
End Class
Public Class Derived : Inherits Base
    Public Overrides Sub Run([|text|] As String)
        Console.WriteLine(text.Length)
    End Sub
End Class
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that rule DCA050 reports a parameter assigned to a framework property whose setter rejects <see langword="Nothing"/> 
        ''' (the reported <c>SetCurrentCulture</c> example, and <c>Console.Title</c>), and not a parameter assigned to a property declared 
        ''' in source or to a framework property that accepts <see langword="Nothing"/> (<c>[AllowNull]</c>, nullable, or without information).
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
        Public Async Function Reports_AssignmentToNullRejectingFrameworkProperty(targetNetFramework As Boolean) As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Imports System.Globalization
Imports System.Threading
Public NotInheritable Class UtilCulture
    Public Shared Sub SetCurrentCulture([|ci|] As CultureInfo)

        Thread.CurrentThread.CurrentCulture = ci
        Thread.CurrentThread.CurrentUICulture = ci
    End Sub
    Public Shared Sub SetTitle([|title|] As String)
        Console.Title = title
    End Sub
    Public Shared Sub Configure([|writer|] As IO.TextWriter, newLine As String, [|ex|] As Exception, link As String, [|holder|] As Holder, value As Object)
        writer.NewLine = newLine
        ex.HelpLink = link
        holder.Value = value
        Dim other As New Holder With {.Value = value}
    End Sub
End Class
Public NotInheritable Class Holder
    Public Property Value As Object
End Class
", targetNetFramework:=targetNetFramework).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that rule DCA050 does not report validated, optional, ByRef, ParamArray, value-type, nullable, Flags,
        ''' unused or event parameters, nor parameters of non-visible methods, event handlers, comparison methods, entry points,
        ''' constructors that pass the parameter to MyBase.New, and methods that only throw.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function DoesNotReport_ValidatedOrExcludedParameters() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, $"{Declarations}
Public Class Base
    Protected Sub New(name As String)
        If name Is Nothing Then
            Throw New ArgumentNullException(NameOf(name))
        End If
    End Sub
End Class
Public Class C : Inherits Base
    Public Sub New(name As String)
        MyBase.New(name)
        Console.WriteLine(name.Length)
    End Sub
    Public Sub Validated(a As String, b As Object, c As RegistryScope, d As String, e As Object, f As String, g As Object, h As Object, i As String)
        If String.IsNullOrEmpty(a) Then
            Throw New ArgumentException(""Empty."", NameOf(a))
        End If
        ArgumentNullException.ThrowIfNull(b)
        Select Case c
            Case RegistryScope.CurrentUser, RegistryScope.LocalMachine
            Case Else
                Throw New ArgumentOutOfRangeException(NameOf(c))
        End Select
        Console.WriteLine(d?.Length)
        Console.WriteLine(If(e, 0).ToString() & f & g.ToString() & h.ToString() & i)
        If f Is Nothing OrElse TypeOf g Is String Then
            Return
        End If
        Debug.Assert(h IsNot Nothing)
        While i.Length > 0
            i = i.Substring(1)
        End While
    End Sub
    Public Sub Excluded(Optional a As String = Nothing, Optional b As RegistryScope = RegistryScope.CurrentUser)
        Console.WriteLine(a & b.ToString())
    End Sub
    Public Sub Excluded2(ByRef a As String, number As Integer, nullable As Integer?, flags As Options, ParamArray values As String())
        a = number.ToString() & nullable.ToString() & flags.ToString() & values.Length.ToString()
    End Sub
    Public Sub Unused(a As String, b As Object)
        Console.WriteLine()
    End Sub
    Public Function NotConsumed(a As String, b As Object, c As String) As String
        Me.Stored = b
        Return If(c = ""x"", a, $""{{a}}-{{c}}"" & a)
    End Function
    Public Property Stored As Object
    Public Function DelegatedToHelper(provider As IServiceProvider, key As String) As Object
        Return C.GetService(provider, key)
    End Function
    Private Shared Function GetService(provider As IServiceProvider, key As String) As Object
        If provider Is Nothing Then
            Throw New ArgumentNullException(NameOf(provider))
        End If
        ArgumentException.ThrowIfNullOrEmpty(key)
        Return provider.GetService(Type.GetType(key))
    End Function
    Public Sub OnClick(sender As Object, e As EventArgs)
        Console.WriteLine(sender.ToString() & e.ToString())
    End Sub
    Protected Overridable Sub OnChanged(e As EventArgs)
        Console.WriteLine(e.ToString())
    End Sub
    Public Overrides Function Equals(obj As Object) As Boolean
        Return MyBase.Equals(obj)
    End Function
    Public Overrides Function GetHashCode() As Integer
        Return 0
    End Function
    Public Shared Sub Main(args As String())
        Console.WriteLine(args.Length)
    End Sub
    Public Function NotImplemented(a As String) As String
        Throw New NotImplementedException()
    End Function
    Public Function Generic(Of T)(value As T) As String
        Return value.ToString()
    End Function
    Friend Sub Internal(a As String)
        Console.WriteLine(a.Length)
    End Sub
    Private Sub Hidden(a As String)
        Console.WriteLine(a.Length)
    End Sub
End Class
Public Class Converter : Inherits ComponentModel.TypeConverter
    Public Overrides Function CanConvertFrom(context As ComponentModel.ITypeDescriptorContext, sourceType As Type) As Boolean
        Return context.Container.Components.Count > 0 AndAlso sourceType.Name.Length > 0
    End Function
End Class
Public Class Formattable : Implements IFormattable
    Public Function Format(formatText As String, formatProvider As IFormatProvider) As String Implements IFormattable.ToString
        Return formatText.Trim() & formatProvider.ToString()
    End Function
End Class
Public NotInheritable Class Sealed
    Protected Sub Run(a As String)
        Console.WriteLine(a.Length)
    End Sub
End Class
Friend Class Internal
    Public Sub Run(a As String)
        Console.WriteLine(a.Length)
    End Sub
End Class
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies the default code fixes of rule DCA050 on the example of the rule documentation: <c>[Enum].IsDefined</c> with
        ''' <c>InvalidEnumArgumentException</c> for the Enum parameter, and <c>String.IsNullOrEmpty</c> for the String parameter,
        ''' in parameter order; the result compiles on .NET Framework.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_EnumAndString_Defaults() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, $"Imports System.ComponentModel
{Declarations}
Public NotInheritable Class UtilPaths
    <DebuggerStepThrough>
    Public Shared Function ContainsDirectory(scope As RegistryScope,
                                             directory As String) As Boolean

        Return Array.Exists(UtilEnvironmentPath.GetPathData(scope), Function(dir As String) dir.Equals(directory, StringComparison.OrdinalIgnoreCase))

    End Function
End Class
", $"Imports System.ComponentModel
{Declarations}
Public NotInheritable Class UtilPaths
    <DebuggerStepThrough>
    Public Shared Function ContainsDirectory(scope As RegistryScope,
                                             directory As String) As Boolean

        If Not [Enum].IsDefined(GetType(RegistryScope), scope) Then
            Throw New InvalidEnumArgumentException(NameOf(scope), scope, GetType(RegistryScope))
        End If
        If String.IsNullOrEmpty(directory) Then
            Throw New ArgumentNullException(NameOf(directory), $""The {{NameOf(directory)}} cannot be null or empty."")
        End If

        Return Array.Exists(UtilEnvironmentPath.GetPathData(scope), Function(dir As String) dir.Equals(directory, StringComparison.OrdinalIgnoreCase))

    End Function
End Class
", targetNetFramework:=True).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA050 qualifies InvalidEnumArgumentException when its namespace is not imported,
        ''' and uses ArgumentOutOfRangeException for an Enum whose values do not fit in an Integer.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_Enum_QualifiedName_And_LongEnum() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, $"{Declarations}
Public Class C
    Public Function Run(scope As RegistryScope, big As BigScope) As String
        Return scope.ToString() & big.ToString()
    End Function
End Class
", $"{Declarations}
Public Class C
    Public Function Run(scope As RegistryScope, big As BigScope) As String
        If Not [Enum].IsDefined(GetType(RegistryScope), scope) Then
            Throw New ComponentModel.InvalidEnumArgumentException(NameOf(scope), scope, GetType(RegistryScope))
        End If
        If Not [Enum].IsDefined(GetType(BigScope), big) Then
            Throw New ArgumentOutOfRangeException(NameOf(big), big, $""The value is not defined in the {{NameOf(BigScope)}} enumeration."")
        End If

        Return scope.ToString() & big.ToString()
    End Function
End Class
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies the code fixes of rule DCA050 for a String parameter: <c>If</c> blocks on .NET Framework, and the throw helpers 
        ''' (<c>ArgumentException.ThrowIfNullOrEmpty</c>, <c>ThrowIfNullOrWhiteSpace</c>, <c>ArgumentNullException.ThrowIfNull</c>) on modern .NET.
        ''' </summary>
        '''
        ''' <param name="variant">
        ''' The guard clause variant.
        ''' </param>
        '''
        ''' <param name="targetNetFramework">
        ''' <see langword="True"/> to compile against .NET Framework 4.8.
        ''' </param>
        '''
        ''' <param name="expectedGuard">
        ''' The expected guard clause.
        ''' </param>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Theory>
        <InlineData(UnvalidatedParameterCodeFixProvider.NullOrEmptyVariant, False, "        ArgumentException.ThrowIfNullOrEmpty(path)")>
        <InlineData(UnvalidatedParameterCodeFixProvider.NullOrWhiteSpaceVariant, False, "        ArgumentException.ThrowIfNullOrWhiteSpace(path)")>
        <InlineData(UnvalidatedParameterCodeFixProvider.NothingVariant, False, "        ArgumentNullException.ThrowIfNull(path)")>
        <InlineData(UnvalidatedParameterCodeFixProvider.NullOrEmptyVariant, True, "        If String.IsNullOrEmpty(path) Then
            Throw New ArgumentNullException(NameOf(path), $""The {NameOf(path)} cannot be null or empty."")
        End If")>
        <InlineData(UnvalidatedParameterCodeFixProvider.NullOrWhiteSpaceVariant, True, "        If String.IsNullOrWhiteSpace(path) Then
            Throw New ArgumentNullException(NameOf(path), $""The {NameOf(path)} cannot be null, empty or white space."")
        End If")>
        <InlineData(UnvalidatedParameterCodeFixProvider.NothingVariant, True, "        If path Is Nothing Then
            Throw New ArgumentNullException(NameOf(path))
        End If")>
        Public Async Function Fix_String_Variants([variant] As String, targetNetFramework As Boolean, expectedGuard As String) As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
Public Class C
    Public Function Run(path As String) As Integer
        ' Counts the characters.
        Return path.Length
    End Function
End Class
", $"
Public Class C
    Public Function Run(path As String) As Integer
{expectedGuard}

        ' Counts the characters.
        Return path.Length
    End Function
End Class
", equivalenceKey:=$"{NameOf(UnvalidatedParameterCodeFixProvider)}.{[variant]}", targetNetFramework:=targetNetFramework).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA050 writes the guard clause after the MyBase.New call of a constructor, and keeps escaped names.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_Constructor_AfterMyBaseNew() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
Public Class Base
    Protected Sub New(count As Integer)
    End Sub
End Class
Public Class C : Inherits Base
    Public Sub New(count As Integer, [error] As Exception)
        MyBase.New(count)
        Me.Message = [error].Message
    End Sub
    Public ReadOnly Property Message As String
End Class
", "
Public Class Base
    Protected Sub New(count As Integer)
    End Sub
End Class
Public Class C : Inherits Base
    Public Sub New(count As Integer, [error] As Exception)
        MyBase.New(count)
        If [error] Is Nothing Then
            Throw New ArgumentNullException(NameOf([error]))
        End If

        Me.Message = [error].Message
    End Sub
    Public ReadOnly Property Message As String
End Class
", targetNetFramework:=True).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that Fix All of rule DCA050 inserts the guard clauses among the existing ones, in parameter order, in several methods (.NET Framework: <c>If</c> blocks).
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function FixAll_KeepsParameterOrder() As Task
            Dim fixedText As String = Await AnalyzerTestHost.ApplyFixAllInDocumentAsync(Analyzer, Provider, "
Public Class C
    Public Sub Run(a As String, b As Object, c As Object)
        Me.ThrowIfDisposed()
        If b Is Nothing Then
            Throw New ArgumentNullException(NameOf(b))
        End If

        Console.WriteLine(a.Length.ToString() & b.ToString() & c.ToString())
    End Sub
    Public Sub Other(x As Object, y As Object)
        Console.WriteLine(x.ToString() & y.ToString())
    End Sub
    Private Sub ThrowIfDisposed()
    End Sub
End Class
", $"{NameOf(UnvalidatedParameterCodeFixProvider)}.{UnvalidatedParameterCodeFixProvider.NothingVariant}", targetNetFramework:=True).ConfigureAwait(True)

            AnalyzerTestHost.AssertTextEqual("
Public Class C
    Public Sub Run(a As String, b As Object, c As Object)
        Me.ThrowIfDisposed()
        If a Is Nothing Then
            Throw New ArgumentNullException(NameOf(a))
        End If
        If b Is Nothing Then
            Throw New ArgumentNullException(NameOf(b))
        End If
        If c Is Nothing Then
            Throw New ArgumentNullException(NameOf(c))
        End If

        Console.WriteLine(a.Length.ToString() & b.ToString() & c.ToString())
    End Sub
    Public Sub Other(x As Object, y As Object)
        If x Is Nothing Then
            Throw New ArgumentNullException(NameOf(x))
        End If
        If y Is Nothing Then
            Throw New ArgumentNullException(NameOf(y))
        End If

        Console.WriteLine(x.ToString() & y.ToString())
    End Sub
    Private Sub ThrowIfDisposed()
    End Sub
End Class
", fixedText)
        End Function

        ''' <summary>
        ''' Verifies that Fix All of rule DCA050 writes <c>ArgumentNullException.ThrowIfNull</c> on modern .NET, 
        ''' in parameter order (the reported <c>MailTo</c> example).
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function FixAll_ModernNet_UsesThrowHelpers() As Task
            Dim fixedText As String = Await AnalyzerTestHost.ApplyFixAllInDocumentAsync(Analyzer, Provider, "
Public NotInheritable Class UtilMail
    Public Shared Sub MailTo(addresses As String(),
                             subject As String,
                             body As String)

        Dim mailTo As String =
                $""MailTo:{String.Join("";""c, addresses).Trim("" ""c)}?subject={ _
                subject.Replace(Environment.NewLine, ""%20"", StringComparison.Ordinal)};&body={body.Replace(Environment.NewLine, ""%0A"", StringComparison.Ordinal)}""

        Diagnostics.Process.Start(mailTo)

    End Sub
End Class
", $"{NameOf(UnvalidatedParameterCodeFixProvider)}.{UnvalidatedParameterCodeFixProvider.NothingVariant}").ConfigureAwait(True)

            AnalyzerTestHost.AssertTextEqual("
Public NotInheritable Class UtilMail
    Public Shared Sub MailTo(addresses As String(),
                             subject As String,
                             body As String)

        ArgumentNullException.ThrowIfNull(addresses)
        ArgumentNullException.ThrowIfNull(subject)
        ArgumentNullException.ThrowIfNull(body)

        Dim mailTo As String =
                $""MailTo:{String.Join("";""c, addresses).Trim("" ""c)}?subject={ _
                subject.Replace(Environment.NewLine, ""%20"", StringComparison.Ordinal)};&body={body.Replace(Environment.NewLine, ""%0A"", StringComparison.Ordinal)}""

        Diagnostics.Process.Start(mailTo)

    End Sub
End Class
", fixedText)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA050 writes an <c>If</c> block, not a throw helper, when the document is also 
        ''' compiled by a .NET Framework project (multi-targeted project or linked file), where the helper does not exist.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_LinkedNetFrameworkProject_UsesIfBlock() As Task
            Dim project As Project = AnalyzerTestHost.CreateProject({New TestSource("Shared.vb", "
Public Class C
    Public Function Run(value As Object) As String
        Return value.ToString()
    End Function
End Class
")}, linkedNetFrameworkProject:=True)
            Assert.Equal(2, project.Solution.ProjectIds.Count)

            Dim diagnostics As ImmutableArray(Of Diagnostic) = Await AnalyzerTestHost.GetAnalyzerDiagnosticsAsync(project, Analyzer).ConfigureAwait(True)
            Dim document As Document = project.GetDocument(Assert.Single(diagnostics).Location.SourceTree)
            Dim actions As New List(Of CodeAction)()
            Await Provider.RegisterCodeFixesAsync(New CodeFixContext(document, diagnostics(0),
                                                                     Sub(a As CodeAction, d As ImmutableArray(Of Diagnostic)) actions.Add(a),
                                                                     CancellationToken.None)).ConfigureAwait(True)
            Dim operations As ImmutableArray(Of CodeActionOperation) = Await actions(0).GetOperationsAsync(CancellationToken.None).ConfigureAwait(True)
            Dim changed As Solution = operations.OfType(Of ApplyChangesOperation)().Single().ChangedSolution
            Dim text As String = (Await changed.GetDocument(document.Id).GetTextAsync(CancellationToken.None).ConfigureAwait(True)).ToString()

            Assert.Contains("If value Is Nothing Then", text, StringComparison.Ordinal)
            Assert.DoesNotContain("ThrowIfNull", text, StringComparison.Ordinal)
        End Function

        ''' <summary>
        ''' Verifies that rule DCA050 does not report parameters passed only to framework methods that accept <see langword="Nothing"/> 
        ''' (nullable annotations on modern .NET, or the known methods on .NET Framework), nor <c>ReferenceEquals</c> methods.
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
        Public Async Function DoesNotReport_ArgumentsThatAcceptNothing(targetNetFramework As Boolean) As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Imports System.Collections.Generic
Imports System.Text
Public Class C
    <ComponentModel.EditorBrowsable(ComponentModel.EditorBrowsableState.Never)>
    <DebuggerNonUserCode>
    Public Shared Shadows Function ReferenceEquals(objA As Object, objB As Object) As Boolean
        Return Object.ReferenceEquals(objA, objB)
    End Function
    Public Shared Function Same(a As Object, b As Object) As Boolean
        Return Object.ReferenceEquals(a, b) OrElse Object.Equals(a, b)
    End Function
    Public Sub Log(message As String, value As Object)
        Dim items As New List(Of String)()
        Dim builder As New StringBuilder()
        Console.WriteLine(message)
        Debug.WriteLine(value)
        items.Add(message)
        builder.Append(message).AppendLine(Strings.Trim(message))
    End Sub
End Class
", targetNetFramework:=targetNetFramework).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies the code actions offered by rule DCA050 for each kind of parameter, the default one first.
        ''' </summary>
        '''
        ''' <param name="parameter">
        ''' The declaration of the single parameter of the method.
        ''' </param>
        '''
        ''' <param name="expectedTitles">
        ''' The expected titles, separated by '|'.
        ''' </param>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Theory>
        <InlineData("value As RegistryScope", "Throw InvalidEnumArgumentException if 'value' is not a defined value")>
        <InlineData("value As BigScope", "Throw ArgumentOutOfRangeException if 'value' is not a defined value")>
        <InlineData("value As String", "Throw ArgumentNullException if 'value' is Nothing or empty|Throw ArgumentNullException if 'value' is Nothing, empty or white space|Throw ArgumentNullException if 'value' is Nothing")>
        <InlineData("value As Object", "Throw ArgumentNullException if 'value' is Nothing")>
        Public Async Function OffersOneActionPerGuardVariant(parameter As String, expectedTitles As String) As Task
            Dim actions As List(Of CodeAction) = Await AnalyzerTestHost.GetRegisteredActionsAsync(Analyzer, Provider, $"{Declarations}
Public Class C
    Public Function Run({parameter}) As String
        Return value.ToString()
    End Function
End Class
").ConfigureAwait(True)

            Assert.Equal(expectedTitles.Split("|"c), actions.Select(Function(a As CodeAction) a.Title), StringComparer.Ordinal)
        End Function

#End Region

    End Class

End Namespace

#End Region
