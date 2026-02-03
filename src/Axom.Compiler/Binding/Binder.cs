using System.Linq;
using Axom.Compiler.Diagnostics;
using Axom.Compiler.Lexing;
using Axom.Compiler.Parsing;
using Axom.Compiler.Syntax;
using Axom.Compiler.Text;

namespace Axom.Compiler.Binding;

public sealed class Binder
{
    private readonly List<Diagnostic> diagnostics = new();
    private BoundScope scope = new(null);
    private SourceText? sourceText;
    private SourceText SourceText => sourceText ?? new SourceText(string.Empty, string.Empty);
    private FunctionSymbol? currentFunction;
    private readonly Stack<LambdaBindingContext> lambdaStack = new();
    private List<TypeSymbol>? returnTypes;
    private readonly Dictionary<string, TypeSymbol> recordTypes = new(StringComparer.Ordinal);
    private readonly Dictionary<string, BoundRecordTypeDeclaration> recordDefinitions = new(StringComparer.Ordinal);
    private readonly Dictionary<string, TypeSymbol> sumTypes = new(StringComparer.Ordinal);
    private readonly Dictionary<string, BoundSumTypeDeclaration> sumDefinitions = new(StringComparer.Ordinal);
    private readonly Dictionary<string, SumVariantSymbol> variantDefinitions = new(StringComparer.Ordinal);

    private sealed class LambdaBindingContext
    {
        public BoundScope Scope { get; }
        public HashSet<VariableSymbol> Captures { get; } = new();

        public LambdaBindingContext(BoundScope scope)
        {
            Scope = scope;
        }
    }

    public BinderResult Bind(SyntaxTree syntaxTree)
    {
        sourceText = syntaxTree.SourceText;
        scope = new BoundScope(null);
        recordTypes.Clear();
        recordDefinitions.Clear();
        sumTypes.Clear();
        sumDefinitions.Clear();
        variantDefinitions.Clear();
        DeclareBuiltins();

        var statements = new List<BoundStatement>();
        var functions = new List<BoundFunctionDeclaration>();
        var recordDeclarations = syntaxTree.Root.Statements
            .OfType<RecordTypeDeclarationSyntax>()
            .ToList();
        DeclareRecordTypeSymbols(recordDeclarations);
        var records = BindRecordTypeDeclarations(recordDeclarations);

        var sumDeclarations = syntaxTree.Root.Statements
            .OfType<SumTypeDeclarationSyntax>()
            .ToList();
        DeclareSumTypeSymbols(sumDeclarations);
        var sums = BindSumTypeDeclarations(sumDeclarations);

        var functionDeclarations = syntaxTree.Root.Statements
            .OfType<FunctionDeclarationSyntax>()
            .ToList();
        DeclareFunctionSymbols(functionDeclarations);

        foreach (var statement in syntaxTree.Root.Statements)
        {
            if (statement is RecordTypeDeclarationSyntax)
            {
                continue;
            }

            if (statement is SumTypeDeclarationSyntax)
            {
                continue;
            }

            if (statement is FunctionDeclarationSyntax functionDeclaration)
            {
                functions.Add(BindFunctionDeclaration(functionDeclaration));
                continue;
            }

            statements.Add(BindStatement(statement));
        }

        var allDiagnostics = syntaxTree.Diagnostics
            .Concat(diagnostics)
            .ToList();
        return new BinderResult(new BoundProgram(records, sums, functions, statements), allDiagnostics);
    }

    public BinderResult BindCached(SyntaxTree syntaxTree, BindingCache cache)
    {
        if (cache.TryGet(syntaxTree, out var cached) && cached is not null)
        {
            return cached;
        }

        var result = Bind(syntaxTree);
        cache.Store(syntaxTree, result);
        return result;
    }

    private void DeclareBuiltins()
    {
        foreach (var function in BuiltinFunctions.All)
        {
            scope.TryDeclareFunction(function);
        }
    }

    private void DeclareRecordTypeSymbols(IReadOnlyList<RecordTypeDeclarationSyntax> declarations)
    {
        foreach (var declaration in declarations)
        {
            var name = declaration.IdentifierToken.Text;
            if (string.IsNullOrEmpty(name))
            {
                continue;
            }

            if (recordTypes.ContainsKey(name) || sumTypes.ContainsKey(name))
            {
                diagnostics.Add(Diagnostic.Error(
                    SourceText,
                    declaration.IdentifierToken.Span,
                    $"Type '{name}' is already declared in this scope."));
                continue;
            }

            recordTypes[name] = TypeSymbol.Record(name);
        }
    }

    private List<BoundRecordTypeDeclaration> BindRecordTypeDeclarations(IReadOnlyList<RecordTypeDeclarationSyntax> declarations)
    {
        var records = new List<BoundRecordTypeDeclaration>();
        foreach (var declaration in declarations)
        {
            var name = declaration.IdentifierToken.Text;
            if (string.IsNullOrEmpty(name))
            {
                continue;
            }

            if (!recordTypes.TryGetValue(name, out var type))
            {
                continue;
            }

            var fields = new List<RecordFieldSymbol>();
            var seenFields = new HashSet<string>(StringComparer.Ordinal);
            foreach (var field in declaration.Fields)
            {
                var fieldName = field.IdentifierToken.Text;
                if (!seenFields.Add(fieldName))
                {
                    diagnostics.Add(Diagnostic.Error(
                        SourceText,
                        field.IdentifierToken.Span,
                        $"Field '{fieldName}' is already declared in record '{name}'."));
                    continue;
                }

                var fieldType = BindType(field.Type);
                fields.Add(new RecordFieldSymbol(fieldName, fieldType));
            }

            var record = new BoundRecordTypeDeclaration(type, fields);
            records.Add(record);
            recordDefinitions[name] = record;
        }

        return records;
    }

    private void DeclareSumTypeSymbols(IReadOnlyList<SumTypeDeclarationSyntax> declarations)
    {
        foreach (var declaration in declarations)
        {
            var name = declaration.IdentifierToken.Text;
            if (string.IsNullOrEmpty(name))
            {
                continue;
            }

            if (sumTypes.ContainsKey(name) || recordTypes.ContainsKey(name))
            {
                diagnostics.Add(Diagnostic.Error(
                    SourceText,
                    declaration.IdentifierToken.Span,
                    $"Type '{name}' is already declared in this scope."));
                continue;
            }

            sumTypes[name] = TypeSymbol.Sum(name);
        }
    }

