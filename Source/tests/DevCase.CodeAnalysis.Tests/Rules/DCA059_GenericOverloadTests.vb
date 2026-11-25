#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Linq
Imports System.Threading.Tasks

Imports Microsoft.CodeAnalysis

Imports DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers
Imports DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.CodeFixes

Imports Xunit

#End Region

#Region " DCA059_GenericOverloadTests "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Tests

    ''' <summary>
    ''' Tests of rule DCA059 (generic overload instead of GetType, .NET Framework) and its code fix.
    ''' </summary>
    Public NotInheritable Class DCA059_GenericOverloadTests

#Region " Fields "

        ''' <summary>
        ''' The analyzer under test.
        ''' </summary>
        Private Shared ReadOnly Analyzer As New GenericOverloadAnalyzer()

        ''' <summary>
        ''' The code fix provider under test.
        ''' </summary>
        Private Shared ReadOnly Provider As New ReplacementCodeFixProvider()

        ''' <summary>
        ''' The expected messages of <see cref="Message_NamesTheGenericOverload"/>.
        ''' </summary>
        Private Shared ReadOnly Message_NamesTheGenericOverloadExpected As String() = {"Use the generic overload 'Activator.CreateInstance(Of T)' instead of 'GetType(T)' for compile-time type safety"}

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="DCA059_GenericOverloadTests"/> class.
        ''' </summary>
        Public Sub New()
        End Sub

#End Region

#Region " Test Methods "

        ''' <summary>
        ''' Verifies that rule DCA059 reports <c>GetType(T)</c> passed to <c>Activator.CreateInstance</c>, <c>Marshal.SizeOf</c> and
        ''' <c>Marshal.PtrToStructure</c>, which have a generic overload.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Reports_TypeArgumentsWithAGenericOverload() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Imports System.Runtime.InteropServices

Public Class C
    Public Function Create(Of T As New)() As T
        Return DirectCast([|Activator.CreateInstance(GetType(T))|], T)
    End Function

    Public Function Size() As Integer
        Return [|Marshal.SizeOf(GetType(NativePoint))|]
    End Function

    Public Function Read(pointer As IntPtr) As NativePoint
        Return CType([|Marshal.PtrToStructure(pointer, GetType(NativePoint))|], NativePoint)
    End Function

    Public Function Untyped() As Object
        Return [|Activator.CreateInstance(GetType(Item))|]
    End Function

    Public Function Converted() As IDisposable
        Return DirectCast(([|Activator.CreateInstance(GetType(Resource))|]), IDisposable)
    End Function
End Class

Public Structure NativePoint
    Public X As Integer
End Structure

Public Class Item
End Class

Public Class Resource
    Implements IDisposable

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class
", targetNetFramework:=True).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that rule DCA059 does not run in a modern .NET project, where the built-in rule CA2263 applies.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function DoesNotReport_OnModernNet() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Imports System.Runtime.InteropServices

Public Class C
    Public Function Create(Of T As New)() As T
        Return DirectCast(Activator.CreateInstance(GetType(T)), T)
    End Function

    Public Function Size() As Integer
        Return Marshal.SizeOf(GetType(NativePoint))
    End Function

    Public Function Read(pointer As IntPtr) As NativePoint
        Return CType(Marshal.PtrToStructure(pointer, GetType(NativePoint)), NativePoint)
    End Function

    Public Function Untyped() As Object
        Return Activator.CreateInstance(GetType(Item))
    End Function

    Public Function Converted() As IDisposable
        Return DirectCast((Activator.CreateInstance(GetType(Resource))), IDisposable)
    End Function
End Class

Public Structure NativePoint
    Public X As Integer
End Structure

Public Class Item
End Class

