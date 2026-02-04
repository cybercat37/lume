using Axom.Compiler.Diagnostics;
using Axom.Compiler.Lexing;
using Axom.Compiler.Syntax;
using Axom.Compiler.Text;

namespace Axom.Compiler.Parsing;

public sealed class Parser
{
    private readonly SourceText sourceText;
    private readonly IReadOnlyList<SyntaxToken> tokens;
    private readonly List<Diagnostic> diagnostics;
    private int position;
    private static readonly HashSet<TokenKind> StatementStartTokens = new()
    {
        TokenKind.OpenBrace,
        TokenKind.LetKeyword,
        TokenKind.PrintKeyword,
        TokenKind.PrintlnKeyword,
        TokenKind.FnKeyword,
        TokenKind.ReturnKeyword,
        TokenKind.TypeKeyword,
        TokenKind.MatchKeyword,
        TokenKind.Identifier,
        TokenKind.InputKeyword,
        TokenKind.TrueKeyword,
        TokenKind.FalseKeyword,
        TokenKind.NumberLiteral,
        TokenKind.StringLiteral,
        TokenKind.OpenParen,
        TokenKind.Plus,
        TokenKind.Minus,
        TokenKind.Bang
    };

    private static readonly HashSet<TokenKind> BlockSyncTokens = new()
    {
        TokenKind.CloseBrace,
        TokenKind.NewLine,
        TokenKind.Semicolon
    };

    public Parser(SourceText sourceText, IReadOnlyList<SyntaxToken> tokens)
    {
        this.sourceText = sourceText;
        this.tokens = tokens;
        diagnostics = new List<Diagnostic>();
    }

    public IReadOnlyList<Diagnostic> Diagnostics => diagnostics;

    public CompilationUnitSyntax ParseCompilationUnit()
    {
        ConsumeSeparators();
        var statements = new List<StatementSyntax>();
        if (Current().Kind == TokenKind.EndOfFile)
        {
            diagnostics.Add(Diagnostic.Error(sourceText, Current().Span, "Expected statement."));
        }
        else
        {
            while (Current().Kind != TokenKind.EndOfFile)
            {
                var start = position;
                if (!IsStatementStart(Current().Kind))
                {
                    diagnostics.Add(Diagnostic.Error(sourceText, Current().Span, UnexpectedTokenMessage("statement", Current())));
                    SynchronizeStatement();
                }
                else
                {
                    var statement = ParseStatement();
                    statements.Add(statement);
                }
                ConsumeSeparators();

                if (position == start)
                {
                    NextToken();
                }
            }
        }

        var endOfFileToken = MatchToken(TokenKind.EndOfFile, "end of file");
        return new CompilationUnitSyntax(statements, endOfFileToken);
    }

    private StatementSyntax ParseStatement()
    {
        return Current().Kind switch
        {
            TokenKind.OpenBrace => ParseBlockStatement(),
            TokenKind.LetKeyword => ParseVariableDeclaration(),
            TokenKind.PrintKeyword => ParsePrintStatement(),
            TokenKind.PrintlnKeyword => ParsePrintStatement(),
            TokenKind.ReturnKeyword => ParseReturnStatement(),
            TokenKind.TypeKeyword => ParseTypeDeclaration(),
            TokenKind.FnKeyword when Peek(1).Kind == TokenKind.Identifier => ParseFunctionDeclaration(),
            _ => ParseExpressionStatement()
        };
    }

