#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Immutable

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

#End Region

#Region " ComImportClassCodeAnalyzer "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Reports code declared in a class marked with <c>&lt;ComImport&gt;</c> (rule <see cref="DiagnosticIds.ComImportClassCode"/>):
    ''' constructors (instance and <see langword="Shared"/>), methods, operators, properties (also auto-implemented),
    ''' custom events, and field initializers.
    ''' </summary>
    '''
    ''' <remarks>
    ''' The Visual Basic compiler accepts these members without an error or a warning, but compiles every one of them as a
    ''' runtime-implemented member (<c>MethodImplAttributes.InternalCall Or MethodImplAttributes.Runtime</c>) without a body:
    ''' their code never runs. Field initializers are part of the constructors, so they never run either.
    ''' Fields without an initializer, nested types and <see langword="Declare"/> statements are not reported.
    ''' </remarks>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public NotInheritable Class ComImportClassCodeAnalyzer : Inherits DiagnosticAnalyzer

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(DevCaseDescriptors.ComImportClassCode)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="ComImportClassCodeAnalyzer"/> class.
        ''' </summary>
        Public Sub New()
        End Sub

#End Region

#Region " Public Methods "

        ''' <inheritdoc/>
        Public Overrides Sub Initialize(context As AnalysisContext)

            If context Is Nothing Then
                Throw New ArgumentNullException(NameOf(context))
            End If

            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None)
            context.EnableConcurrentExecution()
            context.RegisterSyntaxNodeAction(UtilAnalyzerFiles.SkipExcludedNodes(AddressOf ComImportClassCodeAnalyzer.AnalyzeClass), SyntaxKind.ClassBlock)
        End Sub

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Reports the members with code of a <c>&lt;ComImport&gt;</c> class.
        ''' </summary>
        '''
        ''' <param name="context">
        ''' The syntax node analysis context.
        ''' </param>
        Private Shared Sub AnalyzeClass(context As SyntaxNodeAnalysisContext)

            Dim block As ClassBlockSyntax = DirectCast(context.Node, ClassBlockSyntax)
            Dim type As INamedTypeSymbol = context.SemanticModel.GetDeclaredSymbol(block, context.CancellationToken)
            If type Is Nothing OrElse Not UtilAnalyzerSemantic.IsComImport(type) Then
                Exit Sub
            End If

            For Each member As StatementSyntax In block.Members
                Select Case True
                    Case TypeOf member Is ConstructorBlockSyntax
                        Dim statement As SubNewStatementSyntax = DirectCast(member, ConstructorBlockSyntax).SubNewStatement
                        Dim name As String = If(statement.Modifiers.Any(SyntaxKind.SharedKeyword), "the shared constructor", "the constructor")
                        ComImportClassCodeAnalyzer.Report(context, statement.NewKeyword, name, type)

                    Case TypeOf member Is MethodBlockSyntax
                        Dim statement As MethodStatementSyntax = DirectCast(member, MethodBlockSyntax).SubOrFunctionStatement
                        ComImportClassCodeAnalyzer.Report(context, statement.Identifier, $"method '{statement.Identifier.ValueText}'", type)

                    Case TypeOf member Is OperatorBlockSyntax
                        Dim statement As OperatorStatementSyntax = DirectCast(member, OperatorBlockSyntax).OperatorStatement
                        ComImportClassCodeAnalyzer.Report(context, statement.OperatorToken, $"operator '{statement.OperatorToken.Text}'", type)

                    Case TypeOf member Is PropertyBlockSyntax
                        Dim statement As PropertyStatementSyntax = DirectCast(member, PropertyBlockSyntax).PropertyStatement
                        ComImportClassCodeAnalyzer.Report(context, statement.Identifier, $"property '{statement.Identifier.ValueText}'", type)

                    Case TypeOf member Is PropertyStatementSyntax
                        ' An auto-implemented property: its generated accessors have no body either.
                        Dim statement As PropertyStatementSyntax = DirectCast(member, PropertyStatementSyntax)
                        If Not statement.Modifiers.Any(SyntaxKind.MustOverrideKeyword) Then
                            ComImportClassCodeAnalyzer.Report(context, statement.Identifier, $"property '{statement.Identifier.ValueText}'", type)
                        End If

                    Case TypeOf member Is EventBlockSyntax
                        Dim statement As EventStatementSyntax = DirectCast(member, EventBlockSyntax).EventStatement
                        ComImportClassCodeAnalyzer.Report(context, statement.Identifier, $"event '{statement.Identifier.ValueText}'", type)

                    Case TypeOf member Is FieldDeclarationSyntax
                        For Each declarator As VariableDeclaratorSyntax In DirectCast(member, FieldDeclarationSyntax).Declarators
                            If declarator.Initializer IsNot Nothing OrElse TypeOf declarator.AsClause Is AsNewClauseSyntax Then
                                For Each name As ModifiedIdentifierSyntax In declarator.Names
                                    ComImportClassCodeAnalyzer.Report(context, name.Identifier, $"the initializer of field '{name.Identifier.ValueText}'", type)
                                Next name
                            End If
                        Next declarator
                End Select
            Next member
        End Sub

        ''' <summary>
        ''' Reports one member.
        ''' </summary>
        '''
        ''' <param name="context">
        ''' The syntax node analysis context.
        ''' </param>
        '''
        ''' <param name="token">
        ''' The token where the diagnostic is reported (the member name, or <see langword="New"/>).
        ''' </param>
        '''
        ''' <param name="memberName">
        ''' The member, as shown in the message (for example <c>the constructor</c> or <c>method 'Show'</c>).
        ''' </param>
        '''
        ''' <param name="type">
        ''' The <c>&lt;ComImport&gt;</c> class.
        ''' </param>
        Private Shared Sub Report(context As SyntaxNodeAnalysisContext, token As SyntaxToken, memberName As String, type As INamedTypeSymbol)

            context.ReportDiagnostic(Diagnostic.Create(DevCaseDescriptors.ComImportClassCode, token.GetLocation(), memberName, type.Name))
        End Sub

#End Region

    End Class

End Namespace

#End Region