    private List<BoundSumTypeDeclaration> BindSumTypeDeclarations(IReadOnlyList<SumTypeDeclarationSyntax> declarations)
    {
        var sums = new List<BoundSumTypeDeclaration>();
        foreach (var declaration in declarations)
        {
            var name = declaration.IdentifierToken.Text;
            if (string.IsNullOrEmpty(name))
            {
                continue;
            }

            if (!sumTypes.TryGetValue(name, out var type))
            {
                continue;
            }

            var variants = new List<SumVariantSymbol>();
            var seenVariants = new HashSet<string>(StringComparer.Ordinal);
            foreach (var variant in declaration.Variants)
            {
                var variantName = variant.IdentifierToken.Text;
                if (!seenVariants.Add(variantName))
                {
                    diagnostics.Add(Diagnostic.Error(
                        SourceText,
                        variant.IdentifierToken.Span,
                        $"Variant '{variantName}' is already declared in '{name}'."));
                    continue;
                }

                if (variantDefinitions.ContainsKey(variantName))
                {
                    diagnostics.Add(Diagnostic.Error(
                        SourceText,
                        variant.IdentifierToken.Span,
                        $"Variant '{variantName}' is already declared in another sum type."));
                    continue;
                }

                var payloadType = variant.PayloadType is null ? null : BindType(variant.PayloadType);
                var symbol = new SumVariantSymbol(variantName, type, payloadType);
                variants.Add(symbol);
                variantDefinitions[variantName] = symbol;
            }

            var sum = new BoundSumTypeDeclaration(type, variants);
            sums.Add(sum);
            sumDefinitions[name] = sum;
        }

        return sums;
    }

    private void DeclareFunctionSymbols(IReadOnlyList<FunctionDeclarationSyntax> declarations)
    {
        foreach (var declaration in declarations)
        {
            var name = declaration.IdentifierToken.Text;
            if (string.IsNullOrEmpty(name))
            {
                continue;
            }

            var parameters = new List<ParameterSymbol>();
            foreach (var parameter in declaration.Parameters)
            {
                var type = BindType(parameter.Type);
                parameters.Add(new ParameterSymbol(parameter.IdentifierToken.Text, type));
            }

            var returnType = declaration.ReturnType is null
                ? TypeSymbol.Unit
                : BindType(declaration.ReturnType);

            var symbol = new FunctionSymbol(name, parameters, returnType);
            var declared = scope.TryDeclareFunction(symbol);
            if (declared is null)
            {
                diagnostics.Add(Diagnostic.Error(
                    SourceText,
                    declaration.IdentifierToken.Span,
                    $"Function '{name}' is already declared in this scope."));
            }
        }
    }

    private BoundStatement BindStatement(StatementSyntax statement)
    {
        switch (statement)
        {
            case BlockStatementSyntax block:
                return BindBlockStatement(block);
            case VariableDeclarationSyntax declaration:
                return BindVariableDeclaration(declaration);
            case PrintStatementSyntax print:
                return new BoundPrintStatement(BindExpression(print.Expression));
            case ReturnStatementSyntax returnStatement:
                return BindReturnStatement(returnStatement);
            case FunctionDeclarationSyntax:
                diagnostics.Add(Diagnostic.Error(
                    SourceText,
                    statement.Span,
                    "Function declarations are only allowed at the top level."));
                return new BoundExpressionStatement(new BoundLiteralExpression(null, TypeSymbol.Unit));
            case RecordTypeDeclarationSyntax:
                diagnostics.Add(Diagnostic.Error(
                    SourceText,
                    statement.Span,
                    "Type declarations are only allowed at the top level."));
                return new BoundExpressionStatement(new BoundLiteralExpression(null, TypeSymbol.Unit));
            case SumTypeDeclarationSyntax:
                diagnostics.Add(Diagnostic.Error(
                    SourceText,
                    statement.Span,
                    "Type declarations are only allowed at the top level."));
                return new BoundExpressionStatement(new BoundLiteralExpression(null, TypeSymbol.Unit));
            case ExpressionStatementSyntax expressionStatement:
                return new BoundExpressionStatement(BindExpression(expressionStatement.Expression));
            default:
                throw new InvalidOperationException($"Unexpected statement: {statement.GetType().Name}");
        }
    }

    private BoundStatement BindBlockStatement(BlockStatementSyntax block)
    {
        var previousScope = scope;
        scope = new BoundScope(previousScope);
        var statements = new List<BoundStatement>();

        foreach (var statement in block.Statements)
        {
            statements.Add(BindStatement(statement));
        }

        scope = previousScope;
        return new BoundBlockStatement(statements);
    }

    private BoundFunctionDeclaration BindFunctionDeclaration(FunctionDeclarationSyntax declaration)
    {
        var name = declaration.IdentifierToken.Text;
        var functionSymbol = scope.TryLookupFunction(name) ?? new FunctionSymbol(
            name,
            declaration.Parameters.Select(parameter => new ParameterSymbol(parameter.IdentifierToken.Text, BindType(parameter.Type))).ToList(),
            declaration.ReturnType is null ? TypeSymbol.Unit : BindType(declaration.ReturnType));

        var previousScope = scope;
        var previousFunction = currentFunction;
        var previousReturnTypes = returnTypes;
        scope = new BoundScope(previousScope);
        currentFunction = functionSymbol;
        returnTypes = new List<TypeSymbol>();

        var parameterSymbols = new List<VariableSymbol>();
        foreach (var parameter in functionSymbol.Parameters)
        {
            var symbol = new VariableSymbol(parameter.Name, false, parameter.Type);
            parameterSymbols.Add(symbol);
            var declared = scope.TryDeclare(symbol);
            if (declared is null)
            {
                diagnostics.Add(Diagnostic.Error(
                    SourceText,
                    declaration.IdentifierToken.Span,
                    $"Parameter '{parameter.Name}' is already declared in this scope."));
            }
        }

        BoundBlockStatement body;
        if (declaration.BodyExpression is not null)
        {
            var expression = BindExpression(declaration.BodyExpression);
            returnTypes.Add(expression.Type);
            body = new BoundBlockStatement(new BoundStatement[]
            {
                new BoundReturnStatement(expression)
            });
        }
        else if (declaration.BodyBlock is not null)
        {
            body = ApplyImplicitReturn((BoundBlockStatement)BindBlockStatement(declaration.BodyBlock));
        }
        else
        {
            body = new BoundBlockStatement(Array.Empty<BoundStatement>());
        }

        InferAndValidateReturnType(functionSymbol, declaration.ReturnType is not null, body, declaration.IdentifierToken.Span);

        currentFunction = previousFunction;
        returnTypes = previousReturnTypes;
        scope = previousScope;
        return new BoundFunctionDeclaration(functionSymbol, parameterSymbols, body);
    }