    private StatementSyntax ParseTypeDeclaration()
    {
        var typeKeyword = MatchToken(TokenKind.TypeKeyword, "type");
        var identifier = MatchToken(TokenKind.Identifier, "type name");
        var openBrace = MatchToken(TokenKind.OpenBrace, "{");
        var fields = new List<RecordFieldSyntax>();
        var variants = new List<SumVariantSyntax>();

        ConsumeSeparators();
        while (Current().Kind != TokenKind.CloseBrace && Current().Kind != TokenKind.EndOfFile)
        {
            var start = position;
            var entryIdentifier = MatchToken(TokenKind.Identifier, "field or variant name");
            if (Current().Kind == TokenKind.Colon)
            {
                var colonToken = MatchToken(TokenKind.Colon, ":");
                var fieldType = ParseTypeSyntax();
                fields.Add(new RecordFieldSyntax(entryIdentifier, colonToken, fieldType));
            }
            else if (Current().Kind == TokenKind.OpenParen)
            {
                var openParen = MatchToken(TokenKind.OpenParen, "(");
                var payloadType = ParseTypeSyntax();
                var closeParen = MatchToken(TokenKind.CloseParen, ")");
                variants.Add(new SumVariantSyntax(entryIdentifier, openParen, payloadType, closeParen));
            }
            else
            {
                variants.Add(new SumVariantSyntax(entryIdentifier, null, null, null));
            }

            if (Current().Kind == TokenKind.Comma)
            {
                NextToken();
            }

            ConsumeSeparators();
            if (position == start)
            {
                NextToken();
            }
        }

        var closeBrace = MatchToken(TokenKind.CloseBrace, "}");
        if (fields.Count > 0 && variants.Count > 0)
        {
            diagnostics.Add(Diagnostic.Error(
                sourceText,
                identifier.Span,
                "Type declarations cannot mix record fields and sum variants."));
        }

        if (fields.Count > 0)
        {
            return new RecordTypeDeclarationSyntax(typeKeyword, identifier, openBrace, fields, closeBrace);
        }

        return new SumTypeDeclarationSyntax(typeKeyword, identifier, openBrace, variants, closeBrace);
    }

    private StatementSyntax ParseFunctionDeclaration()
    {
        var fnKeyword = MatchToken(TokenKind.FnKeyword, "fn");
        var identifier = MatchToken(TokenKind.Identifier, "identifier");
        var openParen = MatchToken(TokenKind.OpenParen, "(");
        var parameters = ParseParameterList();
        var closeParen = MatchToken(TokenKind.CloseParen, ")");

        SyntaxToken? returnArrow = null;
        TypeSyntax? returnType = null;
        if (Current().Kind == TokenKind.ArrowType)
        {
            returnArrow = NextToken();
            returnType = ParseTypeSyntax();
        }

        if (Current().Kind == TokenKind.Arrow)
        {
            var arrowToken = NextToken();
            var expressionBody = ParseExpression();
            return new FunctionDeclarationSyntax(
                fnKeyword,
                identifier,
                openParen,
                parameters,
                closeParen,
                returnArrow,
                returnType,
                arrowToken,
                null,
                expressionBody);
        }

        var bodyBlock = ParseBlockStatement();
        return new FunctionDeclarationSyntax(
            fnKeyword,
            identifier,
            openParen,
            parameters,
            closeParen,
            returnArrow,
            returnType,
            null,
            (BlockStatementSyntax)bodyBlock,
            null);
    }

    private StatementSyntax ParseReturnStatement()
    {
        var returnKeyword = MatchToken(TokenKind.ReturnKeyword, "return");
        ExpressionSyntax? expression = null;
        if (!IsStatementTerminator(Current().Kind))
        {
            expression = ParseExpression();
        }

        if (Current().Kind == TokenKind.Semicolon)
        {
            NextToken();
        }

        return new ReturnStatementSyntax(returnKeyword, expression);
    }

    private StatementSyntax ParsePrintStatement()
    {
        var keywordToken = Current().Kind == TokenKind.PrintlnKeyword
            ? MatchToken(TokenKind.PrintlnKeyword, "println")
            : MatchToken(TokenKind.PrintKeyword, "print");
        var expression = ParseExpression();
        if (!IsStatementTerminator(Current().Kind))
        {
            diagnostics.Add(Diagnostic.Error(sourceText, Current().Span, "Unexpected token after print statement."));
            SynchronizeStatement();
        }

        return new PrintStatementSyntax(keywordToken, expression);
    }

    private StatementSyntax ParseVariableDeclaration()
    {
        var letKeyword = MatchToken(TokenKind.LetKeyword, "let");
        SyntaxToken? mutKeyword = null;
        if (Current().Kind == TokenKind.MutKeyword)
        {
            mutKeyword = NextToken();
        }

        PatternSyntax pattern;
        if (Current().Kind == TokenKind.OpenParen)
        {
            pattern = ParseTuplePattern();
        }
        else
        {
            var identifier = MatchToken(TokenKind.Identifier, "identifier");
            if (identifier.Text == "_")
            {
                pattern = new WildcardPatternSyntax(identifier);
            }
            else
            {
                pattern = new IdentifierPatternSyntax(identifier);
            }
        }

        var equalsToken = MatchToken(TokenKind.EqualsToken, "=");
        var initializer = ParseExpression();
        SyntaxToken? semicolonToken = null;
        if (Current().Kind == TokenKind.Semicolon)
        {
            semicolonToken = NextToken();
        }

        return new VariableDeclarationSyntax(
            letKeyword,
            mutKeyword,
            pattern,
            equalsToken,
            initializer,
            semicolonToken);
    }

