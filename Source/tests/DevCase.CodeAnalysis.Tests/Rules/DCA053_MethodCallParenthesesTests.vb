#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Threading.Tasks

Imports DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers
Imports DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.CodeFixes

Imports Xunit

#End Region

#Region " DCA053_MethodCallParenthesesTests "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Tests

    ''' <summary>
    ''' Tests of rule DCA053 (method call without parentheses) and its code fix.
    ''' </summary>
    Public NotInheritable Class DCA053_MethodCallParenthesesTests

#Region " Fields "

        ''' <summary>
        ''' The analyzer under test.
        ''' </summary>
        Private Shared ReadOnly Analyzer As New MethodCallParenthesesAnalyzer()

        ''' <summary>
        ''' The code fix provider under test.
        ''' </summary>
        Private Shared ReadOnly Provider As New MethodCallParenthesesCodeFixProvider()

        ''' <summary>
        ''' A source with every reported form.
        ''' </summary>
        Private Const ReportedSource As String = "
Imports System.Linq
Public Class C
    Public Async Function Run(items As List(Of Integer), text As String, values As IEnumerable(Of Object), addresses As IEnumerable(Of String)) As Task(Of Integer())
        Me.[|Work|]
        [|Work|]
        Call [|Work|]
        Dim s As String = text.[|Trim|]
        Dim n As Integer? = text?.[|GetHashCode|]
        Dim count As Integer = values.[|Count|]
        Dim numbers As IEnumerable(Of Integer) = values.[|Cast(Of Integer)|]
        Dim mail As String() = (From m As String In addresses Select m.ToUpperInvariant()).[|ToArray|]
        Await Me.[|WorkAsync|]
        Return items.[|ToArray|]
    End Function
    Public Sub Work()
    End Sub
    Public Function WorkAsync() As Task
        Return Task.CompletedTask
    End Function
End Class
"

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="DCA053_MethodCallParenthesesTests"/> class.
        ''' </summary>
        Public Sub New()
        End Sub

#End Region

#Region " Test Methods "

        ''' <summary>
        ''' Verifies that rule DCA053 reports method calls without parentheses: statements, <see langword="Call"/>, member accesses, 
        ''' <c>?.</c>, extension methods (also generic), query results and <see langword="Await"/> operands.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Reports_CallsWithoutParentheses() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, ReportedSource).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that rule DCA053 does not report properties, calls with parentheses, <see langword="AddressOf"/>, 
        ''' <see langword="NameOf"/>, a name followed by an index, and the calls that the compiler generates for 
        ''' <see langword="For Each"/>, <see langword="Using"/>, <see langword="Await"/>, queries and collection initializers.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function DoesNotReport_OtherForms() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Imports System.Linq
Public Class C
    Public Property Name As String
    Public Async Function Run(items As List(Of Integer), text As String) As Task(Of Integer)
        Dim length As Integer = text.Length + items.Count + Me.Name.Length
        Me.Work()
        Dim trimmed As String = text.Trim()
        For Each item As Integer In items
            Console.WriteLine(item)
        Next item
        Using reader As New IO.StringReader(text)
            Console.WriteLine(reader.ReadLine())
        End Using
        Await Task.Delay(1).ConfigureAwait(False)
        Dim query As IEnumerable(Of Integer) = From i As Integer In items Where i > 0 Select i * 2
        Dim list As New List(Of Integer) From {1, 2}
        Dim action As Action = AddressOf Me.Work
        Dim workName As String = NameOf(Work)
        Dim first As Integer = GetItems(0)
        Return length + trimmed.Length + query.Sum() + list.Count + workName.Length + first
    End Function
    Public Sub Work()
    End Sub
    Public Function GetItems() As Integer()
        Return {1}
    End Function
End Class
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA053 adds the parentheses after the method name (and its type arguments).
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_AddsParentheses() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, ReportedSource, "
Imports System.Linq
Public Class C
    Public Async Function Run(items As List(Of Integer), text As String, values As IEnumerable(Of Object), addresses As IEnumerable(Of String)) As Task(Of Integer())
        Me.Work()
        Work()
        Call Work()
        Dim s As String = text.Trim()
        Dim n As Integer? = text?.GetHashCode()
        Dim count As Integer = values.Count()
        Dim numbers As IEnumerable(Of Integer) = values.Cast(Of Integer)()
        Dim mail As String() = (From m As String In addresses Select m.ToUpperInvariant()).ToArray()
        Await Me.WorkAsync()
        Return items.ToArray()
    End Function
    Public Sub Work()
    End Sub
    Public Function WorkAsync() As Task
        Return Task.CompletedTask
    End Function
End Class
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that Fix All of rule DCA053 fixes every call of the document (the reported <c>MailTo</c> example).
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function FixAll_FixesEveryCall() As Task
            Dim fixedText As String = Await AnalyzerTestHost.ApplyFixAllInDocumentAsync(Analyzer, Provider, "
Imports System.Linq
Public Class C
    Public Sub Send(addresses As IEnumerable(Of String), subject As String, body As String)
        C.MailTo((From m As String In addresses Select m.Trim).ToArray, subject.Trim, body)
    End Sub
    Public Shared Sub MailTo(addresses As String(), subject As String, body As String)
    End Sub
End Class
", NameOf(MethodCallParenthesesCodeFixProvider)).ConfigureAwait(True)
            AnalyzerTestHost.AssertTextEqual("
Imports System.Linq
Public Class C
    Public Sub Send(addresses As IEnumerable(Of String), subject As String, body As String)
        C.MailTo((From m As String In addresses Select m.Trim()).ToArray(), subject.Trim(), body)
    End Sub
    Public Shared Sub MailTo(addresses As String(), subject As String, body As String)
    End Sub
End Class
", fixedText)
        End Function

#End Region

    End Class

End Namespace

#End Region