    private BoundStatement BindVariableDeclaration(VariableDeclarationSyntax declaration)
    {
        var name = declaration.IdentifierToken.Text;
        var isMutable = declaration.MutKeyword is not null;
        var initializer = BindExpression(declaration.Initializer);
        var type = initializer.Type;

        VariableSymbol? declaredSymbol = null;
        if (!string.IsNullOrEmpty(name))
        {
            var symbol = new VariableSymbol(name, isMutable, type);
            declaredSymbol = scope.TryDeclare(symbol);
            if (declaredSymbol is null)
            {
                diagnostics.Add(Diagnostic.Error(
                    SourceText,
                    declaration.IdentifierToken.Span,
                    $"Variable '{name}' is already declared in this scope."));
            }
        }

        declaredSymbol ??= new VariableSymbol(name, isMutable, type);
        return new BoundVariableDeclaration(declaredSymbol, initializer);
    }

    private BoundExpression BindExpression(ExpressionSyntax expression)
    {
        switch (expression)
        {
            case LiteralExpressionSyntax literal:
                return BindLiteralExpression(literal);
            case NameExpressionSyntax name:
                return BindNameExpression(name);
            case AssignmentExpressionSyntax assignment:
                return BindAssignmentExpression(assignment);
            case BinaryExpressionSyntax binary:
                return BindBinaryExpression(binary);
            case MatchExpressionSyntax match:
                return BindMatchExpression(match);
            case TupleExpressionSyntax tuple:
                return BindTupleExpression(tuple);
            case RecordLiteralExpressionSyntax record:
                return BindRecordLiteralExpression(record);
            case FieldAccessExpressionSyntax fieldAccess:
                return BindFieldAccessExpression(fieldAccess);
            case UnaryExpressionSyntax unary:
                return BindUnaryExpression(unary);
            case ParenthesizedExpressionSyntax parenthesized:
                return BindExpression(parenthesized.Expression);
            case InputExpressionSyntax:
                return BindInputExpression();
            case CallExpressionSyntax call:
                return BindCallExpression(call);
            case LambdaExpressionSyntax lambda:
                return BindLambdaExpression(lambda);
            default:
                throw new InvalidOperationException($"Unexpected expression: {expression.GetType().Name}");
        }
    }

    private BoundExpression BindRecordLiteralExpression(RecordLiteralExpressionSyntax record)
    {
        var name = record.IdentifierToken.Text;
        if (!recordDefinitions.TryGetValue(name, out var definition))
        {
            diagnostics.Add(Diagnostic.Error(
                SourceText,
                record.IdentifierToken.Span,
                $"Unknown type '{name}'."));
            return new BoundRecordLiteralExpression(TypeSymbol.Error, Array.Empty<BoundRecordFieldAssignment>());
        }

        var assignments = new List<BoundRecordFieldAssignment>();
        var seenFields = new HashSet<string>(StringComparer.Ordinal);
        foreach (var field in record.Fields)
        {
            var fieldName = field.IdentifierToken.Text;
            var boundExpression = BindExpression(field.Expression);
            if (!seenFields.Add(fieldName))
            {
                diagnostics.Add(Diagnostic.Error(
                    SourceText,
                    field.IdentifierToken.Span,
                    $"Field '{fieldName}' is already initialized in record '{name}'."));
                continue;
            }

            var match = definition.Fields.FirstOrDefault(f => string.Equals(f.Name, fieldName, StringComparison.Ordinal));
            if (match is null)
            {
                diagnostics.Add(Diagnostic.Error(
                    SourceText,
                    field.IdentifierToken.Span,
                    $"Record '{name}' has no field '{fieldName}'."));
                continue;
            }
            if (boundExpression.Type != match.Type && boundExpression.Type != TypeSymbol.Error && match.Type != TypeSymbol.Error)
            {
                diagnostics.Add(Diagnostic.Error(
                    SourceText,
                    field.Expression.Span,
                    $"Cannot assign expression of type '{boundExpression.Type}' to field '{fieldName}' of type '{match.Type}'."));
            }

            assignments.Add(new BoundRecordFieldAssignment(match, boundExpression));
        }

        foreach (var field in definition.Fields)
        {
            if (!seenFields.Contains(field.Name))
            {
                diagnostics.Add(Diagnostic.Error(
                    SourceText,
                    record.IdentifierToken.Span,
                    $"Record '{name}' is missing required field '{field.Name}'."));
            }
        }

        return new BoundRecordLiteralExpression(definition.Type, assignments);
    }

    private BoundExpression BindFieldAccessExpression(FieldAccessExpressionSyntax fieldAccess)
    {
        var target = BindExpression(fieldAccess.Target);
        if (!recordDefinitions.TryGetValue(target.Type.Name, out var definition))
        {
            diagnostics.Add(Diagnostic.Error(
                SourceText,
                fieldAccess.IdentifierToken.Span,
                "Field access requires a record type."));
            return new BoundFieldAccessExpression(target, new RecordFieldSymbol(fieldAccess.IdentifierToken.Text, TypeSymbol.Error));
        }

        var fieldName = fieldAccess.IdentifierToken.Text;
        var match = definition.Fields.FirstOrDefault(field => string.Equals(field.Name, fieldName, StringComparison.Ordinal));
        if (match is null)
        {
            diagnostics.Add(Diagnostic.Error(
                SourceText,
                fieldAccess.IdentifierToken.Span,
                $"Record '{definition.Type.Name}' has no field '{fieldName}'."));
            return new BoundFieldAccessExpression(target, new RecordFieldSymbol(fieldName, TypeSymbol.Error));
        }

        return new BoundFieldAccessExpression(target, match);
    }

