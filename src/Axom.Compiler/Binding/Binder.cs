using System.Linq;
using Axom.Compiler.Diagnostics;
using Axom.Compiler.Lexing;
using Axom.Compiler.Parsing;
using Axom.Compiler.Syntax;
using Axom.Compiler.Text;

namespace Axom.Compiler.Binding;

public sealed class Binder
{
    private readonly IReadOnlyDictionary<string, string> importedTypeAliases;
    private readonly List<Diagnostic> diagnostics = new();
    private BoundScope scope = new(null);
    private SourceText? sourceText;
    private SourceText SourceText => sourceText ?? new SourceText(string.Empty, string.Empty);
    private FunctionSymbol? currentFunction;
    private readonly Stack<LambdaBindingContext> lambdaStack = new();
    private List<TypeSymbol>? returnTypes;
    private Dictionary<string, TypeSymbol>? genericTypeParameters;
    private int scopeStatementDepth;
    private int currentScopeDepth;
    private readonly Stack<int> returnBoundaryDepths = new();
    private readonly Dictionary<string, TypeSymbol> recordTypes = new(StringComparer.Ordinal);
    private readonly Dictionary<string, BoundRecordTypeDeclaration> recordDefinitions = new(StringComparer.Ordinal);
    private readonly Dictionary<string, TypeSymbol> sumTypes = new(StringComparer.Ordinal);
    private readonly Dictionary<string, BoundSumTypeDeclaration> sumDefinitions = new(StringComparer.Ordinal);
    private readonly Dictionary<string, SumVariantSymbol> variantDefinitions = new(StringComparer.Ordinal);

    public Binder(IReadOnlyDictionary<string, string>? importedTypeAliases = null)
    {
        this.importedTypeAliases = importedTypeAliases ?? new Dictionary<string, string>(StringComparer.Ordinal);
    }

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
        scopeStatementDepth = 0;
        currentScopeDepth = 0;
        returnBoundaryDepths.Clear();
        DeclareBuiltins();

        var statements = new List<BoundStatement>();
        var functions = new List<BoundFunctionDeclaration>();
        var recordDeclarations = GetTopLevelRecordDeclarations(syntaxTree.Root.Statements).ToList();
        DeclareRecordTypeSymbols(recordDeclarations);
        var records = BindRecordTypeDeclarations(recordDeclarations);

        var sumDeclarations = GetTopLevelSumDeclarations(syntaxTree.Root.Statements).ToList();
        DeclareSumTypeSymbols(sumDeclarations);
        var sums = BindSumTypeDeclarations(sumDeclarations);

        var functionDeclarations = GetTopLevelFunctionDeclarations(syntaxTree.Root.Statements).ToList();
        DeclareFunctionSymbols(functionDeclarations);