    private StatementSyntax ParseExpressionStatement()
    {
        var expression = ParseExpression();
        if (Current().Kind == TokenKind.Semicolon)
        {
            NextToken();
        }
        return new ExpressionStatementSyntax(expression);
    }

    private StatementSyntax ParseBlockStatement()
    {
        var openBrace = MatchToken(TokenKind.OpenBrace, "{");
        var statements = new List<StatementSyntax>();

        ConsumeSeparators();
        while (Current().Kind != TokenKind.CloseBrace && Current().Kind != TokenKind.EndOfFile)
        {
            var start = position;
            if (!IsStatementStart(Current().Kind))
            {
                diagnostics.Add(Diagnostic.Error(sourceText, Current().Span, UnexpectedTokenMessage("statement", Current())));
                SynchronizeBlockStatement();
            }
            else
            {
                var statement = ParseStatement();
                statements.Add(statement);
            }
            ConsumeSeparators();

            if (position == start)
            {
                NextToken();
            }
        }

        var closeBrace = MatchToken(TokenKind.CloseBrace, "}");
        return new BlockStatementSyntax(openBrace, statements, closeBrace);
    }

    private ExpressionSyntax ParseExpression(bool allowRecordLiteral = true) =>
        ParseAssignmentExpression(allowRecordLiteral);

    private ExpressionSyntax ParseAssignmentExpression(bool allowRecordLiteral)
    {
        if (Current().Kind == TokenKind.Identifier && Peek(1).Kind == TokenKind.EqualsToken)
        {
            var identifier = NextToken();
            var equalsToken = NextToken();
            var right = ParseAssignmentExpression(allowRecordLiteral);
            return new AssignmentExpressionSyntax(identifier, equalsToken, right);
        }

        return ParseBinaryExpression(allowRecordLiteral);
    }

    private ExpressionSyntax ParseBinaryExpression(bool allowRecordLiteral, int parentPrecedence = 0)
    {
        ExpressionSyntax left;
        var unaryPrecedence = GetUnaryOperatorPrecedence(Current().Kind);
        if (unaryPrecedence != 0 && unaryPrecedence >= parentPrecedence)
        {
            var operatorToken = NextToken();
            var operand = ParseBinaryExpression(allowRecordLiteral, unaryPrecedence);
            left = new UnaryExpressionSyntax(operatorToken, operand);
        }
        else
        {
            left = ParsePostfixExpression(allowRecordLiteral);
        }

        while (true)
        {
            var precedence = GetBinaryOperatorPrecedence(Current().Kind);
            if (precedence == 0 || precedence <= parentPrecedence)
            {
                break;
            }

            var operatorToken = NextToken();
            var right = ParseBinaryExpression(allowRecordLiteral, precedence);
            left = new BinaryExpressionSyntax(left, operatorToken, right);
        }

        return left;
    }

    private ExpressionSyntax ParsePostfixExpression(bool allowRecordLiteral)
    {
        var expression = ParsePrimaryExpression(allowRecordLiteral);
        while (Current().Kind == TokenKind.OpenParen || Current().Kind == TokenKind.Dot)
        {
            if (Current().Kind == TokenKind.OpenParen)
            {
                expression = ParseCallExpression(expression);
                continue;
            }

            var dotToken = MatchToken(TokenKind.Dot, ".");
            var identifierToken = MatchToken(TokenKind.Identifier, "field name");
            expression = new FieldAccessExpressionSyntax(expression, dotToken, identifierToken);
        }

        return expression;
    }

