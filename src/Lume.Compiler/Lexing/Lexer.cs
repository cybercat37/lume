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
        while (true)
        {
            if (IsAtEnd())
            {
                return new SyntaxToken(TokenKind.EndOfFile, new TextSpan(position, 0), string.Empty, null);
            }

            if (IsLineBreak(Current()))
            {
                return LexNewLine();
            }

            SkipWhitespace();

            if (IsAtEnd())
            {
                return new SyntaxToken(TokenKind.EndOfFile, new TextSpan(position, 0), string.Empty, null);
            }

            if (IsLineBreak(Current()))
            {
                return LexNewLine();
            }

            if (IsCommentStart())
            {
                if (Peek(1) == '/')
                {
                    SkipSingleLineComment();
                }
                else
                {
                    SkipMultiLineComment();
                }

                continue;
            }

            break;
        }

        var start = position;
        var current = Current();

        if (char.IsLetter(current))
        {
            while (char.IsLetterOrDigit(Current()))
            {
                Next();
            }

            var text = sourceText.Text.Substring(start, position - start);
            var kind = GetKeywordKind(text);
            object? value = kind switch
            {
                TokenKind.TrueKeyword => true,
                TokenKind.FalseKeyword => false,
                _ => null
            };
            return new SyntaxToken(kind, new TextSpan(start, text.Length), text, value);
        }

        if (char.IsDigit(current))
        {
            return LexNumberLiteral();
        }

        if (current == '"')
        {
            return LexStringLiteral();
        }

        switch (current)
        {
            case '=':
                Next();
                return new SyntaxToken(TokenKind.EqualsToken, new TextSpan(start, 1), "=", null);
            case '+':
                Next();
                return new SyntaxToken(TokenKind.Plus, new TextSpan(start, 1), "+", null);
            case '-':
                Next();
                return new SyntaxToken(TokenKind.Minus, new TextSpan(start, 1), "-", null);
            case '*':
                Next();
                return new SyntaxToken(TokenKind.Star, new TextSpan(start, 1), "*", null);
            case '/':
                Next();
                return new SyntaxToken(TokenKind.Slash, new TextSpan(start, 1), "/", null);
            case '(':
                Next();
                return new SyntaxToken(TokenKind.OpenParen, new TextSpan(start, 1), "(", null);
            case ')':
                Next();
                return new SyntaxToken(TokenKind.CloseParen, new TextSpan(start, 1), ")", null);
            case '{':
                Next();
                return new SyntaxToken(TokenKind.OpenBrace, new TextSpan(start, 1), "{", null);
            case '}':
                Next();
                return new SyntaxToken(TokenKind.CloseBrace, new TextSpan(start, 1), "}", null);
            case ';':
                Next();
                return new SyntaxToken(TokenKind.Semicolon, new TextSpan(start, 1), ";", null);
        }

        var badChar = current;
        Next();
        var badSpan = new TextSpan(start, 1);
        diagnostics.Add(Diagnostic.Error(sourceText, badSpan, $"Expected token, found '{badChar}'."));
        return new SyntaxToken(TokenKind.BadToken, badSpan, badChar.ToString(), null);
    }

    private SyntaxToken LexNewLine()
    {
        var start = position;
        if (Current() == '\r' && Peek(1) == '\n')
        {
            position += 2;
            return new SyntaxToken(TokenKind.NewLine, new TextSpan(start, 2), "\r\n", null);
        }

        Next();
        return new SyntaxToken(TokenKind.NewLine, new TextSpan(start, 1), "\n", null);
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
                builder.Append(escaped switch
                {
                    '"' => '"',
                    '\\' => '\\',
                    'n' => '\n',
                    't' => '\t',
                    'r' => '\r',
                    _ => escaped
                });
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

    private SyntaxToken LexNumberLiteral()
    {
        var start = position;
        while (char.IsDigit(Current()))
        {
            Next();
        }

        var text = sourceText.Text.Substring(start, position - start);
        if (int.TryParse(text, out var value))
        {
            return new SyntaxToken(TokenKind.NumberLiteral, new TextSpan(start, text.Length), text, value);
        }

        var span = new TextSpan(start, text.Length);
        diagnostics.Add(Diagnostic.Error(sourceText, span, "Number literal is too large."));
        return new SyntaxToken(TokenKind.NumberLiteral, span, text, 0);
    }

    private void SkipWhitespace()
    {
        while (char.IsWhiteSpace(Current()) && !IsLineBreak(Current()))
        {
            Next();
        }
    }

    private bool IsCommentStart() =>
        Current() == '/' && (Peek(1) == '/' || Peek(1) == '*');

    private void SkipSingleLineComment()
    {
        Next();
        Next();

        while (!IsAtEnd() && !IsLineBreak(Current()))
        {
            Next();
        }
    }

    private void SkipMultiLineComment()
    {
        var start = position;
        Next();
        Next();

        while (!IsAtEnd())
        {
            if (Current() == '*' && Peek(1) == '/')
            {
                Next();
                Next();
                return;
            }

            Next();
        }

        var span = new TextSpan(start, Math.Max(1, position - start));
        diagnostics.Add(Diagnostic.Error(sourceText, span, "Unterminated block comment."));
    }

    private char Current() =>
        position >= sourceText.Text.Length ? '\0' : sourceText.Text[position];

    private char Peek(int offset)
    {
        var index = position + offset;
        return index >= sourceText.Text.Length ? '\0' : sourceText.Text[index];
    }

    private void Next()
    {
        position++;
    }

    private bool IsAtEnd() =>
        position >= sourceText.Text.Length;

    private static bool IsLineBreak(char c) =>
        c == '\n' || c == '\r';

    private static TokenKind GetKeywordKind(string text)
    {
        return text switch
        {
            "print" => TokenKind.PrintKeyword,
            "let" => TokenKind.LetKeyword,
            "mut" => TokenKind.MutKeyword,
            "true" => TokenKind.TrueKeyword,
            "false" => TokenKind.FalseKeyword,
            _ => TokenKind.Identifier
        };
    }
}
