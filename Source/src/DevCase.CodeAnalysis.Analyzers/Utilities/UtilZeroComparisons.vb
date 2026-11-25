#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Globalization

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Operations

#End Region

#Region " UtilZeroComparisons "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Recognizes comparisons of a count with zero or one that mean "is empty" or "is not empty" 
    ''' (<c>x.Count() &gt; 0</c>, <c>0 = x.Count</c>, <c>x.Count &lt; 1</c>...).
    ''' </summary>
    Friend NotInheritable Class UtilZeroComparisons

#Region " Constructors "

        ''' <summary>
        ''' Prevents a default instance of the <see cref="UtilZeroComparisons"/> class from being created.
        ''' </summary>
        Private Sub New()
        End Sub

#End Region

#Region " Friend Methods "

        ''' <summary>
        ''' Determines whether a binary operation compares an operand with zero or one, as an emptiness check.
        ''' </summary>
        '''
        ''' <param name="binary">
        ''' The binary operation.
        ''' </param>
        '''
        ''' <param name="equalityOnly">
        ''' <see langword="True"/> to accept only <c>= 0</c> and <c>&lt;&gt; 0</c> (for values that can be negative, such as 
        ''' the result of <c>IndexOf</c>); <see langword="False"/> to also accept <c>&gt; 0</c>, <c>&gt;= 1</c>, <c>&lt; 1</c> 
        ''' and <c>&lt;= 0</c> (for counts, which are never negative).
        ''' </param>
        '''
        ''' <param name="operand">
        ''' When this method returns <see langword="True"/>, the compared operand, without implicit conversions.
        ''' </param>
        '''
        ''' <param name="meansEmpty">
        ''' When this method returns <see langword="True"/>, <see langword="True"/> if the comparison is true when the operand is 
        ''' zero (<c>= 0</c>, <c>&lt; 1</c>, <c>&lt;= 0</c>); <see langword="False"/> if it is true when the operand is not zero.
        ''' </param>
        '''
        ''' <returns>
        ''' <see langword="True"/> if the operation is such a comparison; otherwise, <see langword="False"/>.
        ''' </returns>
        '''
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="binary"/> is <see langword="Nothing"/>.
        ''' </exception>
        Friend Shared Function TryMatch(binary As IBinaryOperation, equalityOnly As Boolean, ByRef operand As IOperation, ByRef meansEmpty As Boolean) As Boolean

            If binary Is Nothing Then
                Throw New ArgumentNullException(NameOf(binary))
            End If

            operand = Nothing
            meansEmpty = False
            Dim kind As BinaryOperatorKind = binary.OperatorKind
            Dim constant As Long
            If UtilZeroComparisons.TryGetConstant(binary.RightOperand, constant) Then
                operand = UtilAnalyzerSemantic.SkipImplicitConversions(binary.LeftOperand)
            ElseIf UtilZeroComparisons.TryGetConstant(binary.LeftOperand, constant) Then
                operand = UtilAnalyzerSemantic.SkipImplicitConversions(binary.RightOperand)
                kind = UtilZeroComparisons.Mirror(kind) ' "0 < x" is "x > 0".
            Else
                Return False
            End If

            Select Case kind
                Case BinaryOperatorKind.Equals
                    meansEmpty = True
                    Return constant = 0L
                Case BinaryOperatorKind.NotEquals
                    meansEmpty = False
                    Return constant = 0L
                Case BinaryOperatorKind.GreaterThan
                    meansEmpty = False
                    Return Not equalityOnly AndAlso constant = 0L
                Case BinaryOperatorKind.GreaterThanOrEqual
                    meansEmpty = False
                    Return Not equalityOnly AndAlso constant = 1L
                Case BinaryOperatorKind.LessThan
                    meansEmpty = True
                    Return Not equalityOnly AndAlso constant = 1L
                Case BinaryOperatorKind.LessThanOrEqual
                    meansEmpty = True
                    Return Not equalityOnly AndAlso constant = 0L
                Case Else
                    Return False
            End Select
        End Function

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Gets the value of an integral constant operand.
        ''' </summary>
        '''
        ''' <param name="operand">
        ''' The operand.
        ''' </param>
        '''
        ''' <param name="value">
        ''' When this method returns <see langword="True"/>, the value.
        ''' </param>
        '''
        ''' <returns>
        ''' <see langword="True"/> if the operand is an integral constant; otherwise, <see langword="False"/>.
        ''' </returns>
        Private Shared Function TryGetConstant(operand As IOperation, ByRef value As Long) As Boolean

            value = 0L
            If operand Is Nothing OrElse Not operand.ConstantValue.HasValue Then
                Return False
            End If

            Dim constant As Object = operand.ConstantValue.Value
            If TypeOf constant Is Integer OrElse TypeOf constant Is Long OrElse TypeOf constant Is Short OrElse TypeOf constant Is Byte Then
                value = Convert.ToInt64(constant, CultureInfo.InvariantCulture)
                Return True
            End If
            Return False
        End Function

        ''' <summary>
        ''' Gets the operator that gives the same result with the operands swapped.
        ''' </summary>
        '''
        ''' <param name="kind">
        ''' The operator.
        ''' </param>
        '''
        ''' <returns>
        ''' The mirrored operator (<c>&lt;</c> for <c>&gt;</c>...); the same operator for <c>=</c> and <c>&lt;&gt;</c>.
        ''' </returns>
        Private Shared Function Mirror(kind As BinaryOperatorKind) As BinaryOperatorKind

            Select Case kind
                Case BinaryOperatorKind.LessThan
                    Return BinaryOperatorKind.GreaterThan
                Case BinaryOperatorKind.LessThanOrEqual
                    Return BinaryOperatorKind.GreaterThanOrEqual
                Case BinaryOperatorKind.GreaterThan
                    Return BinaryOperatorKind.LessThan
                Case BinaryOperatorKind.GreaterThanOrEqual
                    Return BinaryOperatorKind.LessThanOrEqual
                Case Else
                    Return kind
            End Select
        End Function

#End Region

    End Class

End Namespace

#End Region