    private ExpressionSyntax ParsePrimaryExpression(bool allowRecordLiteral)
    {
        switch (Current().Kind)
        {
            case TokenKind.OpenParen:
                var openParen = NextToken();
                var expression = ParseExpression();
                if (Current().Kind == TokenKind.Comma)
                {
                    var elements = new List<ExpressionSyntax> { expression };
                    while (Current().Kind == TokenKind.Comma)
                    {
                        NextToken();
                        elements.Add(ParseExpression());
                    }

                    var tupleCloseParen = MatchToken(TokenKind.CloseParen, ")");
                    return new TupleExpressionSyntax(openParen, elements, tupleCloseParen);
                }

                var closeParen = MatchToken(TokenKind.CloseParen, ")");
                return new ParenthesizedExpressionSyntax(openParen, expression, closeParen);
            case TokenKind.TrueKeyword:
            case TokenKind.FalseKeyword:
            case TokenKind.NumberLiteral:
            case TokenKind.StringLiteral:
                return new LiteralExpressionSyntax(NextToken());
            case TokenKind.InputKeyword:
                if (Peek(1).Kind == TokenKind.OpenParen)
                {
                    return new NameExpressionSyntax(NextToken());
                }

                return new InputExpressionSyntax(NextToken());
            case TokenKind.Identifier:
                if (allowRecordLiteral && Peek(1).Kind == TokenKind.OpenBrace)
                {
                    return ParseRecordLiteralExpression();
                }

                return new NameExpressionSyntax(NextToken());
            case TokenKind.FnKeyword:
                return ParseLambdaExpression();
            case TokenKind.MatchKeyword:
                return ParseMatchExpression();
            default:
                diagnostics.Add(Diagnostic.Error(sourceText, Current().Span, UnexpectedTokenMessage("expression", Current())));
                var missing = SyntaxToken.Missing(TokenKind.NumberLiteral, Current().Position);
                if (Current().Kind != TokenKind.EndOfFile)
                {
                    NextToken();
                }
                return new LiteralExpressionSyntax(missing);
        }
    }

    private ExpressionSyntax ParseMatchExpression()
    {
        var matchKeyword = MatchToken(TokenKind.MatchKeyword, "match");
        var expression = ParseExpression(allowRecordLiteral: false);
        var openBrace = MatchToken(TokenKind.OpenBrace, "{");
        var arms = new List<MatchArmSyntax>();

        ConsumeSeparators();
        while (Current().Kind != TokenKind.CloseBrace && Current().Kind != TokenKind.EndOfFile)
        {
            var start = position;
            var pattern = ParsePattern();
            var arrowToken = MatchToken(TokenKind.ArrowType, "->");
            var armExpression = ParseExpression();
            arms.Add(new MatchArmSyntax(pattern, arrowToken, armExpression));
            ConsumeSeparators();

            if (position == start)
            {
                NextToken();
            }
        }

        var closeBrace = MatchToken(TokenKind.CloseBrace, "}");
        return new MatchExpressionSyntax(matchKeyword, expression, openBrace, arms, closeBrace);
    }

    private ExpressionSyntax ParseRecordLiteralExpression()
    {
        var identifier = MatchToken(TokenKind.Identifier, "type name");
        var openBrace = MatchToken(TokenKind.OpenBrace, "{");
        var fields = new List<RecordFieldAssignmentSyntax>();

        ConsumeSeparators();
        while (Current().Kind != TokenKind.CloseBrace && Current().Kind != TokenKind.EndOfFile)
        {
            var start = position;
            var fieldIdentifier = MatchToken(TokenKind.Identifier, "field name");
            var colonToken = MatchToken(TokenKind.Colon, ":");
            var expression = ParseExpression();
            fields.Add(new RecordFieldAssignmentSyntax(fieldIdentifier, colonToken, expression));

            if (Current().Kind == TokenKind.Comma)
            {
                NextToken();
            }

            ConsumeSeparators();
            if (position == start)
            {
                NextToken();
            }
        }

        var closeBrace = MatchToken(TokenKind.CloseBrace, "}");
        return new RecordLiteralExpressionSyntax(identifier, openBrace, fields, closeBrace);
    }

    private PatternSyntax ParsePattern()
    {
        switch (Current().Kind)
        {
            case TokenKind.TrueKeyword:
            case TokenKind.FalseKeyword:
            case TokenKind.NumberLiteral:
            case TokenKind.StringLiteral:
                return new LiteralPatternSyntax(NextToken());
            case TokenKind.OpenParen:
                return ParseTuplePattern();
            case TokenKind.Identifier:
                var identifier = NextToken();
                if (identifier.Text == "_")
                {
                    return new WildcardPatternSyntax(identifier);
                }

                if (Current().Kind == TokenKind.OpenParen)
                {
                    return ParseVariantPattern(identifier);
                }

                if (Current().Kind == TokenKind.OpenBrace)
                {
                    return ParseRecordPattern(identifier);
                }

                return new IdentifierPatternSyntax(identifier);
            default:
                diagnostics.Add(Diagnostic.Error(sourceText, Current().Span, UnexpectedTokenMessage("pattern", Current())));
                var missing = SyntaxToken.Missing(TokenKind.Identifier, Current().Position);
                if (Current().Kind != TokenKind.EndOfFile)
                {
                    NextToken();
                }
                return new WildcardPatternSyntax(missing);
        }
    }

