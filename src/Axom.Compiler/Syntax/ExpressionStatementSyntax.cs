using Axom.Compiler.Text;

namespace Axom.Compiler.Syntax;

public sealed class ExpressionStatementSyntax : StatementSyntax
{
    public ExpressionSyntax Expression { get; }

    public ExpressionStatementSyntax(ExpressionSyntax expression)
    {
        Expression = expression;
    }

    public override TextSpan Span => Expression.Span;
}