    private BoundExpression BindLiteralExpression(LiteralExpressionSyntax literal)
    {
        var value = literal.LiteralToken.Value;
        if (value is int)
        {
            return new BoundLiteralExpression(value, TypeSymbol.Int);
        }

        if (value is double)
        {
            return new BoundLiteralExpression(value, TypeSymbol.Float);
        }

        if (value is bool)
        {
            return new BoundLiteralExpression(value, TypeSymbol.Bool);
        }

        if (value is string)
        {
            return new BoundLiteralExpression(value, TypeSymbol.String);
        }

        return new BoundLiteralExpression(value, TypeSymbol.Error);
    }

    private BoundExpression BindBinaryExpression(BinaryExpressionSyntax binary)
    {
        var left = BindExpression(binary.Left);
        var right = BindExpression(binary.Right);
        var op = binary.OperatorToken.Kind;

        if (left.Type == TypeSymbol.Bool && right.Type == TypeSymbol.Bool)
        {
            if (op is TokenKind.AmpersandAmpersand or TokenKind.PipePipe or TokenKind.EqualEqual or TokenKind.BangEqual)
            {
                return new BoundBinaryExpression(left, op, right, TypeSymbol.Bool);
            }
        }

        if (left.Type == TypeSymbol.Int && right.Type == TypeSymbol.Int)
        {
            if (op is TokenKind.AmpersandAmpersand or TokenKind.PipePipe)
            {
                diagnostics.Add(Diagnostic.Error(
                    SourceText,
                    binary.OperatorToken.Span,
                    $"Operator '{binary.OperatorToken.Text}' is not defined for types '{left.Type}' and '{right.Type}'."));
                return new BoundBinaryExpression(left, op, right, TypeSymbol.Error);
            }

            if (op is TokenKind.EqualEqual or TokenKind.BangEqual or TokenKind.Less or TokenKind.LessOrEqual
                or TokenKind.Greater or TokenKind.GreaterOrEqual)
            {
                return new BoundBinaryExpression(left, op, right, TypeSymbol.Bool);
            }

            return new BoundBinaryExpression(left, op, right, TypeSymbol.Int);
        }

        if (left.Type == TypeSymbol.Float && right.Type == TypeSymbol.Float)
        {
            if (op is TokenKind.EqualEqual or TokenKind.BangEqual or TokenKind.Less or TokenKind.LessOrEqual
                or TokenKind.Greater or TokenKind.GreaterOrEqual)
            {
                return new BoundBinaryExpression(left, op, right, TypeSymbol.Bool);
            }

            return new BoundBinaryExpression(left, op, right, TypeSymbol.Float);
        }

        if (op is TokenKind.EqualEqual or TokenKind.BangEqual)
        {
            if (left.Type == right.Type && (left.Type == TypeSymbol.Bool || left.Type == TypeSymbol.String))
            {
                return new BoundBinaryExpression(left, op, right, TypeSymbol.Bool);
            }
        }

        if (left.Type != TypeSymbol.Error && right.Type != TypeSymbol.Error)
        {
            diagnostics.Add(Diagnostic.Error(
                SourceText,
                binary.OperatorToken.Span,
                $"Operator '{binary.OperatorToken.Text}' is not defined for types '{left.Type}' and '{right.Type}'."));
        }

        return new BoundBinaryExpression(left, op, right, TypeSymbol.Error);
    }

    private BoundExpression BindMatchExpression(MatchExpressionSyntax match)
    {
        var valueExpression = BindExpression(match.Expression);
        var arms = new List<BoundMatchArm>();
        TypeSymbol? matchType = null;
        var previousScope = scope;

        foreach (var arm in match.Arms)
        {
            scope = new BoundScope(previousScope);
            var boundPattern = BindPattern(arm.Pattern, valueExpression.Type);
            var boundExpression = BindExpression(arm.Expression);
            arms.Add(new BoundMatchArm(boundPattern, boundExpression));
            scope = previousScope;

            if (matchType is null)
            {
                matchType = boundExpression.Type;
            }
            else if (matchType != boundExpression.Type && matchType != TypeSymbol.Error && boundExpression.Type != TypeSymbol.Error)
            {
                diagnostics.Add(Diagnostic.Error(
                    SourceText,
                    arm.Expression.Span,
                    "Match arms must have the same type."));
                matchType = TypeSymbol.Error;
            }
        }

        ReportMatchDiagnostics(match, valueExpression.Type);
        matchType ??= TypeSymbol.Error;
        return new BoundMatchExpression(valueExpression, arms, matchType);
    }

    private BoundPattern BindPattern(PatternSyntax pattern, TypeSymbol targetType)
    {
        switch (pattern)
        {
            case LiteralPatternSyntax literal:
                var (value, type) = BindPatternLiteral(literal.LiteralToken);
                if (type != targetType && type != TypeSymbol.Error && targetType != TypeSymbol.Error)
                {
                    diagnostics.Add(Diagnostic.Error(
                        SourceText,
                        literal.LiteralToken.Span,
                        $"Pattern type '{type}' does not match '{targetType}'."));
                }

                return new BoundLiteralPattern(value, type);
            case WildcardPatternSyntax:
                return new BoundWildcardPattern(targetType);
            case IdentifierPatternSyntax identifier:
                return BindIdentifierPattern(identifier, targetType);
            case TuplePatternSyntax tuple:
                return BindTuplePattern(tuple, targetType);
            case VariantPatternSyntax variant:
                return BindVariantPattern(variant, targetType);
            default:
                return new BoundWildcardPattern(TypeSymbol.Error);
        }
    }

    private BoundPattern BindIdentifierPattern(IdentifierPatternSyntax identifier, TypeSymbol targetType)
    {
        var name = identifier.IdentifierToken.Text;
        var sumDefinition = TryGetSumDefinition(targetType);
        if (sumDefinition is not null)
        {
            var variant = sumDefinition.Variants.FirstOrDefault(item => string.Equals(item.Name, name, StringComparison.Ordinal));
            if (variant is not null)
            {
                if (variant.PayloadType is not null)
                {
                    diagnostics.Add(Diagnostic.Error(
                        SourceText,
                        identifier.IdentifierToken.Span,
                        $"Variant '{name}' requires a payload pattern."));
                }

                return new BoundVariantPattern(variant, null, targetType);
            }
        }

        var symbol = new VariableSymbol(name, false, targetType);
        var declared = scope.TryDeclare(symbol);
        if (declared is null)
        {
            diagnostics.Add(Diagnostic.Error(
                SourceText,
                identifier.IdentifierToken.Span,
                $"Pattern variable '{name}' is already declared in this scope."));
        }

        return new BoundIdentifierPattern(symbol, targetType);
    }