Public Class Resource
    Implements IDisposable

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that rule DCA059 does not report calls whose generic overload does not exist, has other parameters, or has a
        ''' constraint that the type does not satisfy.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function DoesNotReport_WhenTheGenericOverloadDoesNotApply() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Public Class C
    Public Function NonPublic() As Object
        Return Activator.CreateInstance(GetType(Item), True)
    End Function

    Public Function Runtime(type As Type) As Object
        Return Activator.CreateInstance(type)
    End Function

    Public Function Values() As Array
        Return [Enum].GetValues(GetType(DayOfWeek))
    End Function

    Public Function Constrained() As Object
        Return {Factory.Create(GetType(String)), Factory.Make(GetType(Item))}
    End Function
End Class

Public Class Item
End Class

Public NotInheritable Class Factory
    Private Sub New()
    End Sub

    Public Shared Function Create(type As Type) As Object
        Return Nothing
    End Function

    Public Shared Function Create(Of T As Structure)() As T
        Return Nothing
    End Function

    Public Shared Function Make(type As Type) As Object
        Return Nothing
    End Function

    Public Shared Function Make(Of T As {IDisposable, New})() As T
        Return Nothing
    End Function
End Class
", targetNetFramework:=True).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that rule DCA059 reports a generic overload whose constraints the type satisfies (<c>Structure</c>, an interface and
        ''' <c>New</c>), and <c>Activator.CreateInstance</c> for any type, because its generic overload has no constraint in .NET Framework.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Reports_ConstrainedOverloads_WhenTheTypeSatisfiesThem() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Public Class C
    Public Function Constrained() As Object
        Return [|Factory.Create(GetType(Integer))|].ToString() & [|Factory.Make(GetType(Resource))|].ToString()
    End Function

    Public Function Unconstrained(Of T)() As Object
        Return [|Activator.CreateInstance(GetType(T))|]
    End Function

    Public Function Abstract() As Object
        Return [|Activator.CreateInstance(GetType(Base))|]
    End Function
End Class

Public MustInherit Class Base
End Class

Public Class Resource
    Implements IDisposable

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Public NotInheritable Class Factory
    Private Sub New()
    End Sub

    Public Shared Function Create(type As Type) As Object
        Return Nothing
    End Function

    Public Shared Function Create(Of T As Structure)() As T
        Return Nothing
    End Function

    Public Shared Function Make(type As Type) As Object
        Return Nothing
    End Function

    Public Shared Function Make(Of T As {IDisposable, New})() As T
        Return Nothing
    End Function
End Class
", targetNetFramework:=True).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA059 calls the generic overload, without the <c>GetType</c> argument, and removes a
        ''' cast to the same type (the reported example: <c>DirectCast(Activator.CreateInstance(GetType(T)), T)</c>).
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_UsesTheGenericOverload_AndRemovesTheRedundantCast() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
Imports System.Runtime.InteropServices

Public Class C
    Public Function Create(Of T As New)() As T
        Return DirectCast([|Activator.CreateInstance(GetType(T))|], T)
    End Function

    Public Function Size() As Integer
        Return [|Marshal.SizeOf(GetType(NativePoint))|]
    End Function

    Public Function Read(pointer As IntPtr) As NativePoint
        Return CType([|Marshal.PtrToStructure(pointer, GetType(NativePoint))|], NativePoint)
    End Function

    Public Function Untyped() As Object
        Return [|Activator.CreateInstance(GetType(Item))|]
    End Function

    Public Function Converted() As IDisposable
        Return DirectCast(([|Activator.CreateInstance(GetType(Resource))|]), IDisposable)
    End Function
End Class

Public Structure NativePoint
    Public X As Integer
End Structure

Public Class Item
End Class

Public Class Resource
    Implements IDisposable

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class
", "
Imports System.Runtime.InteropServices

Public Class C
    Public Function Create(Of T As New)() As T
        Return Activator.CreateInstance(Of T)()
    End Function

    Public Function Size() As Integer
        Return Marshal.SizeOf(Of NativePoint)()
    End Function

    Public Function Read(pointer As IntPtr) As NativePoint
        Return Marshal.PtrToStructure(Of NativePoint)(pointer)
    End Function

    Public Function Untyped() As Object
        Return Activator.CreateInstance(Of Item)()
    End Function

    Public Function Converted() As IDisposable
        Return DirectCast((Activator.CreateInstance(Of Resource)()), IDisposable)
    End Function
End Class

Public Structure NativePoint
    Public X As Integer
End Structure

Public Class Item
End Class

Public Class Resource
    Implements IDisposable

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class
",
                                                      equivalenceKey:="DCA059.Generic", targetNetFramework:=True).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies the message of rule DCA059.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Message_NamesTheGenericOverload() As Task
            Dim diagnostics As ImmutableArray(Of Diagnostic) = Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Public Class C
    Public Function Create(Of T As New)() As T
        Return DirectCast([|Activator.CreateInstance(GetType(T))|], T)
    End Function
End Class
",
                                                                                                         targetNetFramework:=True).ConfigureAwait(True)
            Assert.Equal(DCA059_GenericOverloadTests.Message_NamesTheGenericOverloadExpected, diagnostics.Select(Function(d As Diagnostic) d.GetMessage(Globalization.CultureInfo.InvariantCulture)).ToArray())
        End Function

#End Region

    End Class

End Namespace

#End Region
