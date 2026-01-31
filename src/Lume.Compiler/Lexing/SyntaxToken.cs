using Lume.Compiler.Text;

namespace Lume.Compiler.Lexing;

public sealed class SyntaxToken
{
    public TokenKind Kind { get; }
    public TextSpan Span { get; }
    public string Text { get; }
    public object? Value { get; }

    public int Position => Span.Start;

    public SyntaxToken(TokenKind kind, TextSpan span, string text, object? value)
    {
        Kind = kind;
        Span = span;
        Text = text;
        Value = value;
    }

    public static SyntaxToken Missing(TokenKind kind, int position) =>
        new(kind, new TextSpan(position, 0), string.Empty, null);
}