    private BoundPattern BindVariantPattern(VariantPatternSyntax variantPattern, TypeSymbol targetType)
    {
        var sumDefinition = TryGetSumDefinition(targetType);
        if (sumDefinition is null)
        {
            diagnostics.Add(Diagnostic.Error(
                SourceText,
                variantPattern.IdentifierToken.Span,
                "Variant pattern requires a sum type."));
            return new BoundVariantPattern(new SumVariantSymbol(variantPattern.IdentifierToken.Text, targetType, TypeSymbol.Error), null, TypeSymbol.Error);
        }

        var name = variantPattern.IdentifierToken.Text;
        var variant = sumDefinition.Variants.FirstOrDefault(item => string.Equals(item.Name, name, StringComparison.Ordinal));
        if (variant is null)
        {
            diagnostics.Add(Diagnostic.Error(
                SourceText,
                variantPattern.IdentifierToken.Span,
                $"Sum type '{sumDefinition.Type.Name}' has no variant '{name}'."));
            return new BoundVariantPattern(new SumVariantSymbol(name, targetType, TypeSymbol.Error), null, TypeSymbol.Error);
        }

        if (variant.PayloadType is null)
        {
            diagnostics.Add(Diagnostic.Error(
                SourceText,
                variantPattern.IdentifierToken.Span,
                $"Variant '{name}' does not take a payload."));
            return new BoundVariantPattern(variant, null, targetType);
        }

        var payload = BindPattern(variantPattern.Payload, variant.PayloadType);
        return new BoundVariantPattern(variant, payload, targetType);
    }

    private BoundPattern BindTuplePattern(TuplePatternSyntax tuple, TypeSymbol targetType)
    {
        if (targetType.TupleElementTypes is null)
        {
            diagnostics.Add(Diagnostic.Error(
                SourceText,
                tuple.Span,
                "Tuple pattern does not match non-tuple type."));
            var fallback = tuple.Elements.Select(_ => new BoundWildcardPattern(TypeSymbol.Error)).ToList();
            return new BoundTuplePattern(fallback, TypeSymbol.Error);
        }

        var elements = new List<BoundPattern>();
        var expected = targetType.TupleElementTypes;
        if (expected.Count != tuple.Elements.Count)
        {
            diagnostics.Add(Diagnostic.Error(
                SourceText,
                tuple.Span,
                "Tuple pattern arity does not match tuple value."));
        }

        for (var i = 0; i < tuple.Elements.Count; i++)
        {
            var elementType = i < expected.Count ? expected[i] : TypeSymbol.Error;
            elements.Add(BindPattern(tuple.Elements[i], elementType));
        }

        return new BoundTuplePattern(elements, targetType);
    }

    private BoundExpression BindTupleExpression(TupleExpressionSyntax tuple)
    {
        var elements = new List<BoundExpression>();
        var elementTypes = new List<TypeSymbol>();
        foreach (var element in tuple.Elements)
        {
            var bound = BindExpression(element);
            elements.Add(bound);
            elementTypes.Add(bound.Type);
        }

        var tupleType = TypeSymbol.Tuple(elementTypes);
        return new BoundTupleExpression(elements, tupleType);
    }

    private void ReportMatchDiagnostics(MatchExpressionSyntax match, TypeSymbol targetType)
    {
        if (targetType == TypeSymbol.Error)
        {
            return;
        }

        var sumDefinition = TryGetSumDefinition(targetType);
        if (sumDefinition is not null)
        {
            var seenVariants = new HashSet<string>(StringComparer.Ordinal);
            var sumSeenCatchAll = false;
            foreach (var arm in match.Arms)
            {
                var pattern = arm.Pattern;
                if (sumSeenCatchAll)
                {
                    diagnostics.Add(Diagnostic.Error(
                        SourceText,
                        pattern.Span,
                        "Unreachable match arm."));
                    continue;
                }

                if (IsSumCatchAll(pattern, sumDefinition, out var variantName))
                {
                    sumSeenCatchAll = true;
                    continue;
                }

                if (variantName is not null)
                {
                    if (!seenVariants.Add(variantName))
                    {
                        diagnostics.Add(Diagnostic.Error(
                            SourceText,
                            pattern.Span,
                            "Duplicate match arm."));
                    }
                }
            }

            if (sumSeenCatchAll)
            {
                return;
            }

            if (seenVariants.Count != sumDefinition.Variants.Count)
            {
                diagnostics.Add(Diagnostic.Error(
                    SourceText,
                    match.MatchKeyword.Span,
                    "Non-exhaustive match expression."));
            }

            return;
        }

        var seenLiterals = new HashSet<object?>();
        var seenTrue = false;
        var seenFalse = false;
        var seenCatchAll = false;

        foreach (var arm in match.Arms)
        {
            var pattern = arm.Pattern;
            var isCatchAll = IsCatchAllPattern(pattern);
            if (seenCatchAll)
            {
                diagnostics.Add(Diagnostic.Error(
                    SourceText,
                    pattern.Span,
                    "Unreachable match arm."));
                continue;
            }

            if (isCatchAll)
            {
                seenCatchAll = true;
                continue;
            }

            if (pattern is LiteralPatternSyntax literalPattern)
            {
                var value = literalPattern.LiteralToken.Value;
                if (!seenLiterals.Add(value))
                {
                    diagnostics.Add(Diagnostic.Error(
                        SourceText,
                        literalPattern.Span,
                        "Duplicate match arm."));
                }

                if (value is bool boolValue)
                {
                    if (boolValue)
                    {
                        seenTrue = true;
                    }
                    else
                    {
                        seenFalse = true;
                    }
                }
            }
        }

        if (seenCatchAll)
        {
            return;
        }

        if (targetType == TypeSymbol.Bool)
        {
            if (!seenTrue || !seenFalse)
            {
                diagnostics.Add(Diagnostic.Error(
                    SourceText,
                    match.MatchKeyword.Span,
                    "Non-exhaustive match expression."));
            }

            return;
        }

        diagnostics.Add(Diagnostic.Error(
            SourceText,
            match.MatchKeyword.Span,
            "Non-exhaustive match expression."));
    }

