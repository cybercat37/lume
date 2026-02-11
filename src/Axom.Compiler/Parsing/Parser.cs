using Axom.Compiler.Diagnostics;
using Axom.Compiler.Lexing;
using Axom.Compiler.Syntax;
using Axom.Compiler.Text;
using System.Text;

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
        TokenKind.PubKeyword,
        TokenKind.ImportKeyword,
        TokenKind.FromKeyword,
        TokenKind.At,
        TokenKind.ScopeKeyword,
        TokenKind.MatchKeyword,
        TokenKind.Identifier,
        TokenKind.InputKeyword,
        TokenKind.TrueKeyword,
        TokenKind.FalseKeyword,
        TokenKind.NumberLiteral,
        TokenKind.StringLiteral,
        TokenKind.OpenParen,
        TokenKind.OpenBracket,
        TokenKind.SpawnKeyword,
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
        if (Current().Kind == TokenKind.At)
        {
            return ParseAnnotatedStatement();
        }

        return Current().Kind switch
        {
            TokenKind.OpenBrace => ParseBlockStatement(),
            TokenKind.LetKeyword => ParseVariableDeclaration(),
            TokenKind.ScopeKeyword => ParseScopeStatement(),
            TokenKind.PrintKeyword => ParsePrintStatement(),
            TokenKind.PrintlnKeyword => ParsePrintStatement(),
            TokenKind.ReturnKeyword => ParseReturnStatement(),
            TokenKind.TypeKeyword => ParseTypeDeclaration(),
            TokenKind.PubKeyword => ParsePubStatement(),
            TokenKind.ImportKeyword => ParseImportStatement(),
            TokenKind.FromKeyword => ParseFromImportStatement(),
            TokenKind.FnKeyword when Peek(1).Kind == TokenKind.Identifier => ParseFunctionDeclaration(),
            _ => ParseExpressionStatement()
        };
    }

    private StatementSyntax ParseAnnotatedStatement()
    {
        var intentAnnotation = ParseIntentAnnotation();
        ConsumeSeparators();
        return Current().Kind switch
        {
            TokenKind.LetKeyword => ParseVariableDeclaration(intentAnnotation),
            TokenKind.OpenBrace => ParseBlockStatement(intentAnnotation),
            _ => ParseInvalidIntentTarget(intentAnnotation)
        };
    }

    private StatementSyntax ParseInvalidIntentTarget(IntentAnnotationSyntax intentAnnotation)
    {
        diagnostics.Add(Diagnostic.Error(
            sourceText,
            intentAnnotation.Span,
            "@intent can only be applied to block statements and let declarations."));

        return ParseStatement();
    }

    private IntentAnnotationSyntax ParseIntentAnnotation()
    {
        var atToken = MatchToken(TokenKind.At, "@");
        var intentIdentifier = MatchToken(TokenKind.Identifier, "intent");
        if (!string.Equals(intentIdentifier.Text, "intent", StringComparison.Ordinal))
        {
            diagnostics.Add(Diagnostic.Error(sourceText, intentIdentifier.Span, "Only @intent annotations are supported."));
        }

        var openParenToken = MatchToken(TokenKind.OpenParen, "(");
        var messageToken = MatchToken(TokenKind.StringLiteral, "string literal");
        var closeParenToken = MatchToken(TokenKind.CloseParen, ")");
        return new IntentAnnotationSyntax(atToken, intentIdentifier, openParenToken, messageToken, closeParenToken);
    }

    private StatementSyntax ParsePubStatement()
    {
        var pubKeyword = MatchToken(TokenKind.PubKeyword, "pub");
        StatementSyntax declaration = Current().Kind switch
        {
            TokenKind.FnKeyword => ParseFunctionDeclaration(),
            TokenKind.TypeKeyword => ParseTypeDeclaration(),
            TokenKind.LetKeyword => ParseVariableDeclaration(),
            _ => ParseInvalidPubTarget(pubKeyword)
        };

        return new PubStatementSyntax(pubKeyword, declaration);
    }

    private StatementSyntax ParseInvalidPubTarget(SyntaxToken pubKeyword)
    {
        diagnostics.Add(Diagnostic.Error(
            sourceText,
            pubKeyword.Span,
            "pub can only be applied to top-level fn/type/let declarations."));
        return ParseExpressionStatement();
    }

    private StatementSyntax ParseImportStatement()
    {
        var importKeyword = MatchToken(TokenKind.ImportKeyword, "import");
        var moduleParts = ParseModulePath();
        SyntaxToken? asKeyword = null;
        SyntaxToken? alias = null;
        if (Current().Kind == TokenKind.AsKeyword)
        {
            asKeyword = MatchToken(TokenKind.AsKeyword, "as");
            alias = MatchToken(TokenKind.Identifier, "alias");
        }

        return new ImportStatementSyntax(importKeyword, moduleParts, asKeyword, alias);
    }

    private StatementSyntax ParseFromImportStatement()
    {
        var fromKeyword = MatchToken(TokenKind.FromKeyword, "from");
        var moduleParts = ParseModulePath();
        var importKeyword = MatchToken(TokenKind.ImportKeyword, "import");
        var specifiers = new List<ImportSpecifierSyntax>();

        if (Current().Kind == TokenKind.Star)
        {
            var star = MatchToken(TokenKind.Star, "*");
            specifiers.Add(new ImportSpecifierSyntax(star, null, null));
        }
        else
        {
            while (Current().Kind != TokenKind.EndOfFile && Current().Kind != TokenKind.NewLine && Current().Kind != TokenKind.Semicolon)
            {
                var name = MatchToken(TokenKind.Identifier, "symbol name");
                SyntaxToken? asKeyword = null;
                SyntaxToken? alias = null;
                if (Current().Kind == TokenKind.AsKeyword)
                {
                    asKeyword = MatchToken(TokenKind.AsKeyword, "as");
                    alias = MatchToken(TokenKind.Identifier, "alias");
                }

                specifiers.Add(new ImportSpecifierSyntax(name, asKeyword, alias));
                if (Current().Kind == TokenKind.Comma)
                {
                    NextToken();
                    continue;
                }

                break;
            }
        }

        return new FromImportStatementSyntax(fromKeyword, moduleParts, importKeyword, specifiers);
    }

    private IReadOnlyList<SyntaxToken> ParseModulePath()
    {
        var parts = new List<SyntaxToken>();
        parts.Add(MatchToken(TokenKind.Identifier, "module name"));
        while (Current().Kind == TokenKind.Dot)
        {
            NextToken();
            parts.Add(MatchToken(TokenKind.Identifier, "module name"));
        }

        return parts;
    }

    private StatementSyntax ParseScopeStatement()
    {
        var scopeKeyword = MatchToken(TokenKind.ScopeKeyword, "scope");
        var body = (BlockStatementSyntax)ParseBlockStatement();
        return new ScopeStatementSyntax(scopeKeyword, body);
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
        SyntaxToken? typeParameterOpenToken = null;
        SyntaxToken? typeParameterCloseToken = null;
        var typeParameters = Array.Empty<SyntaxToken>();
        if (Current().Kind == TokenKind.Less && Peek(1).Kind == TokenKind.Identifier)
        {
            typeParameterOpenToken = MatchToken(TokenKind.Less, "<");
            var typeParameterList = new List<SyntaxToken>();
            while (Current().Kind != TokenKind.Greater && Current().Kind != TokenKind.EndOfFile)
            {
                var typeParam = MatchToken(TokenKind.Identifier, "type parameter");
                typeParameterList.Add(typeParam);
                if (Current().Kind == TokenKind.Comma)
                {
                    NextToken();
                }
                else
                {
                    break;
                }
            }
            typeParameterCloseToken = MatchToken(TokenKind.Greater, ">");
            typeParameters = typeParameterList.ToArray();
        }
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
                typeParameterOpenToken,
                typeParameters,
                typeParameterCloseToken,
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
            typeParameterOpenToken,
            typeParameters,
            typeParameterCloseToken,
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

    private StatementSyntax ParseVariableDeclaration(IntentAnnotationSyntax? intentAnnotation = null)
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
            intentAnnotation,
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

    private StatementSyntax ParseBlockStatement(IntentAnnotationSyntax? intentAnnotation = null)
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
        return new BlockStatementSyntax(intentAnnotation, openBrace, statements, closeBrace);
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
            left = operatorToken.Kind == TokenKind.PipeGreater
                ? new PipelineExpressionSyntax(left, operatorToken, right)
                : new BinaryExpressionSyntax(left, operatorToken, right);
        }

        return left;
    }

    private ExpressionSyntax ParsePostfixExpression(bool allowRecordLiteral)
    {
        var expression = ParsePrimaryExpression(allowRecordLiteral);
        while (Current().Kind == TokenKind.OpenParen
            || Current().Kind == TokenKind.Dot
            || Current().Kind == TokenKind.OpenBracket
            || Current().Kind == TokenKind.QuestionToken
            || Current().Kind == TokenKind.WithKeyword
            || IsGenericCallStart())
        {
            if (Current().Kind == TokenKind.OpenParen)
            {
                expression = ParseCallExpression(expression);
                continue;
            }

            if (IsGenericCallStart())
            {
                expression = ParseGenericCallExpression(expression);
                continue;
            }

            if (Current().Kind == TokenKind.OpenBracket)
            {
                var openBracket = MatchToken(TokenKind.OpenBracket, "[");
                var index = ParseExpression();
                var closeBracket = MatchToken(TokenKind.CloseBracket, "]");
                expression = new IndexExpressionSyntax(expression, openBracket, index, closeBracket);
                continue;
            }

            if (Current().Kind == TokenKind.QuestionToken)
            {
                var questionToken = MatchToken(TokenKind.QuestionToken, "?");
                expression = new QuestionExpressionSyntax(expression, questionToken);
                continue;
            }

            if (Current().Kind == TokenKind.WithKeyword)
            {
                expression = ParseRecordUpdateExpression(expression);
                continue;
            }

            var dotToken = MatchToken(TokenKind.Dot, ".");
            SyntaxToken identifierToken;
            if (Current().Kind == TokenKind.JoinKeyword)
            {
                identifierToken = NextToken();
            }
            else
            {
                identifierToken = MatchToken(TokenKind.Identifier, "field name");
            }
            expression = new FieldAccessExpressionSyntax(expression, dotToken, identifierToken);
        }

        return expression;
    }

    private ExpressionSyntax ParseRecordUpdateExpression(ExpressionSyntax target)
    {
        var withKeyword = MatchToken(TokenKind.WithKeyword, "with");
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
        return new RecordUpdateExpressionSyntax(target, withKeyword, openBrace, fields, closeBrace);
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
            case TokenKind.SpawnKeyword:
                var spawnKeyword = MatchToken(TokenKind.SpawnKeyword, "spawn");
                var spawnBody = (BlockStatementSyntax)ParseBlockStatement();
                return new SpawnExpressionSyntax(spawnKeyword, spawnBody);
            case TokenKind.OpenBracket:
                return ParseListOrMapExpression();
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
                if (string.Equals(Current().Text, "f", StringComparison.Ordinal) && Peek(1).Kind == TokenKind.StringLiteral)
                {
                    return ParseInterpolatedStringExpression();
                }

                if (Peek(1).Kind == TokenKind.ArrowType)
                {
                    return ParseShorthandLambdaExpression();
                }

                if (string.Equals(Current().Text, "channel", StringComparison.Ordinal) &&
                    Peek(1).Kind == TokenKind.Less)
                {
                    return ParseChannelExpression();
                }

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

    private ExpressionSyntax ParseShorthandLambdaExpression()
    {
        var parameter = MatchToken(TokenKind.Identifier, "identifier");
        var arrowToken = MatchToken(TokenKind.ArrowType, "->");
        var body = ParseExpression(allowRecordLiteral: false);
        return new ShorthandLambdaExpressionSyntax(parameter, arrowToken, body);
    }

    private ExpressionSyntax ParseChannelExpression()
    {
        var channelIdentifier = MatchToken(TokenKind.Identifier, "channel");
        var lessToken = MatchToken(TokenKind.Less, "<");
        var elementType = ParseTypeSyntax();
        var greaterToken = MatchToken(TokenKind.Greater, ">");
        var openParenToken = MatchToken(TokenKind.OpenParen, "(");
        ExpressionSyntax? capacityExpression = null;
        if (Current().Kind != TokenKind.CloseParen)
        {
            capacityExpression = ParseExpression();
        }

        var closeParenToken = MatchToken(TokenKind.CloseParen, ")");
        return new ChannelExpressionSyntax(channelIdentifier, lessToken, elementType, greaterToken, openParenToken, capacityExpression, closeParenToken);
    }

    private ExpressionSyntax ParseInterpolatedStringExpression()
    {
        var prefixToken = MatchToken(TokenKind.Identifier, "f");
        var stringToken = MatchToken(TokenKind.StringLiteral, "string literal");
        var content = stringToken.Value as string ?? string.Empty;

        var segments = ParseInterpolatedSegments(content, stringToken, prefixToken.Span.Start);
        if (segments.Count == 0)
        {
            return new LiteralExpressionSyntax(new SyntaxToken(
                TokenKind.StringLiteral,
                stringToken.Span,
                stringToken.Text,
                string.Empty));
        }

        var plusToken = new SyntaxToken(TokenKind.Plus, stringToken.Span, "+", null);
        var combined = segments[0];
        for (var i = 1; i < segments.Count; i++)
        {
            combined = new BinaryExpressionSyntax(combined, plusToken, segments[i]);
        }

        return combined;
    }

    private List<ExpressionSyntax> ParseInterpolatedSegments(string content, SyntaxToken stringToken, int positionHint)
    {
        var segments = new List<ExpressionSyntax>();
        var literalBuilder = new StringBuilder();
        var sawInterpolation = false;
        var index = 0;

        while (index < content.Length)
        {
            var current = content[index];
            if (current == '{')
            {
                if (index + 1 < content.Length && content[index + 1] == '{')
                {
                    literalBuilder.Append('{');
                    index += 2;
                    continue;
                }

                FlushLiteralSegment(segments, literalBuilder, stringToken, positionHint);

                if (!TryParseInterpolationHole(content, index, stringToken, positionHint, out var interpolationExpression, out var nextIndex))
                {
                    break;
                }

                segments.Add(interpolationExpression);

                sawInterpolation = true;
                index = nextIndex;
                continue;
            }

            if (current == '}')
            {
                if (index + 1 < content.Length && content[index + 1] == '}')
                {
                    literalBuilder.Append('}');
                    index += 2;
                    continue;
                }

                diagnostics.Add(CreateInterpolationDiagnostic(stringToken, index, "Interpolated string contains an unexpected '}'."));
                index++;
                continue;
            }

            literalBuilder.Append(current);
            index++;
        }

        FlushLiteralSegment(segments, literalBuilder, stringToken, positionHint);

        if (!sawInterpolation)
        {
            return segments;
        }

        if (segments.Count == 0)
        {
            segments.Add(CreateStringLiteralExpression(string.Empty, stringToken, positionHint));
        }

        return segments;
    }

    private bool TryParseInterpolationHole(
        string content,
        int openBraceIndex,
        SyntaxToken stringToken,
        int positionHint,
        out ExpressionSyntax interpolationExpression,
        out int nextIndex)
    {
        interpolationExpression = CreateStringLiteralExpression(string.Empty, stringToken, positionHint);
        nextIndex = openBraceIndex + 1;

        var parenDepth = 0;
        var bracketDepth = 0;
        var braceDepth = 0;
        var inString = false;
        var escape = false;
        int? formatSeparatorIndex = null;

        for (var index = openBraceIndex + 1; index < content.Length; index++)
        {
            var current = content[index];
            if (inString)
            {
                if (escape)
                {
                    escape = false;
                    continue;
                }

                if (current == '\\')
                {
                    escape = true;
                    continue;
                }

                if (current == '"')
                {
                    inString = false;
                }

                continue;
            }

            switch (current)
            {
                case '"':
                    inString = true;
                    continue;
                case '(':
                    parenDepth++;
                    continue;
                case ')':
                    if (parenDepth > 0)
                    {
                        parenDepth--;
                    }
                    continue;
                case '[':
                    bracketDepth++;
                    continue;
                case ']':
                    if (bracketDepth > 0)
                    {
                        bracketDepth--;
                    }
                    continue;
                case '{':
                    braceDepth++;
                    continue;
                case ':':
                    if (parenDepth == 0 && bracketDepth == 0 && braceDepth == 0 && formatSeparatorIndex is null)
                    {
                        formatSeparatorIndex = index;
                    }
                    continue;
                case '}':
                    if (parenDepth == 0 && bracketDepth == 0 && braceDepth == 0)
                    {
                        var rawExpressionStart = openBraceIndex + 1;
                        var rawExpressionEnd = formatSeparatorIndex ?? index;
                        var rawExpression = content.Substring(rawExpressionStart, rawExpressionEnd - rawExpressionStart).Trim();
                        var formatSpecifier = formatSeparatorIndex is null
                            ? null
                            : content.Substring(formatSeparatorIndex.Value + 1, index - formatSeparatorIndex.Value - 1).Trim();

                        if (string.IsNullOrEmpty(rawExpression))
                        {
                            diagnostics.Add(CreateInterpolationDiagnostic(stringToken, openBraceIndex, "Interpolated string expression cannot be empty."));
                        }
                        else
                        {
                            var parsedExpression = ParseInterpolatedExpression(rawExpression, stringToken, openBraceIndex, positionHint);
                            interpolationExpression = string.IsNullOrEmpty(formatSpecifier)
                                ? WrapStringifyCall(parsedExpression, positionHint)
                                : WrapFormatCall(parsedExpression, formatSpecifier, positionHint, stringToken);
                        }

                        nextIndex = index + 1;
                        return true;
                    }

                    if (braceDepth > 0)
                    {
                        braceDepth--;
                    }
                    continue;
                default:
                    continue;
            }
        }

        diagnostics.Add(CreateInterpolationDiagnostic(stringToken, openBraceIndex, "Interpolated string expression is not closed."));
        return false;
    }

    private ExpressionSyntax ParseInterpolatedExpression(string expressionText, SyntaxToken stringToken, int contentOffset, int positionHint)
    {
        var expressionSource = new SourceText($"print {expressionText}", sourceText.FileName);
        var expressionTree = SyntaxTree.Parse(expressionSource);
        if (expressionTree.Diagnostics.Count > 0)
        {
            diagnostics.Add(CreateInterpolationDiagnostic(stringToken, contentOffset, $"Invalid interpolation expression: {expressionText}"));
            return CreateStringLiteralExpression(string.Empty, stringToken, positionHint);
        }

        if (expressionTree.Root.Statements.FirstOrDefault() is not PrintStatementSyntax printStatement)
        {
            diagnostics.Add(CreateInterpolationDiagnostic(stringToken, contentOffset, $"Invalid interpolation expression: {expressionText}"));
            return CreateStringLiteralExpression(string.Empty, stringToken, positionHint);
        }

        return printStatement.Expression;
    }

    private static void FlushLiteralSegment(List<ExpressionSyntax> segments, StringBuilder literalBuilder, SyntaxToken stringToken, int positionHint)
    {
        if (literalBuilder.Length == 0)
        {
            return;
        }

        segments.Add(CreateStringLiteralExpression(literalBuilder.ToString(), stringToken, positionHint));
        literalBuilder.Clear();
    }

    private static ExpressionSyntax CreateStringLiteralExpression(string value, SyntaxToken stringToken, int positionHint)
    {
        var token = new SyntaxToken(
            TokenKind.StringLiteral,
            stringToken.Span,
            $"\"{value}\"",
            value);
        return new LiteralExpressionSyntax(token);
    }

    private static ExpressionSyntax WrapStringifyCall(ExpressionSyntax expression, int positionHint)
    {
        var strToken = new SyntaxToken(TokenKind.Identifier, new TextSpan(positionHint, 1), "str", null);
        var callee = new NameExpressionSyntax(strToken);
        var openParen = SyntaxToken.Missing(TokenKind.OpenParen, positionHint);
        var closeParen = SyntaxToken.Missing(TokenKind.CloseParen, positionHint);
        return new CallExpressionSyntax(callee, openParen, new[] { expression }, closeParen);
    }

    private static ExpressionSyntax WrapFormatCall(ExpressionSyntax expression, string formatSpecifier, int positionHint, SyntaxToken stringToken)
    {
        var formatToken = new SyntaxToken(TokenKind.Identifier, new TextSpan(positionHint, 1), "format", null);
        var callee = new NameExpressionSyntax(formatToken);
        var openParen = SyntaxToken.Missing(TokenKind.OpenParen, positionHint);
        var closeParen = SyntaxToken.Missing(TokenKind.CloseParen, positionHint);
        var formatLiteral = CreateStringLiteralExpression(formatSpecifier, stringToken, positionHint);
        return new CallExpressionSyntax(callee, openParen, new[] { expression, formatLiteral }, closeParen);
    }

    private Diagnostic CreateInterpolationDiagnostic(SyntaxToken stringToken, int contentOffset, string message)
    {
        var safeOffset = Math.Max(0, contentOffset);
        var spanStart = stringToken.Span.Start + 1 + safeOffset;
        var maxStart = Math.Max(stringToken.Span.Start, stringToken.Span.End - 1);
        var span = new TextSpan(Math.Min(spanStart, maxStart), 1);
        return Diagnostic.Error(sourceText, span, message);
    }

    private ExpressionSyntax ParseListOrMapExpression()
    {
        var openBracket = MatchToken(TokenKind.OpenBracket, "[");
        ConsumeSeparators();
        if (Current().Kind == TokenKind.CloseBracket)
        {
            var closeEmpty = MatchToken(TokenKind.CloseBracket, "]");
            diagnostics.Add(Diagnostic.Error(sourceText, closeEmpty.Span, "List and map literals cannot be empty."));
            return new ListExpressionSyntax(openBracket, Array.Empty<ExpressionSyntax>(), closeEmpty);
        }

        var firstStart = position;
        var firstExpression = ParseExpression();
        if (Current().Kind == TokenKind.Colon)
        {
            var entries = new List<MapEntrySyntax>();
            var colonToken = MatchToken(TokenKind.Colon, ":");
            var valueExpression = ParseExpression();
            entries.Add(new MapEntrySyntax(firstExpression, colonToken, valueExpression));

            ConsumeSeparators();
            while (Current().Kind == TokenKind.Comma)
            {
                NextToken();
                ConsumeSeparators();
                if (Current().Kind == TokenKind.CloseBracket)
                {
                    break;
                }

                var key = ParseExpression();
                var colon = MatchToken(TokenKind.Colon, ":");
                var value = ParseExpression();
                entries.Add(new MapEntrySyntax(key, colon, value));

                ConsumeSeparators();
            }

            if (position == firstStart)
            {
                NextToken();
            }

            var closeBracket = MatchToken(TokenKind.CloseBracket, "]");
            return new MapExpressionSyntax(openBracket, entries, closeBracket);
        }

        var elements = new List<ExpressionSyntax> { firstExpression };
        ConsumeSeparators();
        while (Current().Kind == TokenKind.Comma)
        {
            NextToken();
            ConsumeSeparators();
            if (Current().Kind == TokenKind.CloseBracket)
            {
                break;
            }

            elements.Add(ParseExpression());
            ConsumeSeparators();
        }

        var close = MatchToken(TokenKind.CloseBracket, "]");
        return new ListExpressionSyntax(openBracket, elements, close);
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
            SyntaxToken? whenKeyword = null;
            ExpressionSyntax? guard = null;
            if (Current().Kind == TokenKind.WhenKeyword)
            {
                whenKeyword = NextToken();
                guard = ParseExpression();
            }
            var arrowToken = MatchToken(TokenKind.ArrowType, "->");
            var armExpression = ParseExpression();
            arms.Add(new MatchArmSyntax(pattern, whenKeyword, guard, arrowToken, armExpression));
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
        var entries = new List<RecordLiteralEntrySyntax>();

        ConsumeSeparators();
        while (Current().Kind != TokenKind.CloseBrace && Current().Kind != TokenKind.EndOfFile)
        {
            var start = position;
            if (Current().Kind == TokenKind.Ellipsis)
            {
                var ellipsisToken = MatchToken(TokenKind.Ellipsis, "...");
                var spreadExpression = ParseExpression();
                entries.Add(new RecordSpreadSyntax(ellipsisToken, spreadExpression));
            }
            else
            {
                var fieldIdentifier = MatchToken(TokenKind.Identifier, "field name");
                var colonToken = MatchToken(TokenKind.Colon, ":");
                var expression = ParseExpression();
                entries.Add(new RecordFieldAssignmentSyntax(fieldIdentifier, colonToken, expression));
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
        return new RecordLiteralExpressionSyntax(identifier, openBrace, entries, closeBrace);
    }

    private PatternSyntax ParsePattern()
    {
        switch (Current().Kind)
        {
            case TokenKind.Less:
            case TokenKind.LessOrEqual:
            case TokenKind.Greater:
            case TokenKind.GreaterOrEqual:
            case TokenKind.EqualEqual:
            case TokenKind.BangEqual:
                var relationalOperator = NextToken();
                var relationalRight = ParseExpression(allowRecordLiteral: false);
                return new RelationalPatternSyntax(relationalOperator, relationalRight);
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

    private ExpressionSyntax ParseGenericCallExpression(ExpressionSyntax callee)
    {
        var lessToken = MatchToken(TokenKind.Less, "<");
        var typeArguments = new List<TypeSyntax> { ParseTypeSyntax() };
        while (Current().Kind == TokenKind.Comma)
        {
            NextToken();
            typeArguments.Add(ParseTypeSyntax());
        }

        var greaterToken = MatchToken(TokenKind.Greater, ">");
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
        return new GenericCallExpressionSyntax(callee, lessToken, typeArguments, greaterToken, openParen, arguments, closeParen);
    }

    private bool IsGenericCallStart()
    {
        if (Current().Kind != TokenKind.Less)
        {
            return false;
        }

        var index = 1;
        var sawType = false;
        while (Peek(index).Kind != TokenKind.EndOfFile)
        {
            var kind = Peek(index).Kind;
            if (kind == TokenKind.Identifier)
            {
                sawType = true;
                index++;
                continue;
            }

            if (kind == TokenKind.Comma)
            {
                index++;
                continue;
            }

            if (kind == TokenKind.Greater)
            {
                return sawType && Peek(index + 1).Kind == TokenKind.OpenParen;
            }

            return false;
        }

        return false;
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
            TokenKind.PipeGreater => 1,
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
