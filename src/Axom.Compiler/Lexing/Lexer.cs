using System.Text;
using System.Globalization;
using Axom.Compiler.Diagnostics;
using Axom.Compiler.Text;

namespace Axom.Compiler.Lexing;

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

            break;
        }

        var start = position;
        var current = Current();

        if (IsIdentifierStart(current))
        {
            while (IsIdentifierPart(Current()))
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
                if (Peek(1) == '>')
                {
                    position += 2;
                    return new SyntaxToken(TokenKind.Arrow, new TextSpan(start, 2), "=>", null);
                }

                if (Peek(1) == '=')
                {
                    position += 2;
                    return new SyntaxToken(TokenKind.EqualEqual, new TextSpan(start, 2), "==", null);
                }

                Next();
                return new SyntaxToken(TokenKind.EqualsToken, new TextSpan(start, 1), "=", null);
            case '+':
                Next();
                return new SyntaxToken(TokenKind.Plus, new TextSpan(start, 1), "+", null);
            case '-':
                if (Peek(1) == '>')
                {
                    position += 2;
                    return new SyntaxToken(TokenKind.ArrowType, new TextSpan(start, 2), "->", null);
                }

                Next();
                return new SyntaxToken(TokenKind.Minus, new TextSpan(start, 1), "-", null);
            case '*':
                Next();
                return new SyntaxToken(TokenKind.Star, new TextSpan(start, 1), "*", null);
            case '/':
                Next();
                return new SyntaxToken(TokenKind.Slash, new TextSpan(start, 1), "/", null);
            case '[':
                Next();
                return new SyntaxToken(TokenKind.OpenBracket, new TextSpan(start, 1), "[", null);
            case ']':
                Next();
                return new SyntaxToken(TokenKind.CloseBracket, new TextSpan(start, 1), "]", null);
            case '!':
                if (Peek(1) == '=')
                {
                    position += 2;
                    return new SyntaxToken(TokenKind.BangEqual, new TextSpan(start, 2), "!=", null);
                }

                Next();
                return new SyntaxToken(TokenKind.Bang, new TextSpan(start, 1), "!", null);
            case '?':
                Next();
                return new SyntaxToken(TokenKind.QuestionToken, new TextSpan(start, 1), "?", null);
            case '&':
                if (Peek(1) == '&')
                {
                    position += 2;
                    return new SyntaxToken(TokenKind.AmpersandAmpersand, new TextSpan(start, 2), "&&", null);
                }

                break;
            case '|':
                if (Peek(1) == '|')
                {
                    position += 2;
                    return new SyntaxToken(TokenKind.PipePipe, new TextSpan(start, 2), "||", null);
                }

                break;
            case '<':
                if (Peek(1) == '=')
                {
                    position += 2;
                    return new SyntaxToken(TokenKind.LessOrEqual, new TextSpan(start, 2), "<=", null);
                }

                Next();
                return new SyntaxToken(TokenKind.Less, new TextSpan(start, 1), "<", null);
            case '>':
                if (Peek(1) == '=')
                {
                    position += 2;
                    return new SyntaxToken(TokenKind.GreaterOrEqual, new TextSpan(start, 2), ">=", null);
                }

                Next();
                return new SyntaxToken(TokenKind.Greater, new TextSpan(start, 1), ">", null);
            case ',':
                Next();
                return new SyntaxToken(TokenKind.Comma, new TextSpan(start, 1), ",", null);
            case ':':
                Next();
                return new SyntaxToken(TokenKind.Colon, new TextSpan(start, 1), ":", null);
            case '.':
                if (Peek(1) == '.' && Peek(2) == '.')
                {
                    position += 3;
                    return new SyntaxToken(TokenKind.Ellipsis, new TextSpan(start, 3), "...", null);
                }

                Next();
                return new SyntaxToken(TokenKind.Dot, new TextSpan(start, 1), ".", null);
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

        var hasDecimal = false;
        if (Current() == '.' && char.IsDigit(Peek(1)))
        {
            hasDecimal = true;
            Next();
            while (char.IsDigit(Current()))
            {
                Next();
            }
        }

        var text = sourceText.Text.Substring(start, position - start);
        if (!hasDecimal && int.TryParse(text, out var value))
        {
            return new SyntaxToken(TokenKind.NumberLiteral, new TextSpan(start, text.Length), text, value);
        }

        if (hasDecimal && double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var doubleValue))
        {
            return new SyntaxToken(TokenKind.NumberLiteral, new TextSpan(start, text.Length), text, doubleValue);
        }

        var span = new TextSpan(start, text.Length);
        diagnostics.Add(Diagnostic.Error(sourceText, span, "Number literal is invalid."));
        return new SyntaxToken(TokenKind.NumberLiteral, span, text, 0);
    }

    private void SkipWhitespace()
    {
        while (char.IsWhiteSpace(Current()) && !IsLineBreak(Current()))
        {
            Next();
        }
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
            "println" => TokenKind.PrintlnKeyword,
            "input" => TokenKind.InputKeyword,
            "let" => TokenKind.LetKeyword,
            "mut" => TokenKind.MutKeyword,
            "fn" => TokenKind.FnKeyword,
            "return" => TokenKind.ReturnKeyword,
            "type" => TokenKind.TypeKeyword,
            "match" => TokenKind.MatchKeyword,
            "when" => TokenKind.WhenKeyword,
            "scope" => TokenKind.ScopeKeyword,
            "spawn" => TokenKind.SpawnKeyword,
            "join" => TokenKind.JoinKeyword,
            "pub" => TokenKind.PubKeyword,
            "import" => TokenKind.ImportKeyword,
            "from" => TokenKind.FromKeyword,
            "as" => TokenKind.AsKeyword,
            "true" => TokenKind.TrueKeyword,
            "false" => TokenKind.FalseKeyword,
            "with" => TokenKind.WithKeyword,
            _ => TokenKind.Identifier
        };
    }

    private static bool IsIdentifierStart(char c) =>
        char.IsLetter(c) || c == '_';

    private static bool IsIdentifierPart(char c) =>
        char.IsLetterOrDigit(c) || c == '_';
}
