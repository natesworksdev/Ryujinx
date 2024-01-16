using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace Ryujinx.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class CatchClauseAnalyzer : DiagnosticAnalyzer
    {
        private const string LoggerIdentifier = "Logger";
        private const string LogClassIdentifier = "LogClass";

        public const string DiagnosticId = "RYU0001";

        private static readonly LocalizableString _title = new LocalizableResourceString(nameof(Resources.RYU0001Title),
            Resources.ResourceManager, typeof(Resources));

        private static readonly LocalizableString _messageFormat =
            new LocalizableResourceString(nameof(Resources.RYU0001MessageFormat), Resources.ResourceManager,
                typeof(Resources));

        private static readonly LocalizableString _description =
            new LocalizableResourceString(nameof(Resources.RYU0001Description), Resources.ResourceManager,
                typeof(Resources));

        private const string Category = "Maintainability";

        private static readonly DiagnosticDescriptor _rule = new(DiagnosticId, _title, _messageFormat, Category,
            DiagnosticSeverity.Warning, isEnabledByDefault: true, description: _description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(_rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.ReportDiagnostics);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.CatchClause);
        }

        private static bool EndsWithExpressionText(ExpressionSyntax expression, string text)
        {
            if (expression is MemberAccessExpressionSyntax memberAccessExpression)
            {
                if (memberAccessExpression.Expression.ToString().EndsWith(text))
                {
                    return true;
                }
            }

            foreach (var childNode in expression.ChildNodes())
            {
                if (childNode is not ExpressionSyntax childExpression)
                {
                    continue;
                }

                if (EndsWithExpressionText(childExpression, text))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ContainsInvocationArgText(ExpressionSyntax expression, string text)
        {
            if (expression is InvocationExpressionSyntax invocationExpression)
            {
                if (invocationExpression.ArgumentList.Arguments.Count > 0)
                {
                    var invocationArg = invocationExpression.ArgumentList.Arguments.First();

                    return invocationArg.ToString().StartsWith($"{text}.") ||
                           invocationArg.ToString().Contains($".{text}.");
                }

                return false;
            }

            foreach (var childNode in expression.ChildNodes())
            {
                if (childNode is not ExpressionSyntax childExpression)
                {
                    continue;
                }

                if (ContainsInvocationArgText(childExpression, text))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ContainsIdentifier(ExpressionSyntax expression, string identifierText)
        {
            return expression.DescendantNodes().Any(
                x => x is IdentifierNameSyntax identifierName
                     && identifierName.ToString() == identifierText);
        }

        /// <summary>
        /// Executed for each Syntax Node with 'SyntaxKind.CatchClause'.
        /// </summary>
        /// <param name="context">Operation context.</param>
        private void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
        {
            if (context.Node is not CatchClauseSyntax catchClauseSyntax)
                return;

            var catchDeclaration = catchClauseSyntax.Declaration;

            // Find catch clauses without declaration.
            if (catchDeclaration == null)
            {
                var diagnostic = Diagnostic.Create(_rule,
                    // The highlighted area in the analyzed source code. Keep it as specific as possible.
                    catchClauseSyntax.GetLocation(),
                    // The value is passed to 'MessageFormat' argument of your 'Rule'.
                    "Exception");

                context.ReportDiagnostic(diagnostic);
            }
            // Find catch declarations without an identifier
            else if (string.IsNullOrWhiteSpace(catchDeclaration.Identifier.Text))
            {
                var diagnostic = Diagnostic.Create(_rule,
                    // The highlighted area in the analyzed source code. Keep it as specific as possible.
                    catchClauseSyntax.GetLocation(),
                    // The value is passed to 'MessageFormat' argument of your 'Rule'.
                    catchDeclaration.Type.ToString());

                context.ReportDiagnostic(diagnostic);
            }
            // Check logging statements for a reference to the identifier of the catch declaration 
            else
            {
                var catchDeclarationIdentifier = catchDeclaration.Identifier;
                bool exceptionLogged = false;

                // Iterate through all expression statements
                foreach (var statement in catchClauseSyntax.Block.Statements)
                {
                    if (statement is not ExpressionStatementSyntax expressionStatement)
                    {
                        continue;
                    }

                    // Find Logger invocation
                    if (EndsWithExpressionText(expressionStatement.Expression, LoggerIdentifier) && ContainsInvocationArgText(expressionStatement.Expression, LogClassIdentifier))
                    {
                        // Find catchDeclarationIdentifier in Logger invocation
                        if (ContainsIdentifier(expressionStatement.Expression, catchDeclarationIdentifier.Text))
                        {
                            exceptionLogged = true;
                        }
                    }
                }

                // Create a diagnostic report if the exception was not logged
                if (!exceptionLogged)
                {
                    var diagnostic = Diagnostic.Create(_rule,
                        // The highlighted area in the analyzed source code. Keep it as specific as possible.
                        catchClauseSyntax.GetLocation(),
                        // The value is passed to 'MessageFormat' argument of your 'Rule'.
                        catchDeclaration.Type.ToString());

                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }
}
