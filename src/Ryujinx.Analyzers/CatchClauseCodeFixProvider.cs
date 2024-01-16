using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SyntaxFactory = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using SyntaxKind = Microsoft.CodeAnalysis.CSharp.SyntaxKind;

namespace Ryujinx.Analyzers
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CatchClauseCodeFixProvider)), Shared]
    public class CatchClauseCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds { get; } =
            ImmutableArray.Create(CatchClauseAnalyzer.DiagnosticId);

        public override FixAllProvider? GetFixAllProvider() => null;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.Single();
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            // Find SyntaxNode corresponding to the diagnostic.
            var diagnosticNode = root?.FindNode(diagnosticSpan);

            // To get the required metadata, we should match the Node to the specific type: 'CatchClauseSyntax'.
            if (diagnosticNode is not CatchClauseSyntax catchClause)
                return;

            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: string.Format(Resources.RYU0001CodeFixTitle, GetExceptionType(catchClause)),
                    createChangedDocument: c => LogException(context.Document, catchClause, c),
                    equivalenceKey: nameof(Resources.RYU0001CodeFixTitle)),
                diagnostic);
        }

        private static string GetExceptionType(CatchClauseSyntax catchClause)
        {
            return catchClause.Declaration != null ? catchClause.Declaration.Type.ToString() : "Exception";
        }

        private static MemberAccessExpressionSyntax GetLoggingClass(string className)
        {
            return SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName("Ryujinx"),
                        SyntaxFactory.IdentifierName("Common")),
                    SyntaxFactory.IdentifierName("Logging")),
                SyntaxFactory.IdentifierName(className))
                .WithAdditionalAnnotations(Simplifier.AddImportsAnnotation, Simplifier.Annotation);
        }

        /// <summary>
        /// Executed on the quick fix action raised by the user.
        /// </summary>
        /// <param name="document">Affected source file.</param>
        /// <param name="catchClauseSyntax">Highlighted catch clause syntax node.</param>
        /// <param name="cancellationToken">Any fix is cancellable by the user, so we should support the cancellation token.</param>
        /// <returns>Clone of the document with the modified catch clause.</returns>
        private async Task<Document> LogException(Document document,
            CatchClauseSyntax catchClauseSyntax, CancellationToken cancellationToken)
        {
            CatchDeclarationSyntax catchDeclaration;
            string catchDeclarationIdentifier = "exception";

            // Add a catch declaration if it doesn't exist.
            if (catchClauseSyntax.Declaration == null)
            {
                // System.Exception exception
                catchDeclaration =
                   SyntaxFactory.CatchDeclaration(
                       SyntaxFactory.QualifiedName(
                           SyntaxFactory.IdentifierName("System"), SyntaxFactory.IdentifierName("Exception")),
                       SyntaxFactory.Identifier("exception")
                   );
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(catchClauseSyntax.Declaration.Identifier.Text))
                {
                    catchDeclaration = catchClauseSyntax.Declaration;
                    catchDeclarationIdentifier = catchDeclaration.Identifier.Text;
                }
                else
                {
                    catchDeclaration = catchClauseSyntax.Declaration.WithIdentifier(
                        SyntaxFactory.Identifier(catchDeclarationIdentifier));
                }
            }

            // Create logging statement.
            // Ryujinx.Common.Logging.Logger.Error?.Print(LogClass.Application, $"Exception caught: {exception}");
            var newStatements = catchClauseSyntax.Block.Statements.Insert(0, SyntaxFactory.ExpressionStatement(SyntaxFactory.ConditionalAccessExpression(
                SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                    GetLoggingClass("Logger"),
                    SyntaxFactory.IdentifierName("Error")),
                SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberBindingExpression(SyntaxFactory.IdentifierName("Print")))
                    .AddArgumentListArguments(
                        SyntaxFactory.Argument(SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            GetLoggingClass("LogClass"),
                            SyntaxFactory.IdentifierName("Application"))),
                        SyntaxFactory.Argument(SyntaxFactory.InterpolatedStringExpression(
                            SyntaxFactory.Token(SyntaxKind.InterpolatedStringStartToken)).AddContents(
                                SyntaxFactory.InterpolatedStringText().WithTextToken(
                                    SyntaxFactory.Token(
                                        SyntaxTriviaList.Empty,
                                        SyntaxKind.InterpolatedStringTextToken,
                                        "Exception caught: ",
                                        "Exception caught: ",
                                        SyntaxTriviaList.Empty)
                                ),
                                SyntaxFactory.Interpolation(
                                    SyntaxFactory.IdentifierName(
                                        catchDeclarationIdentifier).WithAdditionalAnnotations(
                                        RenameAnnotation.Create())
                                )
                            )
                        )
                    )
            )));

            // Produce the new catch clause.
            var newCatchClause = catchClauseSyntax
                .WithCatchKeyword(catchClauseSyntax.CatchKeyword.WithTrailingTrivia(SyntaxFactory.Space))
                .WithDeclaration(catchDeclaration)
                .WithBlock(catchClauseSyntax.Block.WithStatements(newStatements))
                .WithAdditionalAnnotations(Formatter.Annotation);

            // Replace the old local declaration with the new local declaration.
            SyntaxNode oldRoot = (await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false))!;
            SyntaxNode newRoot = oldRoot.ReplaceNode(catchClauseSyntax, newCatchClause);

            // Return document with the transformed tree.
            return document.WithSyntaxRoot(newRoot);
        }
    }
}