    private static bool IsCatchAllPattern(PatternSyntax pattern)
    {
        return pattern switch
        {
            WildcardPatternSyntax => true,
            IdentifierPatternSyntax => true,
            TuplePatternSyntax tuple => tuple.Elements.All(IsCatchAllPattern),
            _ => false
        };
    }

    private bool IsSumCatchAll(PatternSyntax pattern, BoundSumTypeDeclaration sum, out string? variantName)
    {
        variantName = null;
        switch (pattern)
        {
            case WildcardPatternSyntax:
                return true;
            case IdentifierPatternSyntax identifier:
                var name = identifier.IdentifierToken.Text;
                if (sum.Variants.Any(variant => string.Equals(variant.Name, name, StringComparison.Ordinal)))
                {
                    variantName = name;
                    return false;
                }

                return true;
            case VariantPatternSyntax variantPattern:
                variantName = variantPattern.IdentifierToken.Text;
                return false;
            default:
                return false;
        }
    }

    private static (object? Value, TypeSymbol Type) BindPatternLiteral(SyntaxToken token)
    {
        var value = token.Value;
        if (value is int)
        {
            return (value, TypeSymbol.Int);
        }

        if (value is double)
        {
            return (value, TypeSymbol.Float);
        }

        if (value is bool)
        {
            return (value, TypeSymbol.Bool);
        }

        if (value is string)
        {
            return (value, TypeSymbol.String);
        }

        return (value, TypeSymbol.Error);
    }

    private BoundSumTypeDeclaration? TryGetSumDefinition(TypeSymbol type)
    {
        return sumDefinitions.TryGetValue(type.Name, out var sum) ? sum : null;
    }

    private BoundExpression BindUnaryExpression(UnaryExpressionSyntax unary)
    {
        var operand = BindExpression(unary.Operand);
        if (operand.Type == TypeSymbol.Bool && unary.OperatorToken.Kind == TokenKind.Bang)
        {
            return new BoundUnaryExpression(operand, unary.OperatorToken.Kind, TypeSymbol.Bool);
        }

        if (operand.Type == TypeSymbol.Float)
        {
            return new BoundUnaryExpression(operand, unary.OperatorToken.Kind, TypeSymbol.Float);
        }

        if (operand.Type == TypeSymbol.Int)
        {
            return new BoundUnaryExpression(operand, unary.OperatorToken.Kind, TypeSymbol.Int);
        }

        if (operand.Type != TypeSymbol.Error)
        {
            diagnostics.Add(Diagnostic.Error(
                SourceText,
                unary.OperatorToken.Span,
                $"Operator '{unary.OperatorToken.Text}' is not defined for type '{operand.Type}'."));
        }

        return new BoundUnaryExpression(operand, unary.OperatorToken.Kind, TypeSymbol.Error);
    }

    private BoundExpression BindNameExpression(NameExpressionSyntax nameExpression)
    {
        var name = nameExpression.IdentifierToken.Text;
        var symbol = scope.TryLookup(name);
        if (symbol is null)
        {
            var functionSymbol = scope.TryLookupFunction(name);
            if (functionSymbol is not null)
            {
                return new BoundFunctionExpression(functionSymbol);
            }

            if (variantDefinitions.TryGetValue(name, out var variant))
            {
                if (variant.PayloadType is not null)
                {
                    diagnostics.Add(Diagnostic.Error(
                        SourceText,
                        nameExpression.IdentifierToken.Span,
                        $"Variant '{name}' requires a payload."));
                    return new BoundSumConstructorExpression(variant, new BoundLiteralExpression(null, TypeSymbol.Error));
                }

                return new BoundSumConstructorExpression(variant, null);
            }

            diagnostics.Add(Diagnostic.Error(
                SourceText,
                nameExpression.IdentifierToken.Span,
                $"Undefined variable '{name}'."));
            return new BoundLiteralExpression(null, TypeSymbol.Error);
        }

        TrackLambdaCapture(symbol, nameExpression.IdentifierToken.Span);

        return new BoundNameExpression(symbol);
    }

    private BoundExpression BindAssignmentExpression(AssignmentExpressionSyntax assignment)
    {
        var name = assignment.IdentifierToken.Text;
        var symbol = scope.TryLookup(name);

        if (symbol is null)
        {
            diagnostics.Add(Diagnostic.Error(
                SourceText,
                assignment.IdentifierToken.Span,
                $"Undefined variable '{name}'."));
            return BindExpression(assignment.Expression);
        }

        if (!symbol.IsMutable)
        {
            diagnostics.Add(Diagnostic.Error(
                SourceText,
                assignment.IdentifierToken.Span,
                $"Variable '{name}' is immutable."));
        }

        var boundExpression = BindExpression(assignment.Expression);
        if (boundExpression.Type != symbol.Type && boundExpression.Type != TypeSymbol.Error && symbol.Type != TypeSymbol.Error)
        {
            diagnostics.Add(Diagnostic.Error(
                SourceText,
                assignment.IdentifierToken.Span,
                $"Cannot assign expression of type '{boundExpression.Type}' to variable of type '{symbol.Type}'."));
        }

        return new BoundAssignmentExpression(symbol, boundExpression);
    }

    private BoundExpression BindInputExpression()
    {
        return new BoundInputExpression();
    }

