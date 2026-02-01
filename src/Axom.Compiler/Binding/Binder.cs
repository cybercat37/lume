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
        DeclareBuiltins();

        var statements = new List<BoundStatement>();
        var functions = new List<BoundFunctionDeclaration>();

        var functionDeclarations = syntaxTree.Root.Statements
            .OfType<FunctionDeclarationSyntax>()
            .ToList();
        DeclareFunctionSymbols(functionDeclarations);

        foreach (var statement in syntaxTree.Root.Statements)
        {
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
        return new BinderResult(new BoundProgram(functions, statements), allDiagnostics);
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

    private BoundExpression BindLiteralExpression(LiteralExpressionSyntax literal)
    {
        var value = literal.LiteralToken.Value;
        if (value is int)
        {
            return new BoundLiteralExpression(value, TypeSymbol.Int);
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

        if (left.Type == TypeSymbol.Int && right.Type == TypeSymbol.Int)
        {
            return new BoundBinaryExpression(left, binary.OperatorToken.Kind, right, TypeSymbol.Int);
        }

        if (left.Type != TypeSymbol.Error && right.Type != TypeSymbol.Error)
        {
            diagnostics.Add(Diagnostic.Error(
                SourceText,
                binary.OperatorToken.Span,
                $"Operator '{binary.OperatorToken.Text}' is not defined for types '{left.Type}' and '{right.Type}'."));
        }

        return new BoundBinaryExpression(left, binary.OperatorToken.Kind, right, TypeSymbol.Error);
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
                var name = identifier.IdentifierToken.Text;
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
            default:
                return new BoundWildcardPattern(TypeSymbol.Error);
        }
    }

    private void ReportMatchDiagnostics(MatchExpressionSyntax match, TypeSymbol targetType)
    {
        if (targetType == TypeSymbol.Error)
        {
            return;
        }

        var seenLiterals = new HashSet<object?>();
        var seenTrue = false;
        var seenFalse = false;
        var seenCatchAll = false;

        foreach (var arm in match.Arms)
        {
            var pattern = arm.Pattern;
            var isCatchAll = pattern is WildcardPatternSyntax or IdentifierPatternSyntax;
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

    private static (object? Value, TypeSymbol Type) BindPatternLiteral(SyntaxToken token)
    {
        var value = token.Value;
        if (value is int)
        {
            return (value, TypeSymbol.Int);
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

    private BoundExpression BindUnaryExpression(UnaryExpressionSyntax unary)
    {
        var operand = BindExpression(unary.Operand);
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
        var callee = BindExpression(call.Callee);
        var boundArguments = new List<BoundExpression>();
        foreach (var argument in call.Arguments)
        {
            boundArguments.Add(BindExpression(argument));
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
                "Bool" => TypeSymbol.Bool,
                "String" => TypeSymbol.String,
                "Unit" => TypeSymbol.Unit,
                _ => TypeSymbol.Error
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