        foreach (var statement in syntaxTree.Root.Statements)
        {
            if (statement is RecordTypeDeclarationSyntax)
            {
                continue;
            }

            if (statement is PubStatementSyntax { Declaration: RecordTypeDeclarationSyntax })
            {
                continue;
            }

            if (statement is SumTypeDeclarationSyntax)
            {
                continue;
            }

            if (statement is PubStatementSyntax { Declaration: SumTypeDeclarationSyntax })
            {
                continue;
            }

            if (statement is FunctionDeclarationSyntax functionDeclaration)
            {
                functions.Add(BindFunctionDeclaration(functionDeclaration));
                continue;
            }

            if (statement is PubStatementSyntax { Declaration: FunctionDeclarationSyntax pubFunction })
            {
                functions.Add(BindFunctionDeclaration(pubFunction));
                continue;
            }

            if (statement is ImportStatementSyntax or FromImportStatementSyntax)
            {
                continue;
            }

            if (statement is PubStatementSyntax pubStatement)
            {
                statements.Add(BindStatement(pubStatement.Declaration));
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

            var previousGenericParameters = genericTypeParameters;
            genericTypeParameters = new Dictionary<string, TypeSymbol>(StringComparer.Ordinal);
            var genericParameters = new List<TypeSymbol>();
            foreach (var typeParameter in declaration.TypeParameters)
            {
                var typeName = typeParameter.Text;
                if (genericTypeParameters.ContainsKey(typeName))
                {
                    diagnostics.Add(Diagnostic.Error(
                        SourceText,
                        typeParameter.Span,
                        $"Type parameter '{typeName}' is already declared."));
                    continue;
                }

                var typeSymbol = TypeSymbol.Generic(typeName);
                genericTypeParameters[typeName] = typeSymbol;
                genericParameters.Add(typeSymbol);
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
            var (enableLogging, timeoutMilliseconds) = AnalyzeFunctionAspects(
                declaration.Aspects,
                returnType,
                declaration.IdentifierToken.Span,
                validateTimeoutReturnType: declaration.ReturnType is not null);

            var symbol = new FunctionSymbol(
                name,
                parameters,
                genericParameters,
                returnType,
                enableLogging: enableLogging,
                timeoutMilliseconds: timeoutMilliseconds);
            var declared = scope.TryDeclareFunction(symbol);
            if (declared is null)
            {
                diagnostics.Add(Diagnostic.Error(
                    SourceText,
                    declaration.IdentifierToken.Span,
                    $"Function '{name}' is already declared in this scope."));
            }

            genericTypeParameters = previousGenericParameters;
        }
    }

    private (bool enableLogging, int? timeoutMilliseconds) AnalyzeFunctionAspects(
        IReadOnlyList<string> aspects,
        TypeSymbol returnType,
        TextSpan span,
        bool validateTimeoutReturnType)
    {
        var enableLogging = false;
        int? timeoutMilliseconds = null;

        foreach (var aspect in aspects)
        {
            if (string.Equals(aspect, "logging", StringComparison.OrdinalIgnoreCase))
            {
                enableLogging = true;
                continue;
            }

            if (TryParseTimeoutAspect(aspect, out var timeout))
            {
                if (timeoutMilliseconds is not null)
                {
                    diagnostics.Add(Diagnostic.Error(
                        SourceText,
                        span,
                        "@timeout can only be declared once per function."));
                    continue;
                }

                timeoutMilliseconds = timeout;
                continue;
            }

            if (string.Equals(aspect, "timeout", StringComparison.OrdinalIgnoreCase))
            {
                diagnostics.Add(Diagnostic.Error(
                    SourceText,
                    span,
                    "Invalid @timeout usage. Use @timeout(<positive integer milliseconds>)."));
                continue;
            }

            diagnostics.Add(Diagnostic.Error(
                SourceText,
                span,
                $"Unknown aspect '@{aspect}'."));
        }

        if (validateTimeoutReturnType
            && timeoutMilliseconds is not null
            && (returnType.ResultValueType is null || returnType.ResultErrorType != TypeSymbol.String))
        {
            diagnostics.Add(Diagnostic.Error(
                SourceText,
                span,
                "@timeout requires function return type Result<T, String>."));
            timeoutMilliseconds = null;
        }

        return (enableLogging, timeoutMilliseconds);
    }

    private static bool TryParseTimeoutAspect(string aspect, out int timeoutMilliseconds)
    {
        const string prefix = "timeout:";
        timeoutMilliseconds = 0;
        if (!aspect.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var value = aspect.Substring(prefix.Length);
        return int.TryParse(value, out timeoutMilliseconds) && timeoutMilliseconds > 0;
    }

    private void ValidateTimeoutAspectReturnType(FunctionSymbol functionSymbol, TextSpan span)
    {
        if (functionSymbol.TimeoutMilliseconds is null)
        {
            return;
        }

        if (functionSymbol.ReturnType.ResultValueType is null || functionSymbol.ReturnType.ResultErrorType != TypeSymbol.String)
        {
            diagnostics.Add(Diagnostic.Error(
                SourceText,
                span,
                "@timeout requires function return type Result<T, String>."));
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
            case ScopeStatementSyntax scopeStatement:
                scopeStatementDepth++;
                try
                {
                    return BindBlockStatement(scopeStatement.Body, isScopeBlock: true);
                }
                finally
                {
                    scopeStatementDepth--;
                }
            case ExpressionStatementSyntax expressionStatement:
                var expression = BindExpression(expressionStatement.Expression);
                if (expression.Type.ResultValueType is not null)
                {
                    diagnostics.Add(Diagnostic.Error(
                        SourceText,
                        expressionStatement.Span,
                        "Result value must be handled with '?' or 'match'."));
                }

                return new BoundExpressionStatement(expression);
            case ImportStatementSyntax:
            case FromImportStatementSyntax:
                diagnostics.Add(Diagnostic.Error(
                    SourceText,
                    statement.Span,
                    "Import statements are only allowed at module top level."));
                return new BoundExpressionStatement(new BoundLiteralExpression(null, TypeSymbol.Unit));
            case PubStatementSyntax pubStatement:
                diagnostics.Add(Diagnostic.Error(
                    SourceText,
                    pubStatement.PubKeyword.Span,
                    "pub is only allowed for top-level declarations."));
                return BindStatement(pubStatement.Declaration);
            default:
                throw new InvalidOperationException($"Unexpected statement: {statement.GetType().Name}");
        }
    }

    private static IEnumerable<RecordTypeDeclarationSyntax> GetTopLevelRecordDeclarations(IReadOnlyList<StatementSyntax> statements)
    {
        foreach (var statement in statements)
        {
            if (statement is RecordTypeDeclarationSyntax record)
            {
                yield return record;
            }
            else if (statement is PubStatementSyntax { Declaration: RecordTypeDeclarationSyntax pubRecord })
            {
                yield return pubRecord;
            }
        }
    }

    private static IEnumerable<SumTypeDeclarationSyntax> GetTopLevelSumDeclarations(IReadOnlyList<StatementSyntax> statements)
    {
        foreach (var statement in statements)
        {
            if (statement is SumTypeDeclarationSyntax sum)
            {
                yield return sum;
            }
            else if (statement is PubStatementSyntax { Declaration: SumTypeDeclarationSyntax pubSum })
            {
                yield return pubSum;
            }
        }
    }

    private static IEnumerable<FunctionDeclarationSyntax> GetTopLevelFunctionDeclarations(IReadOnlyList<StatementSyntax> statements)
    {
        foreach (var statement in statements)
        {
            if (statement is FunctionDeclarationSyntax function)
            {
                yield return function;
            }
            else if (statement is PubStatementSyntax { Declaration: FunctionDeclarationSyntax pubFunction })
            {
                yield return pubFunction;
            }
        }
    }

    private BoundStatement BindBlockStatement(BlockStatementSyntax block, bool isScopeBlock = false)
    {
        var intentAnnotation = BindIntentAnnotation(block.IntentAnnotation);
        var previousScope = scope;
        scope = new BoundScope(previousScope);
        currentScopeDepth++;
        var statements = new List<BoundStatement>();

        foreach (var statement in block.Statements)
        {
            statements.Add(BindStatement(statement));
        }

        scope = previousScope;
        currentScopeDepth--;
        return new BoundBlockStatement(statements, isScopeBlock, intentAnnotation);
    }

    private BoundFunctionDeclaration BindFunctionDeclaration(FunctionDeclarationSyntax declaration)
    {
        var name = declaration.IdentifierToken.Text;
        var previousGenericParameters = genericTypeParameters;
        genericTypeParameters = new Dictionary<string, TypeSymbol>(StringComparer.Ordinal);
        var genericParameters = new List<TypeSymbol>();
        foreach (var typeParameter in declaration.TypeParameters)
        {
            var typeName = typeParameter.Text;
            if (recordTypes.ContainsKey(typeName) || sumTypes.ContainsKey(typeName))
            {
                diagnostics.Add(Diagnostic.Error(
                    SourceText,
                    typeParameter.Span,
                    $"Type parameter '{typeName}' conflicts with existing type."));
            }

            if (genericTypeParameters.ContainsKey(typeName))
            {
                diagnostics.Add(Diagnostic.Error(
                    SourceText,
                    typeParameter.Span,
                    $"Type parameter '{typeName}' is already declared."));
                continue;
            }

            var symbol = TypeSymbol.Generic(typeName);
            genericTypeParameters[typeName] = symbol;
            genericParameters.Add(symbol);
        }

        var functionSymbol = scope.TryLookupFunction(name);
        if (functionSymbol is null)
        {
            var declaredReturnType = declaration.ReturnType is null ? TypeSymbol.Unit : BindType(declaration.ReturnType);
            var (enableLogging, timeoutMilliseconds) = AnalyzeFunctionAspects(
                declaration.Aspects,
                declaredReturnType,
                declaration.IdentifierToken.Span,
                validateTimeoutReturnType: declaration.ReturnType is not null);

            functionSymbol = new FunctionSymbol(
                name,
                declaration.Parameters.Select(parameter => new ParameterSymbol(parameter.IdentifierToken.Text, BindType(parameter.Type))).ToList(),
                genericParameters,
                declaredReturnType,
                enableLogging: enableLogging,
                timeoutMilliseconds: timeoutMilliseconds);
        }

        var previousScope = scope;
        var previousFunction = currentFunction;
        var previousReturnTypes = returnTypes;
        scope = new BoundScope(previousScope);
        currentScopeDepth++;
        currentFunction = functionSymbol;
        returnTypes = new List<TypeSymbol>();
        returnBoundaryDepths.Push(currentScopeDepth);

        var parameterSymbols = new List<VariableSymbol>();
        foreach (var parameter in functionSymbol.Parameters)
        {
            var symbol = new VariableSymbol(parameter.Name, false, parameter.Type, currentScopeDepth);
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
        ValidateTimeoutAspectReturnType(functionSymbol, declaration.IdentifierToken.Span);

        currentFunction = previousFunction;
        returnTypes = previousReturnTypes;
        scope = previousScope;
        currentScopeDepth--;
        returnBoundaryDepths.Pop();
        genericTypeParameters = previousGenericParameters;
        return new BoundFunctionDeclaration(functionSymbol, parameterSymbols, body);
    }

    private BoundStatement BindVariableDeclaration(VariableDeclarationSyntax declaration)
    {
        var intentAnnotation = BindIntentAnnotation(declaration.IntentAnnotation);
        var initializer = BindExpression(declaration.Initializer);
        var isMutable = declaration.MutKeyword is not null;

        if (declaration.Pattern is IdentifierPatternSyntax identifierPattern)
        {
            var name = identifierPattern.IdentifierToken.Text;
            var type = initializer.Type;
            VariableSymbol? declaredSymbol = null;
            if (!string.IsNullOrEmpty(name))
            {
                var symbol = new VariableSymbol(name, isMutable, type, currentScopeDepth);
                declaredSymbol = scope.TryDeclare(symbol);
                if (declaredSymbol is null)
                {
                    diagnostics.Add(Diagnostic.Error(
                        SourceText,
                        identifierPattern.IdentifierToken.Span,
                        $"Variable '{name}' is already declared in this scope."));
                }
            }

            declaredSymbol ??= new VariableSymbol(name, isMutable, type, currentScopeDepth);
            return new BoundVariableDeclaration(declaredSymbol, initializer, intentAnnotation);
        }

        if (declaration.Pattern is not TuplePatternSyntax)
        {
            diagnostics.Add(Diagnostic.Error(
                SourceText,
                declaration.Pattern.Span,
                "Only tuple patterns are supported in let declarations."));
        }

        var boundPattern = BindPattern(declaration.Pattern, initializer.Type);
        return new BoundDeconstructionStatement(boundPattern, initializer);
    }

    private BoundIntentAnnotation? BindIntentAnnotation(IntentAnnotationSyntax? annotation)
    {
        if (annotation is null)
        {
            return null;
        }

        var message = annotation.MessageToken.Value as string ?? string.Empty;
        var effects = ParseIntentEffects(message, annotation.Span);
        return new BoundIntentAnnotation(message, effects, annotation.Span);
    }

    private IReadOnlyList<string> ParseIntentEffects(string message, TextSpan span)
    {
        var normalized = message.Trim();
        if (string.IsNullOrEmpty(normalized))
        {
            diagnostics.Add(Diagnostic.Error(
                SourceText,
                span,
                "@intent must declare at least one effect tag."));
            return Array.Empty<string>();
        }

        var tokens = normalized
            .Split(new[] { ',', ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(static token => token.Trim())
            .Where(static token => token.Length > 0)
            .ToList();

        if (tokens.Count == 0)
        {
            diagnostics.Add(Diagnostic.Error(
                SourceText,
                span,
                "@intent must declare at least one effect tag."));
            return Array.Empty<string>();
        }

        var unique = new HashSet<string>(StringComparer.Ordinal);
        foreach (var token in tokens)
        {
            if (!IsValidIntentEffectTag(token))
            {
                diagnostics.Add(Diagnostic.Error(
                    SourceText,
                    span,
                    $"Invalid intent effect tag '{token}'."));
                continue;
            }

            unique.Add(token.ToLowerInvariant());
        }

        return unique.ToList();
    }

    private static bool IsValidIntentEffectTag(string token)
    {
        foreach (var character in token)
        {
            if (!char.IsLetterOrDigit(character) && character != '_' && character != '-')
            {
                return false;
            }
        }

        return true;
    }

    private void ValidateIntentCoverage(BoundIntentAnnotation? annotation, IReadOnlyCollection<string> inferredEffects)
    {
        if (annotation is null || inferredEffects.Count == 0)
        {
            return;
        }

        var declared = new HashSet<string>(annotation.Effects, StringComparer.OrdinalIgnoreCase);
        var missing = inferredEffects
            .Where(effect => !declared.Contains(effect))
            .OrderBy(effect => effect, StringComparer.Ordinal)
            .ToList();
        if (missing.Count == 0)
        {
            return;
        }

        diagnostics.Add(Diagnostic.Warning(
            SourceText,
            annotation.Span,
            $"Intent does not declare inferred effects: {string.Join(", ", missing)}."));
    }

    private static HashSet<string> InferEffects(IReadOnlyList<BoundStatement> statements)
    {
        var effects = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var statement in statements)
        {
            InferEffects(statement, effects);
        }

        return effects;
    }

    private static HashSet<string> InferEffects(BoundExpression expression)
    {
        var effects = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        InferEffects(expression, effects);
        return effects;
    }

    private static void InferEffects(BoundStatement statement, ISet<string> effects)
    {
        switch (statement)
        {
            case BoundBlockStatement block:
                foreach (var nested in block.Statements)
                {
                    InferEffects(nested, effects);
                }
                break;
            case BoundVariableDeclaration declaration:
                InferEffects(declaration.Initializer, effects);
                break;
            case BoundDeconstructionStatement deconstruction:
                InferEffects(deconstruction.Initializer, effects);
                break;
            case BoundPrintStatement print:
                effects.Add("io");
                InferEffects(print.Expression, effects);
                break;
            case BoundExpressionStatement expressionStatement:
                InferEffects(expressionStatement.Expression, effects);
                break;
            case BoundReturnStatement returnStatement when returnStatement.Expression is not null:
                InferEffects(returnStatement.Expression, effects);
                break;
        }
    }

    private static void InferEffects(BoundExpression expression, ISet<string> effects)
    {
        switch (expression)
        {
            case BoundInputExpression:
                effects.Add("io");
                break;
            case BoundDotNetCallExpression dotNetCall:
                effects.Add("dotnet");
                InferEffects(dotNetCall.TypeNameExpression, effects);
                InferEffects(dotNetCall.MethodNameExpression, effects);
                foreach (var argument in dotNetCall.Arguments)
                {
                    InferEffects(argument, effects);
                }
                break;
            case BoundSpawnExpression spawn:
                effects.Add("concurrency");
                InferEffects(spawn.Body, effects);
                break;
            case BoundJoinExpression join:
                effects.Add("concurrency");
                InferEffects(join.Expression, effects);
                break;
            case BoundChannelCreateExpression:
                effects.Add("concurrency");
                break;
            case BoundChannelSendExpression send:
                effects.Add("concurrency");
                InferEffects(send.Sender, effects);
                InferEffects(send.Value, effects);
                break;
            case BoundChannelReceiveExpression receive:
                effects.Add("concurrency");
                InferEffects(receive.Receiver, effects);
                break;
            case BoundCallExpression call:
                if (call.Callee is BoundFunctionExpression functionExpression
                    && functionExpression.Function.IsBuiltin
                    && string.Equals(functionExpression.Function.Name, "input", StringComparison.Ordinal))
                {
                    effects.Add("io");
                }

                if (call.Callee is BoundLambdaExpression lambda)
                {
                    InferEffects(lambda.Body, effects);
                }

                InferEffects(call.Callee, effects);
                foreach (var argument in call.Arguments)
                {
                    InferEffects(argument, effects);
                }
                break;
            case BoundUnaryExpression unary:
                InferEffects(unary.Operand, effects);
                break;
            case BoundBinaryExpression binary:
                InferEffects(binary.Left, effects);
                InferEffects(binary.Right, effects);
                break;
            case BoundAssignmentExpression assignment:
                InferEffects(assignment.Expression, effects);
                break;
            case BoundLambdaExpression:
                break;
            case BoundMatchExpression match:
                InferEffects(match.Expression, effects);
                foreach (var arm in match.Arms)
                {
                    if (arm.Guard is not null)
                    {
                        InferEffects(arm.Guard, effects);
                    }

                    InferEffects(arm.Expression, effects);
                }
                break;
            case BoundTupleExpression tuple:
                foreach (var element in tuple.Elements)
                {
                    InferEffects(element, effects);
                }
                break;
            case BoundListExpression list:
                foreach (var element in list.Elements)
                {
                    InferEffects(element, effects);
                }
                break;
            case BoundMapExpression map:
                foreach (var entry in map.Entries)
                {
                    InferEffects(entry.Key, effects);
                    InferEffects(entry.Value, effects);
                }
                break;
            case BoundRecordLiteralExpression record:
                foreach (var field in record.Fields)
                {
                    InferEffects(field.Expression, effects);
                }
                break;
            case BoundFieldAccessExpression fieldAccess:
                InferEffects(fieldAccess.Target, effects);
                break;
            case BoundIndexExpression index:
                InferEffects(index.Target, effects);
                InferEffects(index.Index, effects);
                break;
            case BoundQuestionExpression question:
                InferEffects(question.Expression, effects);
                break;
            case BoundUnwrapExpression unwrap:
                InferEffects(unwrap.Expression, effects);
                break;
            case BoundSumConstructorExpression sum when sum.Payload is not null:
                InferEffects(sum.Payload, effects);
                break;
        }
    }

    private BoundExpression BindExpression(ExpressionSyntax expression, TypeSymbol? expectedType = null)
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
            case PipelineExpressionSyntax pipeline:
                return BindPipelineExpression(pipeline);
            case MatchExpressionSyntax match:
                return BindMatchExpression(match);
            case TupleExpressionSyntax tuple:
                return BindTupleExpression(tuple);
            case ListExpressionSyntax list:
                return BindListExpression(list);
            case MapExpressionSyntax map:
                return BindMapExpression(map);
            case ChannelExpressionSyntax channel:
                return BindChannelExpression(channel);
            case RecordLiteralExpressionSyntax record:
                return BindRecordLiteralExpression(record);
            case RecordUpdateExpressionSyntax recordUpdate:
                return BindRecordUpdateExpression(recordUpdate);
            case FieldAccessExpressionSyntax fieldAccess:
                return BindFieldAccessExpression(fieldAccess);
            case IndexExpressionSyntax index:
                return BindIndexExpression(index);
            case QuestionExpressionSyntax question:
                return BindQuestionExpression(question);
            case SpawnExpressionSyntax spawn:
                return BindSpawnExpression(spawn);
            case UnaryExpressionSyntax unary:
                return BindUnaryExpression(unary);
            case ParenthesizedExpressionSyntax parenthesized:
                return BindExpression(parenthesized.Expression, expectedType);
            case InputExpressionSyntax:
                return BindInputExpression();
            case CallExpressionSyntax call:
                return BindCallExpression(call);
            case GenericCallExpressionSyntax genericCall:
                return BindGenericCallExpression(genericCall);
            case LambdaExpressionSyntax lambda:
                return BindLambdaExpression(lambda);
            case ShorthandLambdaExpressionSyntax shorthandLambda:
                return BindShorthandLambdaExpression(shorthandLambda, expectedType);
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

        var assignmentsByName = new Dictionary<string, BoundRecordFieldAssignment>(StringComparer.Ordinal);
        var assignedFromSpread = new HashSet<string>(StringComparer.Ordinal);
        var explicitFields = new HashSet<string>(StringComparer.Ordinal);
        var sawSpread = false;
        foreach (var entry in record.Entries)
        {
            if (entry is RecordSpreadSyntax spread)
            {
                if (sawSpread)
                {
                    diagnostics.Add(Diagnostic.Error(
                        SourceText,
                        spread.EllipsisToken.Span,
                        "Record literals support only one spread expression."));
                    continue;
                }

                if (explicitFields.Count > 0)
                {
                    diagnostics.Add(Diagnostic.Error(
                        SourceText,
                        spread.EllipsisToken.Span,
                        "Record spread cannot overwrite explicit fields. Use 'with { ... }' for updates."));
                }

                var spreadExpression = BindExpression(spread.Expression);
                if (spreadExpression.Type != definition.Type && spreadExpression.Type != TypeSymbol.Error)
                {
                    diagnostics.Add(Diagnostic.Error(
                        SourceText,
                        spread.Expression.Span,
                        $"Record spread for '{name}' expects '{definition.Type}', found '{spreadExpression.Type}'."));
                    continue;
                }

                foreach (var recordField in definition.Fields)
                {
                    assignmentsByName[recordField.Name] = new BoundRecordFieldAssignment(
                        recordField,
                        new BoundFieldAccessExpression(spreadExpression, recordField));
                    assignedFromSpread.Add(recordField.Name);
                }

                sawSpread = true;

                continue;
            }

            if (entry is not RecordFieldAssignmentSyntax field)
            {
                continue;
            }

            var fieldName = field.IdentifierToken.Text;
            var boundExpression = BindExpression(field.Expression);
            if (assignedFromSpread.Contains(fieldName))
            {
                diagnostics.Add(Diagnostic.Error(
                    SourceText,
                    field.IdentifierToken.Span,
                    $"Field '{fieldName}' cannot overwrite a spread value. Use 'with {{ {fieldName}: ... }}' for updates."));
                continue;
            }

            if (assignmentsByName.ContainsKey(fieldName) && !assignedFromSpread.Contains(fieldName))
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

            assignmentsByName[fieldName] = new BoundRecordFieldAssignment(match, boundExpression);
            explicitFields.Add(fieldName);
            assignedFromSpread.Remove(fieldName);
        }

        foreach (var field in definition.Fields)
        {
            if (!assignmentsByName.ContainsKey(field.Name))
            {
                diagnostics.Add(Diagnostic.Error(
                    SourceText,
                    record.IdentifierToken.Span,
                    $"Record '{name}' is missing required field '{field.Name}'."));
            }
        }

        var assignments = definition.Fields
            .Where(field => assignmentsByName.ContainsKey(field.Name))
            .Select(field => assignmentsByName[field.Name])
            .ToList();

        return new BoundRecordLiteralExpression(definition.Type, assignments);
    }

    private BoundExpression BindRecordUpdateExpression(RecordUpdateExpressionSyntax recordUpdate)
    {
        var target = BindExpression(recordUpdate.Target);
        if (!recordDefinitions.TryGetValue(target.Type.Name, out var definition))
        {
            diagnostics.Add(Diagnostic.Error(
                SourceText,
                recordUpdate.WithKeyword.Span,
                "Record update requires a record type target."));
            return new BoundRecordLiteralExpression(TypeSymbol.Error, Array.Empty<BoundRecordFieldAssignment>());
        }

        var explicitAssignments = new Dictionary<string, BoundRecordFieldAssignment>(StringComparer.Ordinal);
        foreach (var field in recordUpdate.Fields)
        {
            var fieldName = field.IdentifierToken.Text;
            var boundExpression = BindExpression(field.Expression);
            if (explicitAssignments.ContainsKey(fieldName))
            {
                diagnostics.Add(Diagnostic.Error(
                    SourceText,
                    field.IdentifierToken.Span,
                    $"Field '{fieldName}' is already updated in record '{definition.Type.Name}'."));
                continue;
            }

            var match = definition.Fields.FirstOrDefault(f => string.Equals(f.Name, fieldName, StringComparison.Ordinal));
            if (match is null)
            {
                diagnostics.Add(Diagnostic.Error(
                    SourceText,
                    field.IdentifierToken.Span,
                    $"Record '{definition.Type.Name}' has no field '{fieldName}'."));
                continue;
            }

            if (boundExpression.Type != match.Type && boundExpression.Type != TypeSymbol.Error && match.Type != TypeSymbol.Error)
            {
                diagnostics.Add(Diagnostic.Error(
                    SourceText,
                    field.Expression.Span,
                    $"Cannot assign expression of type '{boundExpression.Type}' to field '{fieldName}' of type '{match.Type}'."));
            }

            explicitAssignments[fieldName] = new BoundRecordFieldAssignment(match, boundExpression);
        }

        var assignments = new List<BoundRecordFieldAssignment>(definition.Fields.Count);
        foreach (var field in definition.Fields)
        {
            if (explicitAssignments.TryGetValue(field.Name, out var explicitAssignment))
            {
                assignments.Add(explicitAssignment);
                continue;
            }

            assignments.Add(new BoundRecordFieldAssignment(field, new BoundFieldAccessExpression(target, field)));
        }

        return new BoundRecordLiteralExpression(definition.Type, assignments);
    }

    private BoundExpression BindIndexExpression(IndexExpressionSyntax index)
    {
        var target = BindExpression(index.Target);
        var indexExpression = BindExpression(index.Index);

        if (target.Type.ListElementType is not null)
        {
            if (indexExpression.Type != TypeSymbol.Int && indexExpression.Type != TypeSymbol.Error)
            {
                diagnostics.Add(Diagnostic.Error(
                    SourceText,
                    index.Index.Span,
                    "List index must be Int."));
            }

            return new BoundIndexExpression(target, indexExpression, target.Type.ListElementType);
        }

        if (target.Type.MapValueType is not null)
        {
            if (indexExpression.Type != TypeSymbol.String && indexExpression.Type != TypeSymbol.Error)
            {
                diagnostics.Add(Diagnostic.Error(
                    SourceText,
                    index.Index.Span,
                    "Map index must be String."));
            }

            return new BoundIndexExpression(target, indexExpression, target.Type.MapValueType);
        }

        diagnostics.Add(Diagnostic.Error(
            SourceText,
            index.Target.Span,
            "Indexing is only supported on list and map types."));
        return new BoundIndexExpression(target, indexExpression, TypeSymbol.Error);
    }

    private BoundExpression BindQuestionExpression(QuestionExpressionSyntax question)
    {
        var expression = BindExpression(question.Expression);
        if (!TryGetOptionOrResultShape(expression.Type, out var successVariant, out var failureVariant))
        {
            diagnostics.Add(Diagnostic.Error(
                SourceText,
                question.QuestionToken.Span,
                "? can only be used on Option/Result types."));
            return new BoundQuestionExpression(expression, new SumVariantSymbol("", TypeSymbol.Error, TypeSymbol.Error), new SumVariantSymbol("", TypeSymbol.Error, null), TypeSymbol.Error);
        }

        if (currentFunction is null)
        {
            diagnostics.Add(Diagnostic.Error(
                SourceText,
                question.QuestionToken.Span,
                "? can only be used inside functions."));
            return new BoundQuestionExpression(expression, successVariant, failureVariant, TypeSymbol.Error);
        }

        if (currentFunction.ReturnType != expression.Type && currentFunction.ReturnType != TypeSymbol.Error)
        {
            diagnostics.Add(Diagnostic.Error(
                SourceText,
                question.QuestionToken.Span,
                "? requires the function to return the same Option/Result type."));
        }

        var payloadType = successVariant.PayloadType ?? TypeSymbol.Error;
        return new BoundQuestionExpression(expression, successVariant, failureVariant, payloadType);
    }

    private BoundExpression BindSpawnExpression(SpawnExpressionSyntax spawn)
    {
        if (scopeStatementDepth == 0)
        {
            diagnostics.Add(Diagnostic.Error(
                SourceText,
                spawn.SpawnKeyword.Span,
                "spawn can only be used inside a scope block."));
        }

        var body = (BoundBlockStatement)BindBlockStatement(spawn.Body);
        var inferred = InferReturnType(TypeSymbol.Unit, body, null, allowUnitFallback: true);
        return new BoundSpawnExpression(body, TypeSymbol.Task(inferred));
    }

    private BoundExpression BindChannelExpression(ChannelExpressionSyntax channel)
    {
        var elementType = BindType(channel.ElementType);
        var capacity = 64;
        if (channel.CapacityExpression is not null)
        {
            var boundCapacity = BindExpression(channel.CapacityExpression);
            if (boundCapacity is BoundLiteralExpression literal && literal.Value is int intCapacity)
            {
                if (intCapacity <= 0)
                {
                    diagnostics.Add(Diagnostic.Error(
                        SourceText,
                        channel.CapacityExpression.Span,
                        "channel capacity must be greater than zero."));
                }
                else
                {
                    capacity = intCapacity;
                }
            }
            else
            {
                diagnostics.Add(Diagnostic.Error(
                    SourceText,
                    channel.CapacityExpression.Span,
                    "channel capacity must be an integer literal."));
            }
        }

        return new BoundChannelCreateExpression(elementType, capacity);
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

        if (op == TokenKind.Plus && left.Type == TypeSymbol.String && right.Type == TypeSymbol.String)
        {
            return new BoundBinaryExpression(left, op, right, TypeSymbol.String);
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

    private BoundExpression BindPipelineExpression(PipelineExpressionSyntax pipeline)
    {
        if (pipeline.Right is CallExpressionSyntax call)
        {
            var arguments = new List<ExpressionSyntax>(call.Arguments.Count + 1) { pipeline.Left };
            arguments.AddRange(call.Arguments);
            var rewrittenCall = new CallExpressionSyntax(call.Callee, call.OpenParenToken, arguments, call.CloseParenToken);
            return BindCallExpression(rewrittenCall);
        }

        var position = pipeline.OperatorToken.Span.End;
        var openParen = SyntaxToken.Missing(TokenKind.OpenParen, position);
        var closeParen = SyntaxToken.Missing(TokenKind.CloseParen, position);
        var pipelineCall = new CallExpressionSyntax(pipeline.Right, openParen, new[] { pipeline.Left }, closeParen);
        return BindCallExpression(pipelineCall);
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
            BoundExpression? boundGuard = null;
            BoundPattern boundPattern;
            if (arm.Pattern is RelationalPatternSyntax relationalPattern)
            {
                boundPattern = new BoundWildcardPattern(valueExpression.Type);
                boundGuard = BindRelationalPatternGuard(valueExpression, relationalPattern);
            }
            else
            {
                boundPattern = BindPattern(arm.Pattern, valueExpression.Type);
            }

            if (arm.Guard is not null)
            {
                var explicitGuard = BindExpression(arm.Guard);
                if (boundGuard is null)
                {
                    boundGuard = explicitGuard;
                }
                else
                {
                    var combinedType = boundGuard.Type == TypeSymbol.Bool && explicitGuard.Type == TypeSymbol.Bool
                        ? TypeSymbol.Bool
                        : TypeSymbol.Error;
                    boundGuard = new BoundBinaryExpression(boundGuard, TokenKind.AmpersandAmpersand, explicitGuard, combinedType);
                }

                if (explicitGuard.Type != TypeSymbol.Bool && explicitGuard.Type != TypeSymbol.Error)
                {
                    diagnostics.Add(Diagnostic.Error(
                        SourceText,
                        arm.Guard.Span,
                        "Match guard must be Bool."));
                }
            }
            var boundExpression = BindExpression(arm.Expression);
            arms.Add(new BoundMatchArm(boundPattern, boundGuard, boundExpression));
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

    private BoundExpression BindRelationalPatternGuard(BoundExpression left, RelationalPatternSyntax relationalPattern)
    {
        var right = BindExpression(relationalPattern.RightExpression);
        var op = relationalPattern.OperatorToken.Kind;

        if (left.Type == TypeSymbol.Int && right.Type == TypeSymbol.Int)
        {
            return new BoundBinaryExpression(left, op, right, TypeSymbol.Bool);
        }

        if (left.Type == TypeSymbol.Float && right.Type == TypeSymbol.Float)
        {
            return new BoundBinaryExpression(left, op, right, TypeSymbol.Bool);
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
                relationalPattern.OperatorToken.Span,
                $"Operator '{relationalPattern.OperatorToken.Text}' is not defined for types '{left.Type}' and '{right.Type}'."));
        }

        return new BoundBinaryExpression(left, op, right, TypeSymbol.Error);
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
            case RecordPatternSyntax record:
                return BindRecordPattern(record, targetType);
            case RelationalPatternSyntax relational:
                diagnostics.Add(Diagnostic.Error(
                    SourceText,
                    relational.OperatorToken.Span,
                    "Relational patterns are only valid in match arms."));
                return new BoundWildcardPattern(targetType);
            default:
                return new BoundWildcardPattern(TypeSymbol.Error);
        }
    }

    private BoundPattern BindRecordPattern(RecordPatternSyntax recordPattern, TypeSymbol targetType)
    {
        var recordName = recordPattern.IdentifierToken.Text;
        if (!recordDefinitions.TryGetValue(recordName, out var definition))
        {
            diagnostics.Add(Diagnostic.Error(
                SourceText,
                recordPattern.IdentifierToken.Span,
                $"Record type '{recordName}' is not defined."));
            return new BoundRecordPattern(new BoundRecordTypeDeclaration(TypeSymbol.Error, Array.Empty<RecordFieldSymbol>()),
                Array.Empty<BoundRecordFieldPattern>(),
                TypeSymbol.Error);
        }

        if (definition.Type != targetType && targetType != TypeSymbol.Error)
        {
            diagnostics.Add(Diagnostic.Error(
                SourceText,
                recordPattern.IdentifierToken.Span,
                $"Record pattern '{recordName}' does not match '{targetType}'."));
        }

        var fields = new List<BoundRecordFieldPattern>();
        foreach (var fieldPattern in recordPattern.Fields)
        {
            var fieldName = fieldPattern.IdentifierToken.Text;
            var field = definition.Fields.FirstOrDefault(item => string.Equals(item.Name, fieldName, StringComparison.Ordinal));
            if (field is null)
            {
                diagnostics.Add(Diagnostic.Error(
                    SourceText,
                    fieldPattern.IdentifierToken.Span,
                    $"Record '{recordName}' has no field '{fieldName}'."));
                continue;
            }

            var boundFieldPattern = BindPattern(fieldPattern.Pattern, field.Type);
            fields.Add(new BoundRecordFieldPattern(field, boundFieldPattern));
        }

        return new BoundRecordPattern(definition, fields, definition.Type);
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

        var symbol = new VariableSymbol(name, false, targetType, currentScopeDepth);
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

    private BoundExpression BindListExpression(ListExpressionSyntax list)
    {
        if (list.Elements.Count == 0)
        {
            diagnostics.Add(Diagnostic.Error(
                SourceText,
                list.Span,
                "List literals cannot be empty."));
            return new BoundListExpression(Array.Empty<BoundExpression>(), TypeSymbol.Error);
        }

        var elements = new List<BoundExpression>();
        TypeSymbol? elementType = null;
        foreach (var element in list.Elements)
        {
            var bound = BindExpression(element);
            elements.Add(bound);
            if (elementType is null)
            {
                elementType = bound.Type;
            }
            else if (elementType != bound.Type && elementType != TypeSymbol.Error && bound.Type != TypeSymbol.Error)
            {
                diagnostics.Add(Diagnostic.Error(
                    SourceText,
                    element.Span,
                    "List literal elements must have the same type."));
                elementType = TypeSymbol.Error;
            }
        }

        elementType ??= TypeSymbol.Error;
        return new BoundListExpression(elements, TypeSymbol.List(elementType));
    }

    private BoundExpression BindMapExpression(MapExpressionSyntax map)
    {
        if (map.Entries.Count == 0)
        {
            diagnostics.Add(Diagnostic.Error(
                SourceText,
                map.Span,
                "Map literals cannot be empty."));
            return new BoundMapExpression(Array.Empty<BoundMapEntry>(), TypeSymbol.Error);
        }

        var entries = new List<BoundMapEntry>();
        TypeSymbol? valueType = null;
        foreach (var entry in map.Entries)
        {
            var key = BindExpression(entry.Key);
            var value = BindExpression(entry.Value);
            entries.Add(new BoundMapEntry(key, value));

            if (key.Type != TypeSymbol.String && key.Type != TypeSymbol.Error)
            {
                diagnostics.Add(Diagnostic.Error(
                    SourceText,
                    entry.Key.Span,
                    "Map keys must be String."));
            }

            if (valueType is null)
            {
                valueType = value.Type;
            }
            else if (valueType != value.Type && valueType != TypeSymbol.Error && value.Type != TypeSymbol.Error)
            {
                diagnostics.Add(Diagnostic.Error(
                    SourceText,
                    entry.Value.Span,
                    "Map literal values must have the same type."));
                valueType = TypeSymbol.Error;
            }
        }

        valueType ??= TypeSymbol.Error;
        return new BoundMapExpression(entries, TypeSymbol.Map(valueType));
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

                if (arm.Guard is not null)
                {
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

            if (arm.Guard is not null || pattern is RelationalPatternSyntax)
            {
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
            RecordPatternSyntax record => record.Fields.All(field => IsCatchAllPattern(field.Pattern)),
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
        if (sumDefinitions.TryGetValue(type.Name, out var sum))
        {
            return sum;
        }

        if (type.ResultValueType is not null && type.ResultErrorType is not null)
        {
            var variants = new[]
            {
                new SumVariantSymbol("Ok", type, type.ResultValueType),
                new SumVariantSymbol("Error", type, type.ResultErrorType)
            };
            return new BoundSumTypeDeclaration(type, variants);
        }

        return null;
    }

    private bool TryGetOptionOrResultShape(
        TypeSymbol type,
        out SumVariantSymbol successVariant,
        out SumVariantSymbol failureVariant)
    {
        successVariant = new SumVariantSymbol(string.Empty, TypeSymbol.Error, TypeSymbol.Error);
        failureVariant = new SumVariantSymbol(string.Empty, TypeSymbol.Error, null);
        var sum = TryGetSumDefinition(type);
        if (sum is null)
        {
            if (type.ResultValueType is not null && type.ResultErrorType is not null)
            {
                successVariant = new SumVariantSymbol("Ok", type, type.ResultValueType);
                failureVariant = new SumVariantSymbol("Error", type, type.ResultErrorType);
                return true;
            }

            return false;
        }

        var some = sum.Variants.FirstOrDefault(variant => string.Equals(variant.Name, "Some", StringComparison.Ordinal));
        var none = sum.Variants.FirstOrDefault(variant => string.Equals(variant.Name, "None", StringComparison.Ordinal));
        if (some is not null && none is not null)
        {
            successVariant = some;
            failureVariant = none;
            return true;
        }

        var ok = sum.Variants.FirstOrDefault(variant => string.Equals(variant.Name, "Ok", StringComparison.Ordinal));
        var err = sum.Variants.FirstOrDefault(variant =>
            string.Equals(variant.Name, "Err", StringComparison.Ordinal)
            || string.Equals(variant.Name, "Error", StringComparison.Ordinal));
        if (ok is not null && err is not null)
        {
            successVariant = ok;
            failureVariant = err;
            return true;
        }

        return false;
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
        if (IsChannelEndpointType(symbol.Type))
        {
            ReportChannelEndpointEscape(
                boundExpression,
                symbol.DeclaredScopeDepth,
                assignment.IdentifierToken.Span,
                $"Cannot assign channel endpoint to '{symbol.Name}' because it escapes its owner scope.");
        }

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

        if (call.Callee is FieldAccessExpressionSyntax fieldAccess &&
            string.Equals(fieldAccess.IdentifierToken.Text, "join", StringComparison.Ordinal) &&
            call.Arguments.Count == 0)
        {
            var target = BindExpression(fieldAccess.Target);
            if (target.Type.TaskResultType is null)
            {
                diagnostics.Add(Diagnostic.Error(
                    SourceText,
                    fieldAccess.Span,
                    "join() can only be used on task values."));
                return new BoundJoinExpression(target, TypeSymbol.Error);
            }

            return new BoundJoinExpression(target, target.Type.TaskResultType);
        }

        if (call.Callee is FieldAccessExpressionSyntax sendFieldAccess &&
            string.Equals(sendFieldAccess.IdentifierToken.Text, "send", StringComparison.Ordinal))
        {
            var sender = BindExpression(sendFieldAccess.Target);
            if (!sender.Type.IsChannelSender || sender.Type.ChannelElementType is null)
            {
                diagnostics.Add(Diagnostic.Error(
                    SourceText,
                    sendFieldAccess.Span,
                    "send() can only be used on sender values."));
                var fallbackValue = call.Arguments.Count > 0
                    ? BindExpression(call.Arguments[0])
                    : new BoundLiteralExpression(null, TypeSymbol.Error);
                return new BoundChannelSendExpression(sender, fallbackValue);
            }

            if (call.Arguments.Count != 1)
            {
                diagnostics.Add(Diagnostic.Error(
                    SourceText,
                    call.Span,
                    "send() expects 1 argument."));
                var fallbackValue = call.Arguments.Count > 0
                    ? BindExpression(call.Arguments[0])
                    : new BoundLiteralExpression(null, TypeSymbol.Error);
                return new BoundChannelSendExpression(sender, fallbackValue);
            }

            var value = BindExpression(call.Arguments[0]);
            var elementType = sender.Type.ChannelElementType;
            if (value.Type != elementType && value.Type != TypeSymbol.Error && elementType != TypeSymbol.Error)
            {
                diagnostics.Add(Diagnostic.Error(
                    SourceText,
                    call.Arguments[0].Span,
                    $"send() expects '{elementType}' but got '{value.Type}'."));
            }

            return new BoundChannelSendExpression(sender, value);
        }

        if (call.Callee is FieldAccessExpressionSyntax recvFieldAccess &&
            string.Equals(recvFieldAccess.IdentifierToken.Text, "recv", StringComparison.Ordinal) &&
            call.Arguments.Count == 0)
        {
            var receiver = BindExpression(recvFieldAccess.Target);
            if (!receiver.Type.IsChannelReceiver || receiver.Type.ChannelElementType is null)
            {
                diagnostics.Add(Diagnostic.Error(
                    SourceText,
                    recvFieldAccess.Span,
                    "recv() can only be used on receiver values."));
                return new BoundChannelReceiveExpression(receiver, TypeSymbol.Error);
            }

            return new BoundChannelReceiveExpression(receiver, TypeSymbol.Result(receiver.Type.ChannelElementType, TypeSymbol.String));
        }

        if (call.Callee is FieldAccessExpressionSyntax unwrapFieldAccess &&
            string.Equals(unwrapFieldAccess.IdentifierToken.Text, "unwrap", StringComparison.Ordinal) &&
            call.Arguments.Count == 0)
        {
            var target = BindExpression(unwrapFieldAccess.Target);
            if (TryGetOptionOrResultShape(target.Type, out var successVariant, out var failureVariant))
            {
                var payloadType = successVariant.PayloadType ?? TypeSymbol.Error;
                return new BoundUnwrapExpression(target, successVariant, failureVariant, payloadType);
            }

                diagnostics.Add(Diagnostic.Error(
                    SourceText,
                    unwrapFieldAccess.Span,
                    "unwrap() can only be used on Option/Result types."));
            return new BoundUnwrapExpression(target, new SumVariantSymbol("", TypeSymbol.Error, TypeSymbol.Error), new SumVariantSymbol("", TypeSymbol.Error, null), TypeSymbol.Error);
        }

        var callee = BindExpression(call.Callee);
        var parameterTypes = callee.Type.ParameterTypes;
        if (parameterTypes is null)
        {
            var nonCallableArguments = new List<BoundExpression>();
            foreach (var argument in call.Arguments)
            {
                nonCallableArguments.Add(BindExpression(argument));
            }

            diagnostics.Add(Diagnostic.Error(
                SourceText,
                call.Span,
                "Expression is not callable."));
            return new BoundCallExpression(callee, nonCallableArguments, TypeSymbol.Error);
        }

        var boundArguments = new List<BoundExpression>();

        if (parameterTypes.Count != call.Arguments.Count)
        {
            foreach (var argument in call.Arguments)
            {
                boundArguments.Add(BindExpression(argument));
            }

            if (callee is BoundFunctionExpression mismatchBuiltin && mismatchBuiltin.Function.IsBuiltin)
            {
                var mismatchBuiltinResult = BindBuiltinCall(mismatchBuiltin.Function, call, boundArguments);
                if (mismatchBuiltinResult is not null)
                {
                    return mismatchBuiltinResult;
                }
            }

            diagnostics.Add(Diagnostic.Error(
                SourceText,
                call.Span,
                $"Function expects {parameterTypes.Count} arguments."));
        }
        else
        {
            var substitutions = new Dictionary<TypeSymbol, TypeSymbol>();
            for (var i = 0; i < parameterTypes.Count; i++)
            {
                var expected = SubstituteGenericTypes(parameterTypes[i], substitutions);
                var boundArgument = BindExpression(call.Arguments[i], expected);
                boundArguments.Add(boundArgument);
                var actual = boundArgument.Type;
                if (!TryBindGenericTypes(expected, actual, substitutions) && actual != TypeSymbol.Error)
                {
                    diagnostics.Add(Diagnostic.Error(
                        SourceText,
                        call.Span,
                        $"Function expects '{expected}' but got '{actual}'."));
                }
            }

            if (callee is BoundFunctionExpression builtin && builtin.Function.IsBuiltin)
            {
                var builtinResult = BindBuiltinCall(builtin.Function, call, boundArguments);
                if (builtinResult is not null)
                {
                    return builtinResult;
                }
            }

            var returnType = callee.Type.ReturnType ?? TypeSymbol.Error;
            returnType = SubstituteGenericTypes(returnType, substitutions);
            return new BoundCallExpression(callee, boundArguments, returnType);
        }

        if (callee is BoundFunctionExpression fallbackBuiltin && fallbackBuiltin.Function.IsBuiltin)
        {
            var fallbackBuiltinResult = BindBuiltinCall(fallbackBuiltin.Function, call, boundArguments);
            if (fallbackBuiltinResult is not null)
            {
                return fallbackBuiltinResult;
            }
        }

        var fallbackReturnType = callee.Type.ReturnType ?? TypeSymbol.Error;
        return new BoundCallExpression(callee, boundArguments, fallbackReturnType);
    }

    private BoundExpression BindGenericCallExpression(GenericCallExpressionSyntax call)
    {
        if (call.Callee is not FieldAccessExpressionSyntax fieldAccess
            || fieldAccess.Target is not NameExpressionSyntax targetName
            || !string.Equals(targetName.IdentifierToken.Text, "dotnet", StringComparison.Ordinal))
        {
            diagnostics.Add(Diagnostic.Error(
                SourceText,
                call.Span,
                "Generic calls are only supported for dotnet.call and dotnet.try_call."));
            return new BoundLiteralExpression(null, TypeSymbol.Error);
        }

        var methodName = fieldAccess.IdentifierToken.Text;
        var isTryCall = string.Equals(methodName, "try_call", StringComparison.Ordinal);
        if (!isTryCall && !string.Equals(methodName, "call", StringComparison.Ordinal))
        {
            diagnostics.Add(Diagnostic.Error(
                SourceText,
                fieldAccess.IdentifierToken.Span,
                "Only dotnet.call and dotnet.try_call are supported."));
            return new BoundLiteralExpression(null, TypeSymbol.Error);
        }

        if (call.TypeArguments.Count != 1)
        {
            diagnostics.Add(Diagnostic.Error(
                SourceText,
                call.Span,
                "dotnet interop expects exactly one return type argument."));
            return new BoundLiteralExpression(null, TypeSymbol.Error);
        }

        if (call.Arguments.Count < 2)
        {
            diagnostics.Add(Diagnostic.Error(
                SourceText,
                call.Span,
                "dotnet interop expects at least type and method string arguments."));
            return new BoundLiteralExpression(null, TypeSymbol.Error);
        }

        var returnType = BindType(call.TypeArguments[0]);
        var typeNameExpression = BindExpression(call.Arguments[0]);
        var methodNameExpression = BindExpression(call.Arguments[1]);

        if (typeNameExpression.Type != TypeSymbol.String)
        {
            diagnostics.Add(Diagnostic.Error(
                SourceText,
                call.Arguments[0].Span,
                "dotnet type name must be a String."));
        }

        if (methodNameExpression.Type != TypeSymbol.String)
        {
            diagnostics.Add(Diagnostic.Error(
                SourceText,
                call.Arguments[1].Span,
                "dotnet method name must be a String."));
        }

        var supportsReturnType = returnType == TypeSymbol.Int
            || returnType == TypeSymbol.Float
            || returnType == TypeSymbol.Bool
            || returnType == TypeSymbol.String;
        if (!supportsReturnType && returnType != TypeSymbol.Error)
        {
            diagnostics.Add(Diagnostic.Error(
                SourceText,
                call.TypeArguments[0].Span,
                "dotnet interop supports Int, Float, Bool, and String return types."));
        }

        var arguments = new List<BoundExpression>();
        foreach (var argument in call.Arguments.Skip(2))
        {
            var bound = BindExpression(argument);
            var supportsType = bound.Type == TypeSymbol.Int
                || bound.Type == TypeSymbol.Float
                || bound.Type == TypeSymbol.Bool
                || bound.Type == TypeSymbol.String;
            if (!supportsType && bound.Type != TypeSymbol.Error)
            {
                diagnostics.Add(Diagnostic.Error(
                    SourceText,
                    argument.Span,
                    "dotnet interop arguments must be Int, Float, Bool, or String."));
            }

            arguments.Add(bound);
        }

        return new BoundDotNetCallExpression(isTryCall, returnType, typeNameExpression, methodNameExpression, arguments);
    }

    private BoundExpression? BindBuiltinCall(
        FunctionSymbol function,
        CallExpressionSyntax call,
        IReadOnlyList<BoundExpression> arguments)
    {
        switch (function.Name)
        {
            case "range":
                return BindBuiltinRange(function, call, arguments);
            case "sum":
                return BindBuiltinSum(function, call, arguments);
            case "abs":
                return BindBuiltinUnary(function, call, arguments, allowFloat: true);
            case "min":
            case "max":
                return BindBuiltinBinary(function, call, arguments, allowFloat: true);
            default:
                return null;
        }
    }

    private BoundExpression BindBuiltinRange(
        FunctionSymbol function,
        CallExpressionSyntax call,
        IReadOnlyList<BoundExpression> arguments)
    {
        if (arguments.Count != 2 && arguments.Count != 3)
        {
            diagnostics.Add(Diagnostic.Error(
                SourceText,
                call.Span,
                "range() expects 2 or 3 Int arguments."));
            return new BoundCallExpression(new BoundFunctionExpression(function), arguments, TypeSymbol.Error);
        }

        var hasTypeError = false;
        for (var index = 0; index < arguments.Count; index++)
        {
            var argumentType = arguments[index].Type;
            if (argumentType == TypeSymbol.Int || argumentType == TypeSymbol.Error)
            {
                continue;
            }

            diagnostics.Add(Diagnostic.Error(
                SourceText,
                call.Arguments[index].Span,
                $"range() expects Int arguments but got '{argumentType}'."));
            hasTypeError = true;
        }

        return new BoundCallExpression(
            new BoundFunctionExpression(function),
            arguments,
            hasTypeError ? TypeSymbol.Error : TypeSymbol.List(TypeSymbol.Int));
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

    private BoundExpression BindBuiltinSum(
        FunctionSymbol function,
        CallExpressionSyntax call,
        IReadOnlyList<BoundExpression> arguments)
    {
        if (arguments.Count != 1)
        {
            diagnostics.Add(Diagnostic.Error(
                SourceText,
                call.Span,
                "sum() expects 1 list argument."));
            return new BoundCallExpression(new BoundFunctionExpression(function), arguments, TypeSymbol.Error);
        }

        var argumentType = arguments[0].Type;
        var elementType = argumentType.ListElementType;
        if (elementType == TypeSymbol.Int || elementType == TypeSymbol.Float)
        {
            return new BoundCallExpression(new BoundFunctionExpression(function), arguments, elementType);
        }

        if (argumentType != TypeSymbol.Error)
        {
            diagnostics.Add(Diagnostic.Error(
                SourceText,
                call.Arguments[0].Span,
                "sum() expects List<Int> or List<Float>."));
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

    private static TypeSymbol SubstituteGenericTypes(TypeSymbol type, IReadOnlyDictionary<TypeSymbol, TypeSymbol> substitutions)
    {
        if (type.IsGenericParameter && substitutions.TryGetValue(type, out var mapped))
        {
            return mapped;
        }

        if (type.ListElementType is not null)
        {
            var element = SubstituteGenericTypes(type.ListElementType, substitutions);
            return TypeSymbol.List(element);
        }

        if (type.MapValueType is not null)
        {
            var value = SubstituteGenericTypes(type.MapValueType, substitutions);
            return TypeSymbol.Map(value);
        }

        if (type.ChannelElementType is not null)
        {
            var element = SubstituteGenericTypes(type.ChannelElementType, substitutions);
            if (type.IsChannelSender)
            {
                return TypeSymbol.Sender(element);
            }

            if (type.IsChannelReceiver)
            {
                return TypeSymbol.Receiver(element);
            }
        }

        if (type.ResultValueType is not null && type.ResultErrorType is not null)
        {
            var value = SubstituteGenericTypes(type.ResultValueType, substitutions);
            var error = SubstituteGenericTypes(type.ResultErrorType, substitutions);
            return TypeSymbol.Result(value, error);
        }

        if (type.TupleElementTypes is not null)
        {
            var elements = type.TupleElementTypes.Select(element => SubstituteGenericTypes(element, substitutions)).ToList();
            return TypeSymbol.Tuple(elements);
        }

        if (type.ParameterTypes is not null)
        {
            var parameterTypes = type.ParameterTypes
                .Select(parameter => SubstituteGenericTypes(parameter, substitutions))
                .ToList();
            var returnType = SubstituteGenericTypes(type.ReturnType ?? TypeSymbol.Unit, substitutions);
            return TypeSymbol.Function(parameterTypes, returnType);
        }

        return type;
    }

    private static bool TryBindGenericTypes(TypeSymbol expected, TypeSymbol actual, IDictionary<TypeSymbol, TypeSymbol> substitutions)
    {
        if (expected == TypeSymbol.Error || actual == TypeSymbol.Error)
        {
            return true;
        }

        if (expected.IsGenericParameter)
        {
            if (substitutions.TryGetValue(expected, out var existing))
            {
                return existing == actual;
            }

            substitutions[expected] = actual;
            return true;
        }

        if (expected == actual)
        {
            return true;
        }

        if (expected.ListElementType is not null)
        {
            return actual.ListElementType is not null
                && TryBindGenericTypes(expected.ListElementType, actual.ListElementType, substitutions);
        }

        if (expected.MapValueType is not null)
        {
            return actual.MapValueType is not null
                && TryBindGenericTypes(expected.MapValueType, actual.MapValueType, substitutions);
        }

        if (expected.IsChannelSender && expected.ChannelElementType is not null)
        {
            return actual.IsChannelSender
                && actual.ChannelElementType is not null
                && TryBindGenericTypes(expected.ChannelElementType, actual.ChannelElementType, substitutions);
        }

        if (expected.IsChannelReceiver && expected.ChannelElementType is not null)
        {
            return actual.IsChannelReceiver
                && actual.ChannelElementType is not null
                && TryBindGenericTypes(expected.ChannelElementType, actual.ChannelElementType, substitutions);
        }

        if (expected.ResultValueType is not null && expected.ResultErrorType is not null)
        {
            return actual.ResultValueType is not null
                && actual.ResultErrorType is not null
                && TryBindGenericTypes(expected.ResultValueType, actual.ResultValueType, substitutions)
                && TryBindGenericTypes(expected.ResultErrorType, actual.ResultErrorType, substitutions);
        }

        if (expected.TupleElementTypes is not null)
        {
            if (actual.TupleElementTypes is null || expected.TupleElementTypes.Count != actual.TupleElementTypes.Count)
            {
                return false;
            }

            for (var i = 0; i < expected.TupleElementTypes.Count; i++)
            {
                if (!TryBindGenericTypes(expected.TupleElementTypes[i], actual.TupleElementTypes[i], substitutions))
                {
                    return false;
                }
            }

            return true;
        }

        if (expected.ParameterTypes is not null)
        {
            if (actual.ParameterTypes is null || expected.ParameterTypes.Count != actual.ParameterTypes.Count)
            {
                return false;
            }

            for (var i = 0; i < expected.ParameterTypes.Count; i++)
            {
                if (!TryBindGenericTypes(expected.ParameterTypes[i], actual.ParameterTypes[i], substitutions))
                {
                    return false;
                }
            }

            return TryBindGenericTypes(expected.ReturnType ?? TypeSymbol.Unit, actual.ReturnType ?? TypeSymbol.Unit, substitutions);
        }

        return false;
    }

    private BoundStatement BindReturnStatement(ReturnStatementSyntax returnStatement)
    {
        var expression = returnStatement.Expression is null
            ? null
            : BindExpression(returnStatement.Expression);
        var expressionType = expression?.Type ?? TypeSymbol.Unit;
        if (expression is not null && returnBoundaryDepths.Count > 0)
        {
            ReportChannelEndpointEscape(
                expression,
                returnBoundaryDepths.Peek(),
                returnStatement.ReturnKeyword.Span,
                "Cannot return channel endpoint from a deeper scope.");
        }

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
        currentScopeDepth++;
        var context = new LambdaBindingContext(scope);
        lambdaStack.Push(context);
        returnBoundaryDepths.Push(currentScopeDepth);

        var parameterSymbols = new List<VariableSymbol>();
        foreach (var parameter in lambda.Parameters)
        {
            var type = BindType(parameter.Type);
            var symbol = new VariableSymbol(parameter.IdentifierToken.Text, false, type, currentScopeDepth);
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
        currentScopeDepth--;
        returnBoundaryDepths.Pop();

        var captures = context.Captures.ToList();
        var parameterTypes = parameterSymbols.Select(parameter => parameter.Type).ToList();
        var lambdaType = TypeSymbol.Function(parameterTypes, functionType);
        return new BoundLambdaExpression(parameterSymbols, body, captures, lambdaType);
    }

    private BoundExpression BindShorthandLambdaExpression(ShorthandLambdaExpressionSyntax lambda, TypeSymbol? expectedType)
    {
        if (expectedType?.ParameterTypes is null || expectedType.ParameterTypes.Count != 1)
        {
            diagnostics.Add(Diagnostic.Error(
                SourceText,
                lambda.Span,
                "Shorthand lambda requires an expected single-parameter function type."));
            return new BoundLiteralExpression(null, TypeSymbol.Error);
        }

        var parameterType = expectedType.ParameterTypes[0];
        var expectedReturnType = expectedType.ReturnType ?? TypeSymbol.Error;

        var previousScope = scope;
        scope = new BoundScope(previousScope);
        currentScopeDepth++;
        var context = new LambdaBindingContext(scope);
        lambdaStack.Push(context);
        returnBoundaryDepths.Push(currentScopeDepth);

        var parameterSymbol = new VariableSymbol(lambda.ParameterIdentifier.Text, false, parameterType, currentScopeDepth);
        var declared = scope.TryDeclare(parameterSymbol);
        if (declared is null)
        {
            diagnostics.Add(Diagnostic.Error(
                SourceText,
                lambda.ParameterIdentifier.Span,
                $"Parameter '{lambda.ParameterIdentifier.Text}' is already declared in this scope."));
        }

        var previousFunction = currentFunction;
        var previousReturnTypes = returnTypes;
        currentFunction = null;
        returnTypes = new List<TypeSymbol>();

        var bodyExpression = BindExpression(lambda.BodyExpression, expectedReturnType);
        returnTypes.Add(bodyExpression.Type);
        var body = new BoundBlockStatement(new BoundStatement[]
        {
            new BoundReturnStatement(bodyExpression)
        });

        if (expectedReturnType != TypeSymbol.Error
            && !expectedReturnType.IsGenericParameter
            && bodyExpression.Type != expectedReturnType
            && bodyExpression.Type != TypeSymbol.Error)
        {
            diagnostics.Add(Diagnostic.Error(
                SourceText,
                lambda.BodyExpression.Span,
                $"Lambda body expects '{expectedReturnType}' but got '{bodyExpression.Type}'."));
        }

        currentFunction = previousFunction;
        returnTypes = previousReturnTypes;

        lambdaStack.Pop();
        scope = previousScope;
        currentScopeDepth--;
        returnBoundaryDepths.Pop();

        var captures = context.Captures.ToList();
        var functionType = TypeSymbol.Function(new[] { parameterType }, bodyExpression.Type);
        return new BoundLambdaExpression(new[] { parameterSymbol }, body, captures, functionType);
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

    private void ReportChannelEndpointEscape(BoundExpression expression, int targetScopeDepth, TextSpan span, string message)
    {
        var endpointSymbols = new List<VariableSymbol>();
        CollectChannelEndpointSymbols(expression, endpointSymbols);
        if (endpointSymbols.Any(symbol => symbol.DeclaredScopeDepth > targetScopeDepth))
        {
            diagnostics.Add(Diagnostic.Error(SourceText, span, message));
        }
    }

    private static void CollectChannelEndpointSymbols(BoundExpression expression, List<VariableSymbol> symbols)
    {
        switch (expression)
        {
            case BoundNameExpression name when IsChannelEndpointType(name.Symbol.Type):
                symbols.Add(name.Symbol);
                return;
            case BoundAssignmentExpression assignment:
                CollectChannelEndpointSymbols(assignment.Expression, symbols);
                return;
            case BoundUnaryExpression unary:
                CollectChannelEndpointSymbols(unary.Operand, symbols);
                return;
            case BoundBinaryExpression binary:
                CollectChannelEndpointSymbols(binary.Left, symbols);
                CollectChannelEndpointSymbols(binary.Right, symbols);
                return;
            case BoundCallExpression call:
                CollectChannelEndpointSymbols(call.Callee, symbols);
                foreach (var argument in call.Arguments)
                {
                    CollectChannelEndpointSymbols(argument, symbols);
                }

                return;
            case BoundTupleExpression tuple:
                foreach (var element in tuple.Elements)
                {
                    CollectChannelEndpointSymbols(element, symbols);
                }

                return;
            case BoundListExpression list:
                foreach (var element in list.Elements)
                {
                    CollectChannelEndpointSymbols(element, symbols);
                }

                return;
            case BoundMapExpression map:
                foreach (var entry in map.Entries)
                {
                    CollectChannelEndpointSymbols(entry.Key, symbols);
                    CollectChannelEndpointSymbols(entry.Value, symbols);
                }

                return;
            case BoundIndexExpression index:
                CollectChannelEndpointSymbols(index.Target, symbols);
                CollectChannelEndpointSymbols(index.Index, symbols);
                return;
            case BoundMatchExpression match:
                CollectChannelEndpointSymbols(match.Expression, symbols);
                foreach (var arm in match.Arms)
                {
                    if (arm.Guard is not null)
                    {
                        CollectChannelEndpointSymbols(arm.Guard, symbols);
                    }

                    CollectChannelEndpointSymbols(arm.Expression, symbols);
                }

                return;
            case BoundRecordLiteralExpression record:
                foreach (var field in record.Fields)
                {
                    CollectChannelEndpointSymbols(field.Expression, symbols);
                }

                return;
            case BoundFieldAccessExpression fieldAccess:
                CollectChannelEndpointSymbols(fieldAccess.Target, symbols);
                return;
            case BoundSumConstructorExpression sum when sum.Payload is not null:
                CollectChannelEndpointSymbols(sum.Payload, symbols);
                return;
            case BoundQuestionExpression question:
                CollectChannelEndpointSymbols(question.Expression, symbols);
                return;
            case BoundUnwrapExpression unwrap:
                CollectChannelEndpointSymbols(unwrap.Expression, symbols);
                return;
            case BoundSpawnExpression:
            case BoundLambdaExpression:
            case BoundChannelCreateExpression:
            case BoundChannelSendExpression:
            case BoundChannelReceiveExpression:
            case BoundJoinExpression:
            case BoundLiteralExpression:
            case BoundInputExpression:
            case BoundFunctionExpression:
                return;
        }
    }

    private static bool IsChannelEndpointType(TypeSymbol type)
    {
        return type.IsChannelSender || type.IsChannelReceiver;
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

    private static List<TypeSymbol> CollectReturnTypes(BoundBlockStatement body)
    {
        var types = new List<TypeSymbol>();
        foreach (var statement in body.Statements)
        {
            if (statement is BoundReturnStatement returnStatement)
            {
                types.Add(returnStatement.Expression?.Type ?? TypeSymbol.Unit);
                continue;
            }

            if (statement is BoundBlockStatement block)
            {
                types.AddRange(CollectReturnTypes(block));
            }
        }

        return types;
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
        if (type is TupleTypeSyntax tupleType)
        {
            var elementTypes = tupleType.Elements.Select(BindType).ToList();
            return TypeSymbol.Tuple(elementTypes);
        }

        if (type is NameTypeSyntax nameType)
        {
            var name = nameType.IdentifierToken.Text;
            if (genericTypeParameters is not null && genericTypeParameters.TryGetValue(name, out var genericType))
            {
                return genericType;
            }

            var typeLookupName = importedTypeAliases.TryGetValue(name, out var importedAliasTarget)
                ? importedAliasTarget
                : name;

            var resolved = name switch
            {
                "Int" => TypeSymbol.Int,
                "Float" => TypeSymbol.Float,
                "Bool" => TypeSymbol.Bool,
                "String" => TypeSymbol.String,
                "Unit" => TypeSymbol.Unit,
                _ => recordTypes.TryGetValue(typeLookupName, out var recordType)
                    ? recordType
                    : sumTypes.TryGetValue(typeLookupName, out var sumType)
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
