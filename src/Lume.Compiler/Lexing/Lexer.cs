using System.Text;
using Lume.Compiler.Diagnostics;
using Lume.Compiler.Text;

namespace Lume.Compiler.Lexing;

public sealed class Lexer
{
    private readonly SourceText sourceText;
    private readonly List<Diagnostic> diagnostics;
    private int position;

    public Lexer(SourceText sourceText)
    {
        this.sourceText = sourceText;
        diagnostics = new List<Diagnostic>();
    }

    public IReadOnlyList<Diagnostic> Diagnostics => diagnostics;

    public SyntaxToken Lex()
    {
        SkipWhitespace();

        if (IsAtEnd())
        {
            return new SyntaxToken(TokenKind.EndOfFile, new TextSpan(position, 0), string.Empty, null);
        }

        var start = position;
        var current = Current();

        if (char.IsLetter(current))
        {
            while (char.IsLetter(Current()))
            {
                Next();
            }

            var text = sourceText.Text.Substring(start, position - start);
            var kind = text == "print" ? TokenKind.PrintKeyword : TokenKind.Identifier;
            return new SyntaxToken(kind, new TextSpan(start, text.Length), text, null);
        }

        if (current == '"')
        {
            return LexStringLiteral();
        }

        var badChar = current;
        Next();
        var badSpan = new TextSpan(start, 1);
        diagnostics.Add(Diagnostic.Error(sourceText, badSpan, $"Unexpected character: '{badChar}'."));
        return new SyntaxToken(TokenKind.BadToken, badSpan, badChar.ToString(), null);
    }

    private SyntaxToken LexStringLiteral()
    {
        var start = position;
        var builder = new StringBuilder();
        Next();

        while (!IsAtEnd())
        {
            var current = Current();

            if (current == '"')
            {
                Next();
                var text = sourceText.Text.Substring(start, position - start);
                return new SyntaxToken(
                    TokenKind.StringLiteral,
                    new TextSpan(start, position - start),
                    text,
                    builder.ToString());
            }

            if (current == '\r' || current == '\n')
            {
                break;
            }

            if (current == '\\')
            {
                Next();
                if (IsAtEnd())
                {
                    break;
                }

                var escaped = Current();
                if (escaped == '"' || escaped == '\\')
                {
                    builder.Append(escaped);
                    Next();
                    continue;
                }

                builder.Append(escaped);
                Next();
                continue;
            }

            builder.Append(current);
            Next();
        }

        var span = new TextSpan(start, Math.Max(1, position - start));
        diagnostics.Add(Diagnostic.Error(sourceText, span, "Unterminated string literal."));
        return new SyntaxToken(TokenKind.StringLiteral, span, sourceText.Text.Substring(start, span.Length), builder.ToString());
    }

    private void SkipWhitespace()
    {
        while (char.IsWhiteSpace(Current()))
        {
            Next();
        }
    }

    private char Current() =>
        position >= sourceText.Text.Length ? '\0' : sourceText.Text[position];

    private void Next()
    {
        position++;
    }

    private bool IsAtEnd() =>
        position >= sourceText.Text.Length;
}