    private BoundExpression BindCallExpression(CallExpressionSyntax call)
    {
        if (call.Callee is NameExpressionSyntax nameExpression &&
            variantDefinitions.TryGetValue(nameExpression.IdentifierToken.Text, out var variant))
        {
            if (variant.PayloadType is null)
            {
                if (call.Arguments.Count > 0)
                {
                    diagnostics.Add(Diagnostic.Error(
                        SourceText,
                        call.Span,
                        $"Variant '{variant.Name}' does not take a payload."));
                }

                return new BoundSumConstructorExpression(variant, null);
            }

            if (call.Arguments.Count != 1)
            {
                diagnostics.Add(Diagnostic.Error(
                    SourceText,
                    call.Span,
                    $"Variant '{variant.Name}' expects 1 payload."));
                return new BoundSumConstructorExpression(variant, new BoundLiteralExpression(null, TypeSymbol.Error));
            }

            var payload = BindExpression(call.Arguments[0]);
            if (payload.Type != variant.PayloadType && payload.Type != TypeSymbol.Error && variant.PayloadType != TypeSymbol.Error)
            {
                diagnostics.Add(Diagnostic.Error(
                    SourceText,
                    call.Arguments[0].Span,
                    $"Variant '{variant.Name}' expects '{variant.PayloadType}' but got '{payload.Type}'."));
            }

            return new BoundSumConstructorExpression(variant, payload);
        }

        var callee = BindExpression(call.Callee);
        var boundArguments = new List<BoundExpression>();
        foreach (var argument in call.Arguments)
        {
            boundArguments.Add(BindExpression(argument));
        }

        if (callee is BoundFunctionExpression builtin && builtin.Function.IsBuiltin)
        {
            var builtinResult = BindBuiltinCall(builtin.Function, call, boundArguments);
            if (builtinResult is not null)
            {
                return builtinResult;
            }
        }
        var parameterTypes = callee.Type.ParameterTypes;
        if (parameterTypes is null)
        {
            diagnostics.Add(Diagnostic.Error(
                SourceText,
                call.Span,
                "Expression is not callable."));
            return new BoundCallExpression(callee, boundArguments, TypeSymbol.Error);
        }

        if (parameterTypes.Count != boundArguments.Count)
        {
            diagnostics.Add(Diagnostic.Error(
                SourceText,
                call.Span,
                $"Function expects {parameterTypes.Count} arguments."));
        }
        else
        {
            for (var i = 0; i < parameterTypes.Count; i++)
            {
                var expected = parameterTypes[i];
                var actual = boundArguments[i].Type;
                if (expected != actual && actual != TypeSymbol.Error)
                {
                    diagnostics.Add(Diagnostic.Error(
                        SourceText,
                        call.Span,
                        $"Function expects '{expected}' but got '{actual}'."));
                }
            }
        }

        var returnType = callee.Type.ReturnType ?? TypeSymbol.Error;
        return new BoundCallExpression(callee, boundArguments, returnType);
    }

    private BoundExpression? BindBuiltinCall(
        FunctionSymbol function,
        CallExpressionSyntax call,
        IReadOnlyList<BoundExpression> arguments)
    {
        switch (function.Name)
        {
            case "abs":
                return BindBuiltinUnary(function, call, arguments, allowFloat: true);
            case "min":
            case "max":
                return BindBuiltinBinary(function, call, arguments, allowFloat: true);
            default:
                return null;
        }
    }

    private BoundExpression BindBuiltinUnary(
        FunctionSymbol function,
        CallExpressionSyntax call,
        IReadOnlyList<BoundExpression> arguments,
        bool allowFloat)
    {
        if (arguments.Count != 1)
        {
            diagnostics.Add(Diagnostic.Error(
                SourceText,
                call.Span,
                "Function expects 1 arguments."));
            return new BoundCallExpression(new BoundFunctionExpression(function), arguments, TypeSymbol.Error);
        }

        var argumentType = arguments[0].Type;
        if (argumentType == TypeSymbol.Int)
        {
            return new BoundCallExpression(new BoundFunctionExpression(function), arguments, TypeSymbol.Int);
        }

        if (allowFloat && argumentType == TypeSymbol.Float)
        {
            return new BoundCallExpression(new BoundFunctionExpression(function), arguments, TypeSymbol.Float);
        }

        if (argumentType != TypeSymbol.Error)
        {
            var expected = allowFloat
                ? "Int or Float"
                : TypeSymbol.Int.ToString();
            diagnostics.Add(Diagnostic.Error(
                SourceText,
                call.Span,
                $"Function expects '{expected}' but got '{argumentType}'."));
        }

        return new BoundCallExpression(new BoundFunctionExpression(function), arguments, TypeSymbol.Error);
    }

    private BoundExpression BindBuiltinBinary(
        FunctionSymbol function,
        CallExpressionSyntax call,
        IReadOnlyList<BoundExpression> arguments,
        bool allowFloat)
    {
        if (arguments.Count != 2)
        {
            diagnostics.Add(Diagnostic.Error(
                SourceText,
                call.Span,
                "Function expects 2 arguments."));
            return new BoundCallExpression(new BoundFunctionExpression(function), arguments, TypeSymbol.Error);
        }

        var leftType = arguments[0].Type;
        var rightType = arguments[1].Type;
        if (leftType == TypeSymbol.Int && rightType == TypeSymbol.Int)
        {
            return new BoundCallExpression(new BoundFunctionExpression(function), arguments, TypeSymbol.Int);
        }

        if (allowFloat && leftType == TypeSymbol.Float && rightType == TypeSymbol.Float)
        {
            return new BoundCallExpression(new BoundFunctionExpression(function), arguments, TypeSymbol.Float);
        }

        if (leftType != TypeSymbol.Error && rightType != TypeSymbol.Error)
        {
            var expected = allowFloat
                ? "Int or Float"
                : TypeSymbol.Int.ToString();
            diagnostics.Add(Diagnostic.Error(
                SourceText,
                call.Span,
                $"Function expects '{expected}' but got '{leftType}' and '{rightType}'."));
        }

        return new BoundCallExpression(new BoundFunctionExpression(function), arguments, TypeSymbol.Error);
    }

    private BoundStatement BindReturnStatement(ReturnStatementSyntax returnStatement)
    {
        var expression = returnStatement.Expression is null
            ? null
            : BindExpression(returnStatement.Expression);
        var expressionType = expression?.Type ?? TypeSymbol.Unit;

        if (currentFunction is null && lambdaStack.Count == 0)
        {
            diagnostics.Add(Diagnostic.Error(
                SourceText,
                returnStatement.ReturnKeyword.Span,
                "Return statements are only allowed inside functions."));
        }
        else
        {
            returnTypes?.Add(expressionType);
        }

        return new BoundReturnStatement(expression);
    }

