using Axom.Compiler.Lexing;
using Axom.Compiler.Text;

namespace Axom.Compiler.Syntax;

public sealed class MapEntrySyntax : SyntaxNode
{
    public ExpressionSyntax Key { get; }
    public SyntaxToken ColonToken { get; }
    public ExpressionSyntax Value { get; }

    public MapEntrySyntax(ExpressionSyntax key, SyntaxToken colonToken, ExpressionSyntax value)
    {
        Key = key;
        ColonToken = colonToken;
        Value = value;
    }

    public override TextSpan Span =>
        TextSpan.FromBounds(Key.Span.Start, Value.Span.End);
}
