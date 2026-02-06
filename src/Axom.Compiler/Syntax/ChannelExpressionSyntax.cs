using Axom.Compiler.Lexing;
using Axom.Compiler.Text;

namespace Axom.Compiler.Syntax;

public sealed class ChannelExpressionSyntax : ExpressionSyntax
{
    public SyntaxToken ChannelIdentifier { get; }
    public SyntaxToken LessToken { get; }
    public TypeSyntax ElementType { get; }
    public SyntaxToken GreaterToken { get; }
    public SyntaxToken OpenParenToken { get; }
    public SyntaxToken CloseParenToken { get; }

    public ChannelExpressionSyntax(
        SyntaxToken channelIdentifier,
        SyntaxToken lessToken,
        TypeSyntax elementType,
        SyntaxToken greaterToken,
        SyntaxToken openParenToken,
        SyntaxToken closeParenToken)
    {
        ChannelIdentifier = channelIdentifier;
        LessToken = lessToken;
        ElementType = elementType;
        GreaterToken = greaterToken;
        OpenParenToken = openParenToken;
        CloseParenToken = closeParenToken;
    }

    public override TextSpan Span =>
        TextSpan.FromBounds(ChannelIdentifier.Span.Start, CloseParenToken.Span.End);
}