    private BoundExpression BindLambdaExpression(LambdaExpressionSyntax lambda)
    {
        var previousScope = scope;
        scope = new BoundScope(previousScope);
        var context = new LambdaBindingContext(scope);
        lambdaStack.Push(context);

        var parameterSymbols = new List<VariableSymbol>();
        foreach (var parameter in lambda.Parameters)
        {
            var type = BindType(parameter.Type);
            var symbol = new VariableSymbol(parameter.IdentifierToken.Text, false, type);
            parameterSymbols.Add(symbol);
            var declared = scope.TryDeclare(symbol);
            if (declared is null)
            {
                diagnostics.Add(Diagnostic.Error(
                    SourceText,
                    parameter.IdentifierToken.Span,
                    $"Parameter '{parameter.IdentifierToken.Text}' is already declared in this scope."));
            }
        }

        var previousFunction = currentFunction;
        var previousReturnTypes = returnTypes;
        currentFunction = null;
        returnTypes = new List<TypeSymbol>();

        BoundBlockStatement body;
        if (lambda.BodyExpression is not null)
        {
            var expression = BindExpression(lambda.BodyExpression);
            returnTypes.Add(expression.Type);
            body = new BoundBlockStatement(new BoundStatement[]
            {
                new BoundReturnStatement(expression)
            });
        }
        else if (lambda.BodyBlock is not null)
        {
            body = ApplyImplicitReturn((BoundBlockStatement)BindBlockStatement(lambda.BodyBlock));
        }
        else
        {
            body = new BoundBlockStatement(Array.Empty<BoundStatement>());
        }

        var functionType = InferReturnType(TypeSymbol.Unit, body, returnTypes, allowUnitFallback: true);
        currentFunction = previousFunction;
        returnTypes = previousReturnTypes;

        lambdaStack.Pop();
        scope = previousScope;

        var captures = context.Captures.ToList();
        var parameterTypes = parameterSymbols.Select(parameter => parameter.Type).ToList();
        var lambdaType = TypeSymbol.Function(parameterTypes, functionType);
        return new BoundLambdaExpression(parameterSymbols, body, captures, lambdaType);
    }

    private void TrackLambdaCapture(VariableSymbol symbol, TextSpan span)
    {
        if (lambdaStack.Count == 0)
        {
            return;
        }

        var context = lambdaStack.Peek();
        if (IsDeclaredInScopeChain(scope, context.Scope, symbol))
        {
            return;
        }

        if (symbol.IsMutable)
        {
            diagnostics.Add(Diagnostic.Error(
                SourceText,
                span,
                $"Cannot capture mutable variable '{symbol.Name}' in lambda."));
        }

        context.Captures.Add(symbol);
    }

    private static bool IsDeclaredInScopeChain(BoundScope? current, BoundScope? stopScope, VariableSymbol symbol)
    {
        while (current is not null)
        {
            if (current.ContainsSymbol(symbol))
            {
                return true;
            }

            if (ReferenceEquals(current, stopScope))
            {
                break;
            }

            current = current.Parent;
        }

        return false;
    }

    private void InferAndValidateReturnType(
        FunctionSymbol function,
        bool hasAnnotatedReturnType,
        BoundBlockStatement body,
        TextSpan span)
    {
        var inferred = InferReturnType(function.ReturnType, body, returnTypes, allowUnitFallback: !hasAnnotatedReturnType);
        if (inferred == TypeSymbol.Error)
        {
            diagnostics.Add(Diagnostic.Error(
                SourceText,
                span,
                $"Function '{function.Name}' has inconsistent return types."));
        }

        if (hasAnnotatedReturnType)
        {
            if (inferred != function.ReturnType && inferred != TypeSymbol.Error)
            {
                diagnostics.Add(Diagnostic.Error(
                    SourceText,
                    span,
                    $"Function '{function.Name}' returns '{inferred}' but is declared as '{function.ReturnType}'."));
            }

            return;
        }

        function.SetReturnType(inferred);
    }

    private static TypeSymbol InferReturnType(
        TypeSymbol declaredReturnType,
        BoundBlockStatement body,
        List<TypeSymbol>? collectedReturnTypes,
        bool allowUnitFallback)
    {
        var returnCandidates = new List<TypeSymbol>();
        if (collectedReturnTypes is not null)
        {
            returnCandidates.AddRange(collectedReturnTypes);
        }

        var implicitReturn = GetImplicitReturnType(body);
        if (implicitReturn is not null)
        {
            returnCandidates.Add(implicitReturn);
        }

        if (returnCandidates.Count == 0)
        {
            return allowUnitFallback ? TypeSymbol.Unit : declaredReturnType;
        }

        var first = returnCandidates.First();
        foreach (var candidate in returnCandidates.Skip(1))
        {
            if (candidate != first && candidate != TypeSymbol.Error)
            {
                return TypeSymbol.Error;
            }
        }

        return first;
    }

    private static TypeSymbol? GetImplicitReturnType(BoundBlockStatement body)
    {
        if (body.Statements.Count == 0)
        {
            return null;
        }

        var last = body.Statements[^1];
        if (last is BoundExpressionStatement expressionStatement)
        {
            return expressionStatement.Expression.Type;
        }

        return null;
    }

    private BoundBlockStatement ApplyImplicitReturn(BoundBlockStatement body)
    {
        if (body.Statements.Count == 0)
        {
            return body;
        }

        var statements = body.Statements.ToList();
        if (statements[^1] is BoundExpressionStatement expressionStatement)
        {
            returnTypes?.Add(expressionStatement.Expression.Type);
            statements[^1] = new BoundReturnStatement(expressionStatement.Expression);
            return new BoundBlockStatement(statements);
        }

        return body;
    }

    private TypeSymbol BindType(TypeSyntax type)
    {
        if (type is NameTypeSyntax nameType)
        {
            var name = nameType.IdentifierToken.Text;
            var resolved = name switch
            {
                "Int" => TypeSymbol.Int,
                "Float" => TypeSymbol.Float,
                "Bool" => TypeSymbol.Bool,
                "String" => TypeSymbol.String,
                "Unit" => TypeSymbol.Unit,
                _ => recordTypes.TryGetValue(name, out var recordType)
                    ? recordType
                    : sumTypes.TryGetValue(name, out var sumType)
                        ? sumType
                        : TypeSymbol.Error
            };

            if (resolved == TypeSymbol.Error)
            {
                diagnostics.Add(Diagnostic.Error(
                    SourceText,
                    nameType.IdentifierToken.Span,
                    $"Unknown type '{name}'."));
            }

            return resolved;
        }

        return TypeSymbol.Error;
    }
}