    private PatternSyntax ParseTuplePattern()
    {
        var openParen = MatchToken(TokenKind.OpenParen, "(");
        var first = ParsePattern();
        if (Current().Kind != TokenKind.Comma)
        {
            var closeParen = MatchToken(TokenKind.CloseParen, ")");
            return first is TuplePatternSyntax
                ? new TuplePatternSyntax(openParen, ((TuplePatternSyntax)first).Elements, closeParen)
                : first;
        }

        var elements = new List<PatternSyntax> { first };
        while (Current().Kind == TokenKind.Comma)
        {
            NextToken();
            elements.Add(ParsePattern());
        }

        var close = MatchToken(TokenKind.CloseParen, ")");
        return new TuplePatternSyntax(openParen, elements, close);
    }

    private PatternSyntax ParseVariantPattern(SyntaxToken identifier)
    {
        var openParen = MatchToken(TokenKind.OpenParen, "(");
        var payload = ParsePattern();
        if (Current().Kind == TokenKind.Comma)
        {
            diagnostics.Add(Diagnostic.Error(sourceText, Current().Span, "Sum type variants support a single payload pattern."));
            while (Current().Kind == TokenKind.Comma)
            {
                NextToken();
                ParsePattern();
            }
        }
        var closeParen = MatchToken(TokenKind.CloseParen, ")");
        return new VariantPatternSyntax(identifier, openParen, payload, closeParen);
    }

    private PatternSyntax ParseRecordPattern(SyntaxToken identifier)
    {
        var openBrace = MatchToken(TokenKind.OpenBrace, "{");
        var fields = new List<RecordFieldPatternSyntax>();

        ConsumeSeparators();
        while (Current().Kind != TokenKind.CloseBrace && Current().Kind != TokenKind.EndOfFile)
        {
            var start = position;
            var fieldIdentifier = MatchToken(TokenKind.Identifier, "identifier");
            var colonToken = MatchToken(TokenKind.Colon, ":");
            var pattern = ParsePattern();
            fields.Add(new RecordFieldPatternSyntax(fieldIdentifier, colonToken, pattern));

            if (Current().Kind == TokenKind.Comma)
            {
                NextToken();
            }

            ConsumeSeparators();
            if (position == start)
            {
                NextToken();
            }
        }

        var closeBrace = MatchToken(TokenKind.CloseBrace, "}");
        return new RecordPatternSyntax(identifier, openBrace, fields, closeBrace);
    }

    private ExpressionSyntax ParseCallExpression(ExpressionSyntax callee)
    {
        var openParen = MatchToken(TokenKind.OpenParen, "(");
        var arguments = new List<ExpressionSyntax>();
        if (Current().Kind != TokenKind.CloseParen)
        {
            do
            {
                var expression = ParseExpression();
                arguments.Add(expression);
                if (Current().Kind != TokenKind.Comma)
                {
                    break;
                }

                NextToken();
            } while (Current().Kind != TokenKind.CloseParen && Current().Kind != TokenKind.EndOfFile);
        }

        var closeParen = MatchToken(TokenKind.CloseParen, ")");
        return new CallExpressionSyntax(callee, openParen, arguments, closeParen);
    }

    private ExpressionSyntax ParseLambdaExpression()
    {
        var fnKeyword = MatchToken(TokenKind.FnKeyword, "fn");
        var openParen = MatchToken(TokenKind.OpenParen, "(");
        var parameters = ParseParameterList();
        var closeParen = MatchToken(TokenKind.CloseParen, ")");

        if (Current().Kind == TokenKind.Arrow)
        {
            var arrowToken = NextToken();
            var expressionBody = ParseExpression();
            return new LambdaExpressionSyntax(fnKeyword, openParen, parameters, closeParen, arrowToken, null, expressionBody);
        }

        var bodyBlock = ParseBlockStatement();
        return new LambdaExpressionSyntax(fnKeyword, openParen, parameters, closeParen, null, (BlockStatementSyntax)bodyBlock, null);
    }

    private List<FunctionParameterSyntax> ParseParameterList()
    {
        var parameters = new List<FunctionParameterSyntax>();
        if (Current().Kind == TokenKind.CloseParen)
        {
            return parameters;
        }

        do
        {
            var identifier = MatchToken(TokenKind.Identifier, "identifier");
            var colon = MatchToken(TokenKind.Colon, ":");
            var type = ParseTypeSyntax();
            parameters.Add(new FunctionParameterSyntax(identifier, colon, type));

            if (Current().Kind != TokenKind.Comma)
            {
                break;
            }

            NextToken();
        } while (Current().Kind != TokenKind.CloseParen && Current().Kind != TokenKind.EndOfFile);

        return parameters;
    }

    private TypeSyntax ParseTypeSyntax()
    {
        if (Current().Kind == TokenKind.OpenParen)
        {
            return ParseTupleTypeSyntax();
        }

        var identifier = MatchToken(TokenKind.Identifier, "type name");
        return new NameTypeSyntax(identifier);
    }

    private TypeSyntax ParseTupleTypeSyntax()
    {
        var openParen = MatchToken(TokenKind.OpenParen, "(");
        var first = ParseTypeSyntax();
        if (Current().Kind != TokenKind.Comma)
        {
            var closeParen = MatchToken(TokenKind.CloseParen, ")");
            return first is TupleTypeSyntax
                ? new TupleTypeSyntax(openParen, ((TupleTypeSyntax)first).Elements, closeParen)
                : first;
        }

        var elements = new List<TypeSyntax> { first };
        while (Current().Kind == TokenKind.Comma)
        {
            NextToken();
            elements.Add(ParseTypeSyntax());
        }

        var close = MatchToken(TokenKind.CloseParen, ")");
        return new TupleTypeSyntax(openParen, elements, close);
    }

    private void ConsumeSeparators()
    {
        while (Current().Kind == TokenKind.NewLine || Current().Kind == TokenKind.Semicolon)
        {
            NextToken();
        }
    }

    private SyntaxToken MatchToken(TokenKind kind, string expectedDescription)
    {
        if (Current().Kind == kind)
        {
            return NextToken();
        }

        var current = Current();
        diagnostics.Add(Diagnostic.Error(
            sourceText,
            current.Span,
            UnexpectedTokenMessage(expectedDescription, current)));

        return SyntaxToken.Missing(kind, current.Position);
    }

    private SyntaxToken Current() => Peek(0);

    private SyntaxToken Peek(int offset)
    {
        var index = position + offset;
        if (index >= tokens.Count)
        {
            return tokens[^1];
        }

        return tokens[index];
    }

    private SyntaxToken NextToken()
    {
        var current = Current();
        position++;
        return current;
    }

    private static bool IsStatementStart(TokenKind kind) =>
        StatementStartTokens.Contains(kind);

    private void SynchronizeStatement()
    {
        while (Current().Kind != TokenKind.EndOfFile && !IsStatementStart(Current().Kind))
        {
            if (Current().Kind == TokenKind.NewLine || Current().Kind == TokenKind.Semicolon)
            {
                ConsumeSeparators();
                return;
            }

            NextToken();
        }
    }

    private void SynchronizeBlockStatement()
    {
        while (Current().Kind != TokenKind.EndOfFile)
        {
            if (BlockSyncTokens.Contains(Current().Kind) || IsStatementStart(Current().Kind))
            {
                return;
            }

            NextToken();
        }
    }

    private static string UnexpectedTokenMessage(string expectedDescription, SyntaxToken actual)
    {
        var tokenText = string.IsNullOrEmpty(actual.Text) ? actual.Kind.ToString() : actual.Text;
        return $"Expected {expectedDescription}, found '{tokenText}'.";
    }

    private static bool IsStatementTerminator(TokenKind kind) =>
        kind == TokenKind.NewLine ||
        kind == TokenKind.Semicolon ||
        kind == TokenKind.EndOfFile ||
        kind == TokenKind.CloseBrace;

    private static int GetBinaryOperatorPrecedence(TokenKind kind)
    {
        return kind switch
        {
            TokenKind.Star => 5,
            TokenKind.Slash => 5,
            TokenKind.Plus => 4,
            TokenKind.Minus => 4,
            TokenKind.EqualEqual => 3,
            TokenKind.BangEqual => 3,
            TokenKind.Less => 3,
            TokenKind.LessOrEqual => 3,
            TokenKind.Greater => 3,
            TokenKind.GreaterOrEqual => 3,
            TokenKind.AmpersandAmpersand => 2,
            TokenKind.PipePipe => 1,
            _ => 0
        };
    }

    private static int GetUnaryOperatorPrecedence(TokenKind kind)
    {
        return kind switch
        {
            TokenKind.Plus => 6,
            TokenKind.Minus => 6,
            TokenKind.Bang => 6,
            _ => 0
        };
    }
}
