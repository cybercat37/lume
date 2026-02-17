using System.Globalization;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Axom.Compiler.Binding;
using Axom.Compiler.Diagnostics;
using Axom.Compiler.Interop;
using Axom.Compiler.Lowering;
using Axom.Compiler.Parsing;

namespace Axom.Compiler.Interpreting;

public sealed class Interpreter
{
    private readonly Queue<string> inputBuffer = new();
    private readonly Dictionary<string, string> routeParameters = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> queryParameters = new(StringComparer.Ordinal);
    private string? requestMethod;
    private string? requestPath;

    public void SetInput(params string[] inputs)
    {
        inputBuffer.Clear();
        foreach (var input in inputs)
        {
            inputBuffer.Enqueue(input);
        }
    }

    public InterpreterResult Run(SyntaxTree syntaxTree)
    {
        var binder = new Binder();
        var bindResult = binder.Bind(syntaxTree);
        var bindingErrors = bindResult.Diagnostics
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .ToList();
        if (bindingErrors.Count > 0)
        {
            return new InterpreterResult(string.Empty, bindResult.Diagnostics);
        }

        var lowerer = new Lowerer();
        var loweredProgram = lowerer.Lower(bindResult.Program);
        var evaluator = new Evaluator(loweredProgram, inputBuffer, routeParameters, queryParameters, requestMethod, requestPath);
        var evaluationResult = evaluator.Evaluate();
        var mergedDiagnostics = bindResult.Diagnostics
            .Concat(evaluationResult.Diagnostics)
            .ToList();
        return new InterpreterResult(evaluationResult.Output, mergedDiagnostics, evaluationResult.Response);
    }

    public void SetRouteParameters(IReadOnlyDictionary<string, string> parameters)
    {
        routeParameters.Clear();
        foreach (var parameter in parameters)
        {
            routeParameters[parameter.Key] = parameter.Value;
        }
    }

    public void SetRequestContext(string method, string path)
    {
        requestMethod = method;
        requestPath = path;
    }

    public void SetQueryParameters(IReadOnlyDictionary<string, string> parameters)
    {
        queryParameters.Clear();
        foreach (var parameter in parameters)
        {
            queryParameters[parameter.Key] = parameter.Value;
        }
    }

    private sealed class Evaluator
    {
        private readonly LoweredProgram program;
        private readonly Dictionary<VariableSymbol, object?> values;
        private readonly Dictionary<FunctionSymbol, LoweredFunctionDeclaration> functions;
        private readonly StringBuilder output;
        private readonly List<Diagnostic> diagnostics;
        private readonly Queue<string> inputBuffer;
        private readonly IReadOnlyDictionary<string, string> routeParameters;
        private readonly IReadOnlyDictionary<string, string> queryParameters;
        private readonly string? requestMethod;
        private readonly string? requestPath;
        private readonly object inputLock;
        private readonly object randomLock;
        private readonly Stack<ScopeFrame> scopeFrames;
        private readonly CancellationToken cancellationToken;
        private Random random;
        private FunctionSymbol? currentFunction;
        private InterpreterHttpResponse? response;

        public Evaluator(
            LoweredProgram program,
            Queue<string> inputBuffer,
            IReadOnlyDictionary<string, string> routeParameters,
            IReadOnlyDictionary<string, string> queryParameters,
            string? requestMethod,
            string? requestPath,
            Dictionary<VariableSymbol, object?>? initialValues = null,
            object? sharedInputLock = null,
            Random? sharedRandom = null,
            object? sharedRandomLock = null,
            CancellationToken cancellationToken = default)
        {
            this.program = program;
            values = initialValues is null
                ? new Dictionary<VariableSymbol, object?>()
                : new Dictionary<VariableSymbol, object?>(initialValues);
            functions = program.Functions.ToDictionary(func => func.Symbol, func => func);
            output = new StringBuilder();
            diagnostics = new List<Diagnostic>();
            this.inputBuffer = inputBuffer;
            this.routeParameters = routeParameters;
            this.queryParameters = queryParameters;
            this.requestMethod = requestMethod;
            this.requestPath = requestPath;
            inputLock = sharedInputLock ?? new object();
            random = sharedRandom ?? new Random();
            randomLock = sharedRandomLock ?? new object();
            scopeFrames = new Stack<ScopeFrame>();
            this.cancellationToken = cancellationToken;
        }

        public InterpreterResult Evaluate()
        {
            foreach (var statement in program.Statements)
            {
                EvaluateStatement(statement);
                if (diagnostics.Count > 0)
                {
                    break;
                }
            }

            return new InterpreterResult(output.ToString().TrimEnd('\n', '\r'), diagnostics, response);
        }

        private void EvaluateStatement(LoweredStatement statement)
        {
            switch (statement)
            {
                case LoweredBlockStatement block:
                    if (block.IsScopeBlock)
                    {
                        var frame = new ScopeFrame();
                        scopeFrames.Push(frame);
                        try
                        {
                            foreach (var inner in block.Statements)
                            {
                                EvaluateStatement(inner);
                            }
                        }
                        finally
                        {
                            scopeFrames.Pop();
                            CloseScopeChannels(frame);
                            AutoJoinScope(frame);
                            frame.Dispose();
                        }

                        return;
                    }

                    foreach (var inner in block.Statements)
                    {
                        EvaluateStatement(inner);
                    }

                    return;
                case LoweredVariableDeclaration declaration:
                    values[declaration.Symbol] = EvaluateExpression(declaration.Initializer);
                    return;
                case LoweredPrintStatement print:
                    var value = EvaluateExpression(print.Expression);
                    if (diagnostics.Count == 0)
                    {
                        output.AppendLine(FormatValue(value));
                    }
                    return;
                case LoweredExpressionStatement expressionStatement:
                    EvaluateExpression(expressionStatement.Expression);
                    return;
                case LoweredReturnStatement returnStatement:
                    if (returnStatement.Expression is null)
                    {
                        ExecuteDeferredStatements(returnStatement.DeferredStatements);
                        throw new ReturnSignal(null);
                    }

                    var returnValue = EvaluateTailExpression(returnStatement.Expression);
                    ExecuteDeferredStatements(returnStatement.DeferredStatements);
                    throw new ReturnSignal(returnValue);
                case LoweredIfStatement ifStatement:
                    var conditionValue = EvaluateExpression(ifStatement.Condition);
                    if (conditionValue is bool boolValue && boolValue)
                    {
                        EvaluateStatement(ifStatement.Then);
                    }
                    else if (ifStatement.Else is not null)
                    {
                        EvaluateStatement(ifStatement.Else);
                    }

                    return;
                default:
                    throw new InvalidOperationException($"Unexpected statement: {statement.GetType().Name}");
            }
        }

        private void ExecuteDeferredStatements(IReadOnlyList<LoweredStatement> statements)
        {
            foreach (var statement in statements)
            {
                EvaluateStatement(statement);
            }
        }

        private object? EvaluateExpression(LoweredExpression expression)
        {
            switch (expression)
            {
                case LoweredLiteralExpression literal:
                    return literal.Value;
                case LoweredNameExpression name:
                    if (values.TryGetValue(name.Symbol, out var value))
                    {
                        return value;
                    }

                    return null;
                case LoweredAssignmentExpression assignment:
                    var assignedValue = EvaluateExpression(assignment.Expression);
                    values[assignment.Symbol] = assignedValue;
                    return assignedValue;
                case LoweredUnaryExpression unary:
                    return EvaluateUnaryExpression(unary);
                case LoweredBinaryExpression binary:
                    return EvaluateBinaryExpression(binary);
                case LoweredInputExpression:
                    lock (inputLock)
                    {
                        return inputBuffer.Count > 0 ? inputBuffer.Dequeue() : string.Empty;
                    }
                case LoweredCallExpression call:
                    return EvaluateCall(call);
                case LoweredFunctionExpression functionExpression:
                    return functionExpression.Function;
                case LoweredLambdaExpression lambda:
                    return EvaluateLambda(lambda);
                case LoweredTupleExpression tuple:
                    return EvaluateTupleExpression(tuple);
                case LoweredListExpression list:
                    return EvaluateListExpression(list);
                case LoweredIndexExpression index:
                    return EvaluateIndexExpression(index);
                case LoweredMapExpression map:
                    return EvaluateMapExpression(map);
                case LoweredChannelCreateExpression channelCreate:
                    return EvaluateChannelCreateExpression(channelCreate);
                case LoweredUnwrapExpression unwrap:
                    return EvaluateUnwrapExpression(unwrap);
                case LoweredSpawnExpression spawn:
                    return EvaluateSpawnExpression(spawn);
                case LoweredJoinExpression join:
                    return EvaluateJoinExpression(join);
                case LoweredChannelSendExpression send:
                    return EvaluateChannelSendExpression(send);
                case LoweredChannelReceiveExpression recv:
                    return EvaluateChannelReceiveExpression(recv);
                case LoweredDotNetCallExpression dotNet:
                    return EvaluateDotNetCallExpression(dotNet);
                case LoweredTupleAccessExpression tupleAccess:
                    return EvaluateTupleAccessExpression(tupleAccess);
                case LoweredRecordLiteralExpression record:
                    return EvaluateRecordLiteralExpression(record);
                case LoweredFieldAccessExpression fieldAccess:
                    return EvaluateFieldAccessExpression(fieldAccess);
                case LoweredSumConstructorExpression sum:
                    return EvaluateSumConstructorExpression(sum);
                case LoweredIsTupleExpression isTuple:
                    return EvaluateIsTupleExpression(isTuple);
                case LoweredIsSumExpression isSum:
                    return EvaluateIsSumExpression(isSum);
                case LoweredIsRecordExpression isRecord:
                    return EvaluateIsRecordExpression(isRecord);
                case LoweredSumTagExpression sumTag:
                    return EvaluateSumTagExpression(sumTag);
                case LoweredSumValueExpression sumValue:
                    return EvaluateSumValueExpression(sumValue);
                case LoweredBlockExpression block:
                    return EvaluateBlockExpression(block);
                case LoweredDefaultExpression defaultExpression:
                    return EvaluateDefaultExpression(defaultExpression);
                case LoweredMatchFailureExpression matchFailure:
                    return EvaluateMatchFailureExpression(matchFailure);
                default:
                    throw new InvalidOperationException($"Unexpected expression: {expression.GetType().Name}");
            }
        }

        private object? EvaluateTupleExpression(LoweredTupleExpression tuple)
        {
            return tuple.Elements.Select(EvaluateExpression).ToArray();
        }

        private object? EvaluateListExpression(LoweredListExpression list)
        {
            var valuesList = new List<object?>();
            foreach (var element in list.Elements)
            {
                valuesList.Add(EvaluateExpression(element));
            }

            return valuesList;
        }

        private object? EvaluateIndexExpression(LoweredIndexExpression index)
        {
            var target = EvaluateExpression(index.Target);
            var indexValue = EvaluateExpression(index.Index);
            if (target is List<object?> list && indexValue is int intIndex)
            {
                if (intIndex >= 0 && intIndex < list.Count)
                {
                    return list[intIndex];
                }

                diagnostics.Add(Diagnostic.Error(string.Empty, 1, 1, "List index out of range."));
                return null;
            }

            if (target is Dictionary<string, object?> map && indexValue is string key)
            {
                if (map.TryGetValue(key, out var value))
                {
                    return value;
                }

                diagnostics.Add(Diagnostic.Error(string.Empty, 1, 1, "Map key not found."));
                return null;
            }

            diagnostics.Add(Diagnostic.Error(string.Empty, 1, 1, "List index failed."));
            return null;
        }

        private object? EvaluateMapExpression(LoweredMapExpression map)
        {
            var dictionary = new Dictionary<string, object?>(StringComparer.Ordinal);
            foreach (var entry in map.Entries)
            {
                var keyValue = EvaluateExpression(entry.Key);
                var valueValue = EvaluateExpression(entry.Value);
                if (keyValue is string key)
                {
                    dictionary[key] = valueValue;
                }
                else
                {
                    diagnostics.Add(Diagnostic.Error(string.Empty, 1, 1, "Map key must be String."));
                }
            }

            return dictionary;
        }

        private object? EvaluateChannelCreateExpression(LoweredChannelCreateExpression channelCreate)
        {
            var token = scopeFrames.Count > 0
                ? scopeFrames.Peek().CancellationToken
                : cancellationToken;
            var state = new ChannelState(channelCreate.Capacity, token);
            if (scopeFrames.Count > 0)
            {
                scopeFrames.Peek().Channels.Add(state);
            }

            var sender = new ChannelSender(state);
            var receiver = new ChannelReceiver(state);
            return new object?[] { sender, receiver };
        }

        private object? EvaluateChannelSendExpression(LoweredChannelSendExpression send)
        {
            var senderValue = EvaluateExpression(send.Sender);
            var value = EvaluateExpression(send.Value);
            if (senderValue is ChannelSender sender)
            {
                if (!sender.TrySend(value, out var error))
                {
                    diagnostics.Add(Diagnostic.Error(string.Empty, 1, 1, error ?? "send failed."));
                }

                return null;
            }

            diagnostics.Add(Diagnostic.Error(string.Empty, 1, 1, "send expects a sender handle."));
            return null;
        }

        private object? EvaluateChannelReceiveExpression(LoweredChannelReceiveExpression recv)
        {
            var receiverValue = EvaluateExpression(recv.Receiver);
            if (receiverValue is ChannelReceiver receiver)
            {
                var elementType = recv.Type.ResultValueType ?? TypeSymbol.Error;
                var resultType = TypeSymbol.Result(elementType, TypeSymbol.String);
                var status = receiver.TryReceive(out var value);
                if (status == ChannelReceiveStatus.Success)
                {
                    var okVariant = new SumVariantSymbol("Ok", resultType, elementType);
                    return new SumValue(okVariant, value);
                }

                var errorVariant = new SumVariantSymbol("Error", resultType, TypeSymbol.String);
                var message = status == ChannelReceiveStatus.Cancelled ? "cancelled" : "channel closed";
                return new SumValue(errorVariant, message);
            }

            diagnostics.Add(Diagnostic.Error(string.Empty, 1, 1, "recv expects a receiver handle."));
            return null;
        }

        private object? EvaluateUnwrapExpression(LoweredUnwrapExpression unwrap)
        {
            var target = EvaluateExpression(unwrap.Target);
            if (target is SumValue sum)
            {
                if (string.Equals(sum.Variant.Name, unwrap.FailureVariant.Name, StringComparison.Ordinal))
                {
                    diagnostics.Add(Diagnostic.Error(string.Empty, 1, 1, "Unwrap failed."));
                    return null;
                }

                return sum.Payload;
            }

            diagnostics.Add(Diagnostic.Error(string.Empty, 1, 1, "Unwrap failed."));
            return null;
        }

        private object? EvaluateDotNetCallExpression(LoweredDotNetCallExpression dotNet)
        {
            var typeName = EvaluateExpression(dotNet.TypeNameExpression) as string;
            var methodName = EvaluateExpression(dotNet.MethodNameExpression) as string;
            var args = dotNet.Arguments.Select(EvaluateExpression).ToArray();

            if (string.IsNullOrEmpty(typeName) || string.IsNullOrEmpty(methodName))
            {
                return BuildDotNetResult(dotNet, isSuccess: false, null, "dotnet type and method names must be strings.");
            }

            if (!TryInvokeDotNet(typeName, methodName, args, dotNet.ReturnType, out var value, out var error))
            {
                return BuildDotNetResult(dotNet, isSuccess: false, null, error ?? "dotnet call failed.");
            }

            return BuildDotNetResult(dotNet, isSuccess: true, value, null);
        }

        private object? BuildDotNetResult(LoweredDotNetCallExpression dotNet, bool isSuccess, object? value, string? error)
        {
            if (!dotNet.IsTryCall)
            {
                if (!isSuccess)
                {
                    diagnostics.Add(Diagnostic.Error(string.Empty, 1, 1, error ?? "dotnet call failed."));
                    return null;
                }

                return value;
            }

            var resultType = TypeSymbol.Result(dotNet.ReturnType, TypeSymbol.String);
            if (isSuccess)
            {
                var okVariant = new SumVariantSymbol("Ok", resultType, dotNet.ReturnType);
                return new SumValue(okVariant, value);
            }

            var errorVariant = new SumVariantSymbol("Error", resultType, TypeSymbol.String);
            return new SumValue(errorVariant, error ?? "dotnet call failed.");
        }

        private static bool TryInvokeDotNet(
            string typeName,
            string methodName,
            object?[] args,
            TypeSymbol returnType,
            out object? value,
            out string? error)
        {
            value = null;
            error = null;

            if (!DotNetInteropWhitelist.IsTypeAllowed(typeName))
            {
                var allowed = string.Join(", ", DotNetInteropWhitelist.GetAllowedTypes());
                error = $"dotnet type '{typeName}' is not allowed. Allowed types: {allowed}.";
                return false;
            }

            if (!DotNetInteropWhitelist.IsMethodAllowed(typeName, methodName))
            {
                var allowedMethods = DotNetInteropWhitelist.GetAllowedMethods().TryGetValue(typeName, out var allowedMethodNames)
                    ? string.Join(", ", allowedMethodNames)
                    : string.Empty;
                error = $"dotnet method '{typeName}.{methodName}' is not allowed. Allowed methods: {allowedMethods}.";
                return false;
            }

            if (!DotNetInteropWhitelist.TryResolveType(typeName, out var type))
            {
                error = $"dotnet type '{typeName}' is not allowed.";
                return false;
            }

            var methods = type
                .GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Instance)
                .Where(method => string.Equals(method.Name, methodName, StringComparison.Ordinal))
                .ToList();

            foreach (var method in methods)
            {
                var parameters = method.GetParameters();
                var isStatic = method.IsStatic;
                var expectedParameterCount = isStatic ? args.Length : Math.Max(0, args.Length - 1);
                if (parameters.Length != expectedParameterCount)
                {
                    continue;
                }

                object? instance = null;
                var argumentOffset = 0;
                if (!isStatic)
                {
                    if (args.Length == 0)
                    {
                        continue;
                    }

                    if (!TryConvertArgument(args[0], type, out instance))
                    {
                        continue;
                    }

                    argumentOffset = 1;
                }

                if (!TryConvertArguments(args, argumentOffset, parameters, out var convertedArgs))
                {
                    continue;
                }

                try
                {
                    var raw = method.Invoke(instance, convertedArgs);
                    if (!TryConvertReturnValue(raw, returnType, out value))
                    {
                        error = $"dotnet return type mismatch for '{typeName}.{methodName}'.";
                        return false;
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    error = ex.InnerException?.Message ?? ex.Message;
                    return false;
                }
            }

            error = $"dotnet method '{typeName}.{methodName}' not found for provided arguments.";
            return false;
        }

        private static bool TryConvertArguments(object?[] args, int argumentOffset, System.Reflection.ParameterInfo[] parameters, out object?[] converted)
        {
            converted = new object?[parameters.Length];
            for (var i = 0; i < parameters.Length; i++)
            {
                if (!TryConvertArgument(args[i + argumentOffset], parameters[i].ParameterType, out var value))
                {
                    return false;
                }

                converted[i] = value;
            }

            return true;
        }

        private static bool TryConvertArgument(object? argument, Type targetType, out object? converted)
        {
            converted = null;
            if (targetType == typeof(object))
            {
                converted = argument;
                return true;
            }

            if (targetType == typeof(int))
            {
                if (argument is int i32)
                {
                    converted = i32;
                    return true;
                }

                if (argument is long l32 && l32 >= int.MinValue && l32 <= int.MaxValue)
                {
                    converted = (int)l32;
                    return true;
                }

                return false;
            }

            if (targetType == typeof(long))
            {
                if (argument is long l)
                {
                    converted = l;
                    return true;
                }

                if (argument is int i)
                {
                    converted = (long)i;
                    return true;
                }

                return false;
            }

            if (targetType == typeof(double))
            {
                if (argument is double d)
                {
                    converted = d;
                    return true;
                }

                if (argument is long l)
                {
                    converted = (double)l;
                    return true;
                }

                return false;
            }

            if (targetType == typeof(bool) && argument is bool b)
            {
                converted = b;
                return true;
            }

            if (targetType == typeof(string) && argument is string s)
            {
                converted = s;
                return true;
            }

            return false;
        }

        private static bool TryConvertReturnValue(object? raw, TypeSymbol returnType, out object? value)
        {
            value = null;
            if (returnType == TypeSymbol.Int)
            {
                if (raw is long l)
                {
                    value = l;
                    return true;
                }

                if (raw is int i)
                {
                    value = (long)i;
                    return true;
                }

                return false;
            }

            if (returnType == TypeSymbol.Float)
            {
                if (raw is double d)
                {
                    value = d;
                    return true;
                }

                if (raw is float f)
                {
                    value = (double)f;
                    return true;
                }

                if (raw is long l)
                {
                    value = (double)l;
                    return true;
                }

                return false;
            }

            if (returnType == TypeSymbol.Bool && raw is bool b)
            {
                value = b;
                return true;
            }

            if (returnType == TypeSymbol.String && raw is string s)
            {
                value = s;
                return true;
            }

            return false;
        }

        private object? EvaluateSpawnExpression(LoweredSpawnExpression spawn)
        {
            var snapshot = new Dictionary<VariableSymbol, object?>(values);
            var ownerScope = scopeFrames.Count > 0 ? scopeFrames.Peek() : null;
            var token = ownerScope?.CancellationToken ?? cancellationToken;
            var task = Task.Run(() =>
            {
                var evaluator = new Evaluator(program, inputBuffer, routeParameters, queryParameters, requestMethod, requestPath, snapshot, inputLock, random, randomLock, token);
                return evaluator.EvaluateSpawnBody(spawn.Body);
            }, token);

            var handle = new SpawnHandle(task, ownerScope);
            if (scopeFrames.Count > 0)
            {
                scopeFrames.Peek().Handles.Add(handle);
            }

            return handle;
        }

        private SpawnOutcome EvaluateSpawnBody(LoweredBlockExpression body)
        {
            object? value;
            try
            {
                value = EvaluateBlockExpression(body);
            }
            catch (ReturnSignal signal)
            {
                value = signal.Value;
            }

            return new SpawnOutcome(value, output.ToString(), diagnostics.ToList());
        }

        private object? EvaluateJoinExpression(LoweredJoinExpression join)
        {
            var target = EvaluateExpression(join.Expression);
            if (target is SpawnHandle handle)
            {
                return MergeSpawnOutcome(handle).Value;
            }

            diagnostics.Add(Diagnostic.Error(string.Empty, 1, 1, "join expects a task handle."));
            return null;
        }

        private void AutoJoinScope(ScopeFrame frame)
        {
            foreach (var handle in frame.Handles)
            {
                MergeSpawnOutcome(handle);
            }
        }

        private static void CloseScopeChannels(ScopeFrame frame)
        {
            foreach (var channel in frame.Channels)
            {
                channel.Close();
            }
        }

        private SpawnOutcome MergeSpawnOutcome(SpawnHandle handle)
        {
            var outcome = handle.GetOrWait();
            if (outcome.Diagnostics.Count > 0)
            {
                handle.OwnerScope?.Cancel();
            }

            if (!handle.HasMergedOutcome)
            {
                if (outcome.Diagnostics.Count > 0)
                {
                    diagnostics.AddRange(outcome.Diagnostics);
                }

                if (!string.IsNullOrEmpty(outcome.Output))
                {
                    output.Append(outcome.Output);
                }

                handle.MarkMerged();
            }

            return outcome;
        }

        private object? EvaluateTupleAccessExpression(LoweredTupleAccessExpression tupleAccess)
        {
            var target = EvaluateExpression(tupleAccess.Target);
            if (target is object?[] elements && tupleAccess.Index >= 0 && tupleAccess.Index < elements.Length)
            {
                return elements[tupleAccess.Index];
            }

            diagnostics.Add(Diagnostic.Error(string.Empty, 1, 1, "Tuple access failed."));
            return null;
        }

        private object? EvaluateIsTupleExpression(LoweredIsTupleExpression isTuple)
        {
            var target = EvaluateExpression(isTuple.Target);
            return target is object?[];
        }

        private object? EvaluateIsSumExpression(LoweredIsSumExpression isSum)
        {
            var target = EvaluateExpression(isSum.Target);
            return target is SumValue;
        }

        private object? EvaluateIsRecordExpression(LoweredIsRecordExpression isRecord)
        {
            var target = EvaluateExpression(isRecord.Target);
            return target is RecordValue record && record.Type == isRecord.RecordType;
        }

        private object? EvaluateSumTagExpression(LoweredSumTagExpression sumTag)
        {
            var target = EvaluateExpression(sumTag.Target);
            return target is SumValue sum ? sum.Variant.Name : null;
        }

        private object? EvaluateSumValueExpression(LoweredSumValueExpression sumValue)
        {
            var target = EvaluateExpression(sumValue.Target);
            return target is SumValue sum ? sum.Payload : null;
        }

        private object? EvaluateBlockExpression(LoweredBlockExpression block)
        {
            foreach (var statement in block.Statements)
            {
                EvaluateStatement(statement);
            }

            return EvaluateExpression(block.Result);
        }

        private object? EvaluateDefaultExpression(LoweredDefaultExpression defaultExpression)
        {
            return defaultExpression.Type switch
            {
                var t when t == TypeSymbol.Int => 0,
                var t when t == TypeSymbol.Float => 0.0,
                var t when t == TypeSymbol.Bool => false,
                var t when t == TypeSymbol.String => string.Empty,
                _ => null
            };
        }

        private object? EvaluateMatchFailureExpression(LoweredMatchFailureExpression matchFailure)
        {
            diagnostics.Add(Diagnostic.Error(string.Empty, 1, 1, "Non-exhaustive match expression."));
            return EvaluateDefaultExpression(new LoweredDefaultExpression(matchFailure.Type));
        }

        private object? EvaluateTailExpression(LoweredExpression expression)
        {
            if (currentFunction is not null && expression is LoweredCallExpression call)
            {
                if (call.Callee is LoweredFunctionExpression functionExpression &&
                    functionExpression.Function == currentFunction)
                {
                    var arguments = call.Arguments.Select(EvaluateExpression).ToArray();
                    throw new TailCallSignal(arguments);
                }
            }

            if (expression is LoweredBlockExpression block)
            {
                foreach (var statement in block.Statements)
                {
                    EvaluateStatement(statement);
                }

                return EvaluateTailExpression(block.Result);
            }

            return EvaluateExpression(expression);
        }

        private object? EvaluateRecordLiteralExpression(LoweredRecordLiteralExpression record)
        {
            var valuesByName = new Dictionary<string, object?>(StringComparer.Ordinal);
            foreach (var field in record.Fields)
            {
                valuesByName[field.Field.Name] = EvaluateExpression(field.Expression);
            }

            return new RecordValue(record.RecordType, valuesByName);
        }

        private object? EvaluateFieldAccessExpression(LoweredFieldAccessExpression fieldAccess)
        {
            var target = EvaluateExpression(fieldAccess.Target);
            if (target is RecordValue record && record.TryGet(fieldAccess.Field.Name, out var value))
            {
                return value;
            }

            diagnostics.Add(Diagnostic.Error(string.Empty, 1, 1, "Field access failed."));
            return null;
        }

        private object? EvaluateSumConstructorExpression(LoweredSumConstructorExpression sum)
        {
            var payload = sum.Payload is null ? null : EvaluateExpression(sum.Payload);
            return new SumValue(sum.Variant, payload);
        }


        private object? EvaluateCall(LoweredCallExpression call)
        {
            var callee = EvaluateExpression(call.Callee);
            var arguments = call.Arguments.Select(EvaluateExpression).ToArray();
            return InvokeCallable(callee, arguments);
        }

        private object? InvokeCallable(object? callee, object?[] arguments)
        {
            if (callee is FunctionSymbol functionSymbol)
            {
                if (functionSymbol.IsBuiltin)
                {
                    return EvaluateBuiltin(functionSymbol, arguments);
                }

                if (functions.TryGetValue(functionSymbol, out var declaration))
                {
                    return EvaluateUserFunction(
                        functionSymbol,
                        declaration.Parameters,
                        declaration.Body,
                        arguments,
                        new Dictionary<VariableSymbol, object?>());
                }

                return null;
            }

            if (callee is FunctionValue functionValue)
            {
                return EvaluateUserFunction(null, functionValue.Parameters, functionValue.Body, arguments, functionValue.Captures);
            }

            diagnostics.Add(Diagnostic.Error(string.Empty, 1, 1, "Expression is not callable."));
            return null;
        }

        private object? EvaluateBuiltin(FunctionSymbol functionSymbol, object?[] arguments)
        {
            switch (functionSymbol.Name)
            {
                case "println":
                case "print":
                    output.AppendLine(FormatValue(arguments[0]));
                    return null;
                case "input":
                    return inputBuffer.Count > 0 ? inputBuffer.Dequeue() : string.Empty;
                case "http":
                    if (arguments.Length == 1 && arguments[0] is string baseUrl)
                    {
                        return new HttpClientValue(baseUrl, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase), 30000, 0);
                    }

                    diagnostics.Add(Diagnostic.Error(string.Empty, 1, 1, "http expects (String baseUrl)."));
                    return null;
                case "header":
                    if (arguments.Length == 3
                        && arguments[0] is HttpClientValue headerClient
                        && arguments[1] is string clientHeaderName
                        && arguments[2] is string clientHeaderValue)
                    {
                        var clientHeaders = new Dictionary<string, string>(headerClient.Headers, StringComparer.OrdinalIgnoreCase)
                        {
                            [clientHeaderName] = clientHeaderValue
                        };
                        return new HttpClientValue(headerClient.BaseUrl, clientHeaders, headerClient.TimeoutMs, headerClient.RetryMax);
                    }

                    if (arguments.Length == 3
                        && arguments[0] is HttpRequestValue headerRequest
                        && arguments[1] is string genericRequestHeaderName
                        && arguments[2] is string genericRequestHeaderValue)
                    {
                        var requestHeaders = new Dictionary<string, string>(headerRequest.Headers, StringComparer.OrdinalIgnoreCase)
                        {
                            [genericRequestHeaderName] = genericRequestHeaderValue
                        };
                        return new HttpRequestValue(
                            headerRequest.Method,
                            headerRequest.Url,
                            requestHeaders,
                            headerRequest.Body,
                            headerRequest.TimeoutMs,
                            headerRequest.RetryMax);
                    }

                    diagnostics.Add(Diagnostic.Error(string.Empty, 1, 1, "header expects (Http|Request target, String name, String value)."));
                    return null;
                case "http_header":
                    if (arguments.Length == 3
                        && arguments[0] is HttpClientValue clientWithHeader
                        && arguments[1] is string headerName
                        && arguments[2] is string headerValue)
                    {
                        var mergedHeaders = new Dictionary<string, string>(clientWithHeader.Headers, StringComparer.OrdinalIgnoreCase)
                        {
                            [headerName] = headerValue
                        };
                        return new HttpClientValue(clientWithHeader.BaseUrl, mergedHeaders, clientWithHeader.TimeoutMs, clientWithHeader.RetryMax);
                    }

                    diagnostics.Add(Diagnostic.Error(string.Empty, 1, 1, "http_header expects (Http client, String name, String value)."));
                    return null;
                case "http_timeout":
                    if (arguments.Length == 2
                        && arguments[0] is HttpClientValue clientWithTimeout
                        && arguments[1] is int timeoutMs)
                    {
                        var normalized = timeoutMs <= 0 ? 1 : timeoutMs;
                        return new HttpClientValue(clientWithTimeout.BaseUrl, clientWithTimeout.Headers, normalized, clientWithTimeout.RetryMax);
                    }

                    diagnostics.Add(Diagnostic.Error(string.Empty, 1, 1, "http_timeout expects (Http client, Int timeoutMs)."));
                    return null;
                case "http_retry":
                    if (arguments.Length == 2
                        && arguments[0] is HttpClientValue clientWithRetry
                        && arguments[1] is int maxAttempts)
                    {
                        var normalized = maxAttempts < 0 ? 0 : maxAttempts;
                        return new HttpClientValue(clientWithRetry.BaseUrl, clientWithRetry.Headers, clientWithRetry.TimeoutMs, normalized);
                    }

                    diagnostics.Add(Diagnostic.Error(string.Empty, 1, 1, "http_retry expects (Http client, Int maxAttempts)."));
                    return null;
                case "get":
                    if (arguments.Length == 2
                        && arguments[0] is HttpClientValue getClient
                        && arguments[1] is string getPath)
                    {
                        return BuildRequest(getClient, "GET", getPath, null);
                    }

                    diagnostics.Add(Diagnostic.Error(string.Empty, 1, 1, "get expects (Http client, String path)."));
                    return null;
                case "post":
                    if (arguments.Length == 3
                        && arguments[0] is HttpClientValue postClient
                        && arguments[1] is string postPath
                        && arguments[2] is string postBody)
                    {
                        return BuildRequest(postClient, "POST", postPath, postBody);
                    }

                    diagnostics.Add(Diagnostic.Error(string.Empty, 1, 1, "post expects (Http client, String path, String body)."));
                    return null;
                case "put":
                    if (arguments.Length == 3
                        && arguments[0] is HttpClientValue putClient
                        && arguments[1] is string putPath
                        && arguments[2] is string putBody)
                    {
                        return BuildRequest(putClient, "PUT", putPath, putBody);
                    }

                    diagnostics.Add(Diagnostic.Error(string.Empty, 1, 1, "put expects (Http client, String path, String body)."));
                    return null;
                case "patch":
                    if (arguments.Length == 3
                        && arguments[0] is HttpClientValue patchClient
                        && arguments[1] is string patchPath
                        && arguments[2] is string patchBody)
                    {
                        return BuildRequest(patchClient, "PATCH", patchPath, patchBody);
                    }

                    diagnostics.Add(Diagnostic.Error(string.Empty, 1, 1, "patch expects (Http client, String path, String body)."));
                    return null;
                case "delete":
                    if (arguments.Length == 2
                        && arguments[0] is HttpClientValue deleteClient
                        && arguments[1] is string deletePath)
                    {
                        return BuildRequest(deleteClient, "DELETE", deletePath, null);
                    }

                    diagnostics.Add(Diagnostic.Error(string.Empty, 1, 1, "delete expects (Http client, String path)."));
                    return null;
                case "request_header":
                    if (arguments.Length == 3
                        && arguments[0] is HttpRequestValue requestWithHeader
                        && arguments[1] is string requestHeaderName
                        && arguments[2] is string requestHeaderValue)
                    {
                        var mergedHeaders = new Dictionary<string, string>(requestWithHeader.Headers, StringComparer.OrdinalIgnoreCase)
                        {
                            [requestHeaderName] = requestHeaderValue
                        };
                        return new HttpRequestValue(
                            requestWithHeader.Method,
                            requestWithHeader.Url,
                            mergedHeaders,
                            requestWithHeader.Body,
                            requestWithHeader.TimeoutMs,
                            requestWithHeader.RetryMax);
                    }

                    diagnostics.Add(Diagnostic.Error(string.Empty, 1, 1, "request_header expects (Request request, String name, String value)."));
                    return null;
                case "request_text":
                    if (arguments.Length == 2
                        && arguments[0] is HttpRequestValue requestWithText
                        && arguments[1] is string requestBody)
                    {
                        return new HttpRequestValue(
                            requestWithText.Method,
                            requestWithText.Url,
                            new Dictionary<string, string>(requestWithText.Headers, StringComparer.OrdinalIgnoreCase),
                            requestBody,
                            requestWithText.TimeoutMs,
                            requestWithText.RetryMax);
                    }

                    diagnostics.Add(Diagnostic.Error(string.Empty, 1, 1, "request_text expects (Request request, String body)."));
                    return null;
                case "json":
                    if (arguments.Length == 2
                        && arguments[0] is HttpRequestValue jsonRequest
                        && arguments[1] is string jsonBody)
                    {
                        var jsonHeaders = new Dictionary<string, string>(jsonRequest.Headers, StringComparer.OrdinalIgnoreCase)
                        {
                            ["Content-Type"] = "application/json"
                        };
                        return new HttpRequestValue(
                            jsonRequest.Method,
                            jsonRequest.Url,
                            jsonHeaders,
                            jsonBody,
                            jsonRequest.TimeoutMs,
                            jsonRequest.RetryMax);
                    }

                    diagnostics.Add(Diagnostic.Error(string.Empty, 1, 1, "json expects (Request request, String body)."));
                    return null;
                case "accept_json":
                    if (arguments.Length == 1
                        && arguments[0] is HttpRequestValue acceptJsonRequest)
                    {
                        var acceptHeaders = new Dictionary<string, string>(acceptJsonRequest.Headers, StringComparer.OrdinalIgnoreCase)
                        {
                            ["Accept"] = "application/json"
                        };
                        return new HttpRequestValue(
                            acceptJsonRequest.Method,
                            acceptJsonRequest.Url,
                            acceptHeaders,
                            acceptJsonRequest.Body,
                            acceptJsonRequest.TimeoutMs,
                            acceptJsonRequest.RetryMax);
                    }

                    diagnostics.Add(Diagnostic.Error(string.Empty, 1, 1, "accept_json expects (Request request)."));
                    return null;
                case "send":
                    if (arguments.Length == 1 && arguments[0] is HttpRequestValue request)
                    {
                        return SendRequest(request);
                    }

                    diagnostics.Add(Diagnostic.Error(string.Empty, 1, 1, "send expects (Request request)."));
                    return BuildHttpResponseResult(isSuccess: false, null, BuildHttpNetworkError("invalid request"));
                case "require":
                    if (arguments.Length == 2 && arguments[0] is HttpResponseValue requireResponse && arguments[1] is int expectedStatus)
                    {
                        if (requireResponse.StatusCode == expectedStatus)
                        {
                            return BuildHttpResponseResult(isSuccess: true, requireResponse, null);
                        }

                        return BuildHttpResponseResult(
                            isSuccess: false,
                            null,
                            BuildHttpStatusError($"status mismatch: expected {expectedStatus}, got {requireResponse.StatusCode}"));
                    }

                    diagnostics.Add(Diagnostic.Error(string.Empty, 1, 1, "require expects (Response response, Int statusCode)."));
                    return BuildHttpResponseResult(isSuccess: false, null, BuildHttpStatusError("invalid require invocation"));
                case "response_text":
                    if (arguments.Length == 1 && arguments[0] is HttpResponseValue responseText)
                    {
                        return BuildHttpTextResult(isSuccess: true, responseText.Body, null);
                    }

                    diagnostics.Add(Diagnostic.Error(string.Empty, 1, 1, "response_text expects (Response response)."));
                    return BuildHttpTextResult(isSuccess: false, null, BuildHttpStatusError("invalid response"));
                case "route_param":
                    if (arguments.Length == 1 && arguments[0] is string routeParamName)
                    {
                        if (routeParameters.TryGetValue(routeParamName, out var routeParamValue))
                        {
                            return BuildStringResult(isSuccess: true, routeParamValue, null);
                        }

                        return BuildStringResult(isSuccess: false, null, $"route parameter '{routeParamName}' not found");
                    }

                    return BuildStringResult(isSuccess: false, null, "route parameter name must be a string");
                case "route_param_int":
                    if (arguments.Length == 1 && arguments[0] is string routeParamIntName)
                    {
                        if (!routeParameters.TryGetValue(routeParamIntName, out var routeParamIntValue))
                        {
                            return BuildIntResult(isSuccess: false, null, $"route parameter '{routeParamIntName}' not found");
                        }

                        if (int.TryParse(routeParamIntValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedInt))
                        {
                            return BuildIntResult(isSuccess: true, parsedInt, null);
                        }

                        return BuildIntResult(isSuccess: false, null, $"route parameter '{routeParamIntName}' is not a valid Int");
                    }

                    return BuildIntResult(isSuccess: false, null, "route parameter name must be a string");
                case "route_param_float":
                    if (arguments.Length == 1 && arguments[0] is string routeParamFloatName)
                    {
                        if (!routeParameters.TryGetValue(routeParamFloatName, out var routeParamFloatValue))
                        {
                            return BuildFloatResult(isSuccess: false, null, $"route parameter '{routeParamFloatName}' not found");
                        }

                        if (double.TryParse(routeParamFloatValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedFloat))
                        {
                            return BuildFloatResult(isSuccess: true, parsedFloat, null);
                        }

                        return BuildFloatResult(isSuccess: false, null, $"route parameter '{routeParamFloatName}' is not a valid Float");
                    }

                    return BuildFloatResult(isSuccess: false, null, "route parameter name must be a string");
                case "respond":
                    if (arguments.Length == 2 && arguments[0] is int status)
                    {
                        response = new InterpreterHttpResponse(status, FormatValue(arguments[1]));
                        return null;
                    }

                    diagnostics.Add(Diagnostic.Error(string.Empty, 1, 1, "respond expects (Int status, body)."));
                    return null;
                case "request_method":
                    return requestMethod ?? "request_method is only available in serve route handlers";
                case "request_path":
                    return requestPath ?? "request_path is only available in serve route handlers";
                case "query_param":
                    if (arguments.Length == 1 && arguments[0] is string queryParamName)
                    {
                        if (queryParameters.TryGetValue(queryParamName, out var queryParamValue))
                        {
                            return BuildStringResult(isSuccess: true, queryParamValue, null);
                        }

                        return BuildStringResult(isSuccess: false, null, $"query parameter '{queryParamName}' not found");
                    }

                    return BuildStringResult(isSuccess: false, null, "query parameter name must be a string");
                case "query_param_int":
                    if (arguments.Length == 1 && arguments[0] is string queryParamIntName)
                    {
                        if (!queryParameters.TryGetValue(queryParamIntName, out var queryParamIntValue))
                        {
                            return BuildIntResult(isSuccess: false, null, $"query parameter '{queryParamIntName}' not found");
                        }

                        if (int.TryParse(queryParamIntValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedInt))
                        {
                            return BuildIntResult(isSuccess: true, parsedInt, null);
                        }

                        return BuildIntResult(isSuccess: false, null, $"query parameter '{queryParamIntName}' is not a valid Int");
                    }

                    return BuildIntResult(isSuccess: false, null, "query parameter name must be a string");
                case "query_param_float":
                    if (arguments.Length == 1 && arguments[0] is string queryParamFloatName)
                    {
                        if (!queryParameters.TryGetValue(queryParamFloatName, out var queryParamFloatValue))
                        {
                            return BuildFloatResult(isSuccess: false, null, $"query parameter '{queryParamFloatName}' not found");
                        }

                        if (double.TryParse(queryParamFloatValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedFloat))
                        {
                            return BuildFloatResult(isSuccess: true, parsedFloat, null);
                        }

                        return BuildFloatResult(isSuccess: false, null, $"query parameter '{queryParamFloatName}' is not a valid Float");
                    }

                    return BuildFloatResult(isSuccess: false, null, "query parameter name must be a string");
                case "len":
                    return arguments[0] switch
                    {
                        string text => text.Length,
                        List<object?> list => list.Count,
                        Dictionary<string, object?> map => map.Count,
                        _ => 0
                    };
                case "abs":
                    return arguments[0] switch
                    {
                        int value => Math.Abs(value),
                        double doubleAbsValue => Math.Abs(doubleAbsValue),
                        _ => 0
                    };
                case "min":
                    if (arguments[0] is int left && arguments[1] is int right)
                    {
                        return Math.Min(left, right);
                    }

                    if (arguments[0] is double leftDouble && arguments[1] is double rightDouble)
                    {
                        return Math.Min(leftDouble, rightDouble);
                    }

                    return 0;
                case "max":
                    if (arguments[0] is int leftMax && arguments[1] is int rightMax)
                    {
                        return Math.Max(leftMax, rightMax);
                    }

                    if (arguments[0] is double leftMaxDouble && arguments[1] is double rightMaxDouble)
                    {
                        return Math.Max(leftMaxDouble, rightMaxDouble);
                    }

                    return 0;
                case "float":
                    return arguments[0] is int intValue ? (double)intValue : 0.0;
                case "int":
                    return arguments[0] is double doubleValue ? (int)doubleValue : 0;
                case "str":
                    return FormatValue(arguments[0]);
                case "format":
                    if (arguments.Length == 2 && arguments[1] is string specifier)
                    {
                        return ApplyFormat(arguments[0], specifier);
                    }

                    return FormatValue(arguments[0]);
                case "sleep":
                    if (arguments[0] is int ms && ms > 0)
                    {
                        Thread.Sleep(ms);
                    }

                    return null;
                case "clear":
                    output.Clear();
                    return null;
                case "time_now_utc":
                    return DateTimeOffset.UtcNow;
                case "time_add_ms":
                    if (arguments.Length == 2 && arguments[0] is DateTimeOffset instant && arguments[1] is int deltaMs)
                    {
                        return instant.AddMilliseconds(deltaMs);
                    }

                    return DateTimeOffset.UtcNow;
                case "time_diff_ms":
                    if (arguments.Length == 2 && arguments[0] is DateTimeOffset leftInstant && arguments[1] is DateTimeOffset rightInstant)
                    {
                        return (int)(leftInstant - rightInstant).TotalMilliseconds;
                    }

                    return 0;
                case "time_to_iso":
                    if (arguments.Length == 1 && arguments[0] is DateTimeOffset utcInstant)
                    {
                        return utcInstant.ToString("O", CultureInfo.InvariantCulture);
                    }

                    return string.Empty;
                case "time_to_local_iso":
                    if (arguments.Length == 1 && arguments[0] is DateTimeOffset localInstant)
                    {
                        return localInstant.ToLocalTime().ToString("O", CultureInfo.InvariantCulture);
                    }

                    return string.Empty;
                case "time_from_iso":
                    if (arguments.Length == 1 && arguments[0] is string isoText)
                    {
                        if (DateTimeOffset.TryParse(isoText, CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind, out var parsed))
                        {
                            return new SumValue(
                                new SumVariantSymbol("Ok", TypeSymbol.Result(TypeSymbol.Instant, TypeSymbol.String), TypeSymbol.Instant),
                                parsed);
                        }
                    }

                    return new SumValue(
                        new SumVariantSymbol("Error", TypeSymbol.Result(TypeSymbol.Instant, TypeSymbol.String), TypeSymbol.String),
                        "invalid ISO-8601 instant");
                case "rand_float":
                    lock (randomLock)
                    {
                        return random.NextDouble();
                    }
                case "rand_int":
                    if (arguments[0] is int max && max > 0)
                    {
                        lock (randomLock)
                        {
                            return BuildIntResult(isSuccess: true, random.Next(max), null);
                        }
                    }

                    return BuildIntResult(isSuccess: false, null, "max must be > 0");
                case "rand_seed":
                    if (arguments[0] is int seed)
                    {
                        lock (randomLock)
                        {
                            random = new Random(seed);
                        }
                    }

                    return null;
                case "range":
                    if ((arguments.Length == 2 || arguments.Length == 3)
                        && arguments[0] is int start
                        && arguments[1] is int end)
                    {
                        var step = 1;
                        if (arguments.Length == 3)
                        {
                            if (arguments[2] is not int explicitStep || explicitStep == 0)
                            {
                                return new List<object?>();
                            }

                            step = explicitStep;
                        }

                        var values = new List<object?>();
                        if (step > 0)
                        {
                            for (var i = start; i < end; i += step)
                            {
                                values.Add(i);
                            }
                        }
                        else
                        {
                            for (var i = start; i > end; i += step)
                            {
                                values.Add(i);
                            }
                        }

                        return values;
                    }

                    return new List<object?>();
                case "map":
                    if (arguments.Length == 2 && arguments[0] is List<object?> mapItems)
                    {
                        var mapped = new List<object?>(mapItems.Count);
                        foreach (var item in mapItems)
                        {
                            mapped.Add(InvokeCallable(arguments[1], new[] { item }));
                        }

                        return mapped;
                    }

                    return new List<object?>();
                case "filter":
                    if (arguments.Length == 2 && arguments[0] is List<object?> filterItems)
                    {
                        var filtered = new List<object?>();
                        foreach (var item in filterItems)
                        {
                            if (InvokeCallable(arguments[1], new[] { item }) is bool keep && keep)
                            {
                                filtered.Add(item);
                            }
                        }

                        return filtered;
                    }

                    return new List<object?>();
                case "fold":
                    if (arguments.Length == 3 && arguments[0] is List<object?> foldItems)
                    {
                        var accumulator = arguments[1];
                        foreach (var item in foldItems)
                        {
                            accumulator = InvokeCallable(arguments[2], new[] { accumulator, item });
                        }

                        return accumulator;
                    }

                    return null;
                case "each":
                    if (arguments.Length == 2 && arguments[0] is List<object?> eachItems)
                    {
                        foreach (var item in eachItems)
                        {
                            InvokeCallable(arguments[1], new[] { item });
                        }
                    }

                    return null;
                case "take":
                    if (arguments.Length == 2 && arguments[0] is List<object?> takeItems && arguments[1] is int takeCount)
                    {
                        if (takeCount <= 0)
                        {
                            return new List<object?>();
                        }

                        return takeItems.Take(takeCount).ToList();
                    }

                    return new List<object?>();
                case "skip":
                    if (arguments.Length == 2 && arguments[0] is List<object?> skipItems && arguments[1] is int skipCount)
                    {
                        return skipItems.Skip(Math.Max(0, skipCount)).ToList();
                    }

                    return new List<object?>();
                case "take_while":
                    if (arguments.Length == 2 && arguments[0] is List<object?> takeWhileItems)
                    {
                        var taken = new List<object?>();
                        foreach (var item in takeWhileItems)
                        {
                            if (InvokeCallable(arguments[1], new[] { item }) is not bool keep || !keep)
                            {
                                break;
                            }

                            taken.Add(item);
                        }

                        return taken;
                    }

                    return new List<object?>();
                case "skip_while":
                    if (arguments.Length == 2 && arguments[0] is List<object?> skipWhileItems)
                    {
                        var startIndex = 0;
                        for (; startIndex < skipWhileItems.Count; startIndex++)
                        {
                            if (InvokeCallable(arguments[1], new[] { skipWhileItems[startIndex] }) is not bool keep || !keep)
                            {
                                break;
                            }
                        }

                        return skipWhileItems.Skip(startIndex).ToList();
                    }

                    return new List<object?>();
                case "enumerate":
                    if (arguments.Length == 1 && arguments[0] is List<object?> enumerateItems)
                    {
                        var indexed = new List<object?>(enumerateItems.Count);
                        for (var i = 0; i < enumerateItems.Count; i++)
                        {
                            indexed.Add(new object?[] { i, enumerateItems[i] });
                        }

                        return indexed;
                    }

                    return new List<object?>();
                case "count":
                    if (arguments.Length == 1 && arguments[0] is List<object?> countItems)
                    {
                        return countItems.Count;
                    }

                    return 0;
                case "sum":
                    if (arguments.Length == 1 && arguments[0] is List<object?> sumItems)
                    {
                        if (sumItems.All(item => item is int))
                        {
                            return sumItems.Cast<int>().Sum();
                        }

                        if (sumItems.All(item => item is int or double))
                        {
                            return sumItems.Sum(item => item is int i ? i : (double)item!);
                        }
                    }

                    return 0;
                case "any":
                    if (arguments.Length == 2 && arguments[0] is List<object?> anyItems)
                    {
                        foreach (var item in anyItems)
                        {
                            if (InvokeCallable(arguments[1], new[] { item }) is bool keep && keep)
                            {
                                return true;
                            }
                        }

                        return false;
                    }

                    return false;
                case "all":
                    if (arguments.Length == 2 && arguments[0] is List<object?> allItems)
                    {
                        foreach (var item in allItems)
                        {
                            if (InvokeCallable(arguments[1], new[] { item }) is not bool keep || !keep)
                            {
                                return false;
                            }
                        }

                        return true;
                    }

                    return false;
                case "result_map":
                    if (arguments.Length == 2 && arguments[0] is SumValue sumValue)
                    {
                        if (string.Equals(sumValue.Variant.Name, "Ok", StringComparison.Ordinal))
                        {
                            var mapped = InvokeCallable(arguments[1], new[] { sumValue.Payload });
                            return new SumValue(
                                new SumVariantSymbol(
                                    "Ok",
                                    TypeSymbol.Result(TypeSymbol.Error, TypeSymbol.String),
                                    TypeSymbol.Error),
                                mapped);
                        }

                        if (string.Equals(sumValue.Variant.Name, "Error", StringComparison.Ordinal)
                            || string.Equals(sumValue.Variant.Name, "Err", StringComparison.Ordinal))
                        {
                            return new SumValue(
                                new SumVariantSymbol(
                                    "Error",
                                    TypeSymbol.Result(TypeSymbol.Error, TypeSymbol.String),
                                    TypeSymbol.String),
                                sumValue.Payload);
                        }
                    }

                    return arguments.Length > 0 ? arguments[0] : null;
                case "zip":
                    if (arguments.Length == 2 && arguments[0] is List<object?> leftItems && arguments[1] is List<object?> rightItems)
                    {
                        var length = Math.Min(leftItems.Count, rightItems.Count);
                        var zipped = new List<object?>(length);
                        for (var i = 0; i < length; i++)
                        {
                            zipped.Add(new object?[] { leftItems[i], rightItems[i] });
                        }

                        return zipped;
                    }

                    return new List<object?>();
                case "zip_with":
                    if (arguments.Length == 3 && arguments[0] is List<object?> zipLeft && arguments[1] is List<object?> zipRight)
                    {
                        var length = Math.Min(zipLeft.Count, zipRight.Count);
                        var combined = new List<object?>(length);
                        for (var i = 0; i < length; i++)
                        {
                            combined.Add(InvokeCallable(arguments[2], new[] { zipLeft[i], zipRight[i] }));
                        }

                        return combined;
                    }

                    return new List<object?>();
                default:
                    return null;
            }
        }

        private FunctionValue EvaluateLambda(LoweredLambdaExpression lambda)
        {
            var captures = new Dictionary<VariableSymbol, object?>();
            foreach (var capture in lambda.Captures)
            {
                if (values.TryGetValue(capture, out var value))
                {
                    captures[capture] = value;
                }
            }

            return new FunctionValue(lambda.Parameters, lambda.Body, captures);
        }

        private object? EvaluateUserFunction(
            FunctionSymbol? functionSymbol,
            IReadOnlyList<VariableSymbol> parameters,
            LoweredBlockStatement body,
            object?[] arguments,
            IDictionary<VariableSymbol, object?> captures)
        {
            var previousValues = new Dictionary<VariableSymbol, object?>();
            var modifiedSymbols = new HashSet<VariableSymbol>();
            var previousFunction = currentFunction;

            foreach (var capture in captures)
            {
                if (values.TryGetValue(capture.Key, out var existing))
                {
                    previousValues[capture.Key] = existing;
                }

                values[capture.Key] = capture.Value;
                modifiedSymbols.Add(capture.Key);
            }

            for (var i = 0; i < parameters.Count && i < arguments.Length; i++)
            {
                var parameter = parameters[i];
                if (values.TryGetValue(parameter, out var existing))
                {
                    previousValues[parameter] = existing;
                }

                values[parameter] = arguments[i];
                modifiedSymbols.Add(parameter);
            }

            object? result = null;
            var timeoutMilliseconds = functionSymbol?.TimeoutMilliseconds;
            Stopwatch? timeoutStopwatch = null;
            if (timeoutMilliseconds is not null)
            {
                timeoutStopwatch = Stopwatch.StartNew();
            }

            if (functionSymbol?.EnableLogging == true)
            {
                var renderedArguments = string.Join(", ", arguments.Select(FormatValue));
                output.AppendLine($"{LogTimestamp()} [log] call {functionSymbol.Name}({renderedArguments})");
            }

            try
            {
                currentFunction = functionSymbol;
                var currentArguments = arguments;
                while (true)
                {
                    for (var i = 0; i < parameters.Count && i < currentArguments.Length; i++)
                    {
                        var parameter = parameters[i];
                        values[parameter] = currentArguments[i];
                    }

                    try
                    {
                        foreach (var statement in body.Statements)
                        {
                            EvaluateStatement(statement);
                        }

                        result = null;
                        break;
                    }
                    catch (TailCallSignal signal)
                    {
                        currentArguments = signal.Arguments;
                        continue;
                    }
                }
            }
            catch (ReturnSignal signal)
            {
                result = signal.Value;
            }
            finally
            {
                if (timeoutMilliseconds is not null
                    && timeoutStopwatch is not null
                    && timeoutStopwatch.ElapsedMilliseconds > timeoutMilliseconds.Value)
                {
                    result = BuildTimeoutResult(functionSymbol?.ReturnType, timeoutMilliseconds.Value, result);
                }

                currentFunction = previousFunction;
                foreach (var symbol in modifiedSymbols)
                {
                    if (previousValues.TryGetValue(symbol, out var previous))
                    {
                        values[symbol] = previous;
                    }
                    else
                    {
                        values.Remove(symbol);
                    }
                }

                if (functionSymbol?.EnableLogging == true)
                {
                    output.AppendLine($"{LogTimestamp()} [log] return {functionSymbol.Name} => {FormatValue(result)}");
                }
            }

            return result;
        }

        private object? EvaluateUnaryExpression(LoweredUnaryExpression unary)
        {
            var operand = EvaluateExpression(unary.Operand);
            if (operand is int intValue)
            {
                return unary.OperatorKind == Lexing.TokenKind.Minus
                    ? -intValue
                    : intValue;
            }

            if (operand is double doubleValue)
            {
                return unary.OperatorKind == Lexing.TokenKind.Minus
                    ? -doubleValue
                    : doubleValue;
            }

            if (operand is bool boolValue && unary.OperatorKind == Lexing.TokenKind.Bang)
            {
                return !boolValue;
            }

            return null;
        }

        private object? EvaluateBinaryExpression(LoweredBinaryExpression binary)
        {
            if (binary.OperatorKind is Lexing.TokenKind.AmpersandAmpersand or Lexing.TokenKind.PipePipe)
            {
                var leftValue = EvaluateExpression(binary.Left);
                if (leftValue is not bool leftBool)
                {
                    return null;
                }

                if (binary.OperatorKind == Lexing.TokenKind.AmpersandAmpersand)
                {
                    if (!leftBool)
                    {
                        return false;
                    }

                    var rightValue = EvaluateExpression(binary.Right);
                    return rightValue is bool rightBool && leftBool && rightBool;
                }

                if (leftBool)
                {
                    return true;
                }

                var rightOrValue = EvaluateExpression(binary.Right);
                return rightOrValue is bool rightOrBool && rightOrBool;
            }

            var left = EvaluateExpression(binary.Left);
            var right = EvaluateExpression(binary.Right);

            if (left is int leftInt && right is int rightInt)
            {
                if (binary.OperatorKind == Lexing.TokenKind.Slash && rightInt == 0)
                {
                    diagnostics.Add(Diagnostic.Error(string.Empty, 1, 1, "Division by zero."));
                    return 0;
                }

                return binary.OperatorKind switch
                {
                    Lexing.TokenKind.Plus => leftInt + rightInt,
                    Lexing.TokenKind.Minus => leftInt - rightInt,
                    Lexing.TokenKind.Star => leftInt * rightInt,
                    Lexing.TokenKind.Slash => leftInt / rightInt,
                    Lexing.TokenKind.EqualEqual => leftInt == rightInt,
                    Lexing.TokenKind.BangEqual => leftInt != rightInt,
                    Lexing.TokenKind.Less => leftInt < rightInt,
                    Lexing.TokenKind.LessOrEqual => leftInt <= rightInt,
                    Lexing.TokenKind.Greater => leftInt > rightInt,
                    Lexing.TokenKind.GreaterOrEqual => leftInt >= rightInt,
                    _ => null
                };
            }

            if (left is double leftDouble && right is double rightDouble)
            {
                if (binary.OperatorKind == Lexing.TokenKind.Slash && rightDouble == 0)
                {
                    diagnostics.Add(Diagnostic.Error(string.Empty, 1, 1, "Division by zero."));
                    return 0.0;
                }

                return binary.OperatorKind switch
                {
                    Lexing.TokenKind.Plus => leftDouble + rightDouble,
                    Lexing.TokenKind.Minus => leftDouble - rightDouble,
                    Lexing.TokenKind.Star => leftDouble * rightDouble,
                    Lexing.TokenKind.Slash => leftDouble / rightDouble,
                    Lexing.TokenKind.EqualEqual => leftDouble == rightDouble,
                    Lexing.TokenKind.BangEqual => leftDouble != rightDouble,
                    Lexing.TokenKind.Less => leftDouble < rightDouble,
                    Lexing.TokenKind.LessOrEqual => leftDouble <= rightDouble,
                    Lexing.TokenKind.Greater => leftDouble > rightDouble,
                    Lexing.TokenKind.GreaterOrEqual => leftDouble >= rightDouble,
                    _ => null
                };
            }

            if (left is bool leftBoolean && right is bool rightBoolean)
            {
                return binary.OperatorKind switch
                {
                    Lexing.TokenKind.EqualEqual => leftBoolean == rightBoolean,
                    Lexing.TokenKind.BangEqual => leftBoolean != rightBoolean,
                    Lexing.TokenKind.AmpersandAmpersand => leftBoolean && rightBoolean,
                    Lexing.TokenKind.PipePipe => leftBoolean || rightBoolean,
                    _ => null
                };
            }

            if (left is string leftString && right is string rightString)
            {
                return binary.OperatorKind switch
                {
                    Lexing.TokenKind.Plus => leftString + rightString,
                    Lexing.TokenKind.EqualEqual => string.Equals(leftString, rightString, StringComparison.Ordinal),
                    Lexing.TokenKind.BangEqual => !string.Equals(leftString, rightString, StringComparison.Ordinal),
                    _ => null
                };
            }

            return null;
        }

        private static string FormatValue(object? value)
        {
            return value switch
            {
                null => string.Empty,
                bool boolValue => boolValue ? "true" : "false",
                double doubleValue => doubleValue.ToString(CultureInfo.InvariantCulture),
                DateTimeOffset instant => instant.ToString("O", CultureInfo.InvariantCulture),
                object?[] tuple => $"({string.Join(", ", tuple.Select(FormatValue))})",
                List<object?> list => $"[{string.Join(", ", list.Select(FormatValue))}]",
                Dictionary<string, object?> map => $"{{ {string.Join(", ", map.Select(pair => $"{pair.Key}: {FormatValue(pair.Value)}"))} }}",
                RecordValue record => record.ToString(),
                SumValue sum => sum.ToString(),
                _ => value.ToString() ?? string.Empty
            };
        }

        private static string LogTimestamp()
        {
            return DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
        }

        private static HttpRequestValue BuildRequest(HttpClientValue client, string method, string path, string? body)
        {
            var normalizedPath = path.StartsWith("/", StringComparison.Ordinal) ? path : "/" + path;
            var requestUrl = client.BaseUrl.EndsWith("/", StringComparison.Ordinal)
                ? client.BaseUrl.TrimEnd('/') + normalizedPath
                : client.BaseUrl + normalizedPath;

            return new HttpRequestValue(
                method,
                requestUrl,
                new Dictionary<string, string>(client.Headers, StringComparer.OrdinalIgnoreCase),
                body,
                client.TimeoutMs,
                client.RetryMax);
        }

        private static object SendRequest(HttpRequestValue request)
        {
            if (!Uri.TryCreate(request.Url, UriKind.Absolute, out var uri))
            {
                return BuildHttpResponseResult(isSuccess: false, null, BuildHttpInvalidUrlError($"invalid url: {request.Url}"));
            }

            var attempts = Math.Max(1, request.RetryMax + 1);
            object? lastError = null;
            for (var attempt = 0; attempt < attempts; attempt++)
            {
                using var httpClient = new HttpClient
                {
                    Timeout = TimeSpan.FromMilliseconds(request.TimeoutMs)
                };
                using var message = new HttpRequestMessage(new HttpMethod(request.Method), uri);

                request.Headers.TryGetValue("Content-Type", out var contentType);

                foreach (var header in request.Headers)
                {
                    if (string.Equals(header.Key, "Content-Type", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    message.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }

                if (request.Body is not null)
                {
                    message.Content = new StringContent(
                        request.Body,
                        Encoding.UTF8,
                        string.IsNullOrWhiteSpace(contentType) ? "text/plain" : contentType);
                }

                try
                {
                    var response = httpClient.SendAsync(message).GetAwaiter().GetResult();
                    var responseBody = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    var headers = response.Headers
                        .Concat(response.Content.Headers)
                        .ToDictionary(
                            header => header.Key,
                            header => string.Join(",", header.Value),
                            StringComparer.OrdinalIgnoreCase);

                    var responseValue = new HttpResponseValue((int)response.StatusCode, responseBody, headers);
                    return BuildHttpResponseResult(isSuccess: true, responseValue, null);
                }
                catch (TaskCanceledException)
                {
                    lastError = BuildHttpTimeoutError("request timed out");
                }
                catch (Exception ex)
                {
                    lastError = BuildHttpNetworkError($"network error: {ex.Message}");
                }
            }

            return BuildHttpResponseResult(isSuccess: false, null, lastError ?? BuildHttpNetworkError("network error"));
        }

        private static object BuildIntResult(bool isSuccess, int? value, string? error)
        {
            var resultType = TypeSymbol.Result(TypeSymbol.Int, TypeSymbol.String);
            var okVariant = resultType.SumVariants?.FirstOrDefault(variant => string.Equals(variant.Name, "Ok", StringComparison.Ordinal));
            var errorVariant = resultType.SumVariants?.FirstOrDefault(variant => string.Equals(variant.Name, "Error", StringComparison.Ordinal));
            if (isSuccess)
            {
                return new SumValue(okVariant ?? new SumVariantSymbol("Ok", resultType, TypeSymbol.Int), value ?? 0);
            }

            return new SumValue(errorVariant ?? new SumVariantSymbol("Error", resultType, TypeSymbol.String), error ?? "random error");
        }

        private static object BuildStringResult(bool isSuccess, string? value, string? error)
        {
            var resultType = TypeSymbol.Result(TypeSymbol.String, TypeSymbol.String);
            var okVariant = resultType.SumVariants?.FirstOrDefault(variant => string.Equals(variant.Name, "Ok", StringComparison.Ordinal));
            var errorVariant = resultType.SumVariants?.FirstOrDefault(variant => string.Equals(variant.Name, "Error", StringComparison.Ordinal));
            if (isSuccess)
            {
                return new SumValue(okVariant ?? new SumVariantSymbol("Ok", resultType, TypeSymbol.String), value ?? string.Empty);
            }

            return new SumValue(errorVariant ?? new SumVariantSymbol("Error", resultType, TypeSymbol.String), error ?? "route error");
        }

        private static object BuildHttpResponseResult(bool isSuccess, HttpResponseValue? value, object? error)
        {
            var resultType = TypeSymbol.Result(TypeSymbol.HttpResponse, TypeSymbol.HttpError);
            var okVariant = resultType.SumVariants?.FirstOrDefault(variant => string.Equals(variant.Name, "Ok", StringComparison.Ordinal));
            var errorVariant = resultType.SumVariants?.FirstOrDefault(variant => string.Equals(variant.Name, "Error", StringComparison.Ordinal));
            if (isSuccess)
            {
                return new SumValue(okVariant ?? new SumVariantSymbol("Ok", resultType, TypeSymbol.HttpResponse), value ?? new HttpResponseValue(0, string.Empty, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)));
            }

            return new SumValue(errorVariant ?? new SumVariantSymbol("Error", resultType, TypeSymbol.HttpError), error ?? BuildHttpNetworkError("http error"));
        }

        private static object BuildHttpTextResult(bool isSuccess, string? value, object? error)
        {
            var resultType = TypeSymbol.Result(TypeSymbol.String, TypeSymbol.HttpError);
            var okVariant = resultType.SumVariants?.FirstOrDefault(variant => string.Equals(variant.Name, "Ok", StringComparison.Ordinal));
            var errorVariant = resultType.SumVariants?.FirstOrDefault(variant => string.Equals(variant.Name, "Error", StringComparison.Ordinal));
            if (isSuccess)
            {
                return new SumValue(okVariant ?? new SumVariantSymbol("Ok", resultType, TypeSymbol.String), value ?? string.Empty);
            }

            return new SumValue(errorVariant ?? new SumVariantSymbol("Error", resultType, TypeSymbol.HttpError), error ?? BuildHttpNetworkError("http error"));
        }

        private static SumValue BuildHttpInvalidUrlError(string message)
        {
            return BuildHttpErrorVariant("InvalidUrl", TypeSymbol.String, message);
        }

        private static SumValue BuildHttpTimeoutError(string message)
        {
            return BuildHttpErrorVariant("Timeout", TypeSymbol.String, message);
        }

        private static SumValue BuildHttpNetworkError(string message)
        {
            return BuildHttpErrorVariant("NetworkError", TypeSymbol.String, message);
        }

        private static SumValue BuildHttpStatusError(string message)
        {
            return BuildHttpErrorVariant("StatusError", TypeSymbol.String, message);
        }

        private static SumValue BuildHttpErrorVariant(string variantName, TypeSymbol payloadType, object payload)
        {
            var variant = TypeSymbol.HttpError.SumVariants?.FirstOrDefault(item => string.Equals(item.Name, variantName, StringComparison.Ordinal))
                ?? new SumVariantSymbol(variantName, TypeSymbol.HttpError, payloadType);
            return new SumValue(variant, payload);
        }

        private static object BuildFloatResult(bool isSuccess, double? value, string? error)
        {
            var resultType = TypeSymbol.Result(TypeSymbol.Float, TypeSymbol.String);
            var okVariant = resultType.SumVariants?.FirstOrDefault(variant => string.Equals(variant.Name, "Ok", StringComparison.Ordinal));
            var errorVariant = resultType.SumVariants?.FirstOrDefault(variant => string.Equals(variant.Name, "Error", StringComparison.Ordinal));
            if (isSuccess)
            {
                return new SumValue(okVariant ?? new SumVariantSymbol("Ok", resultType, TypeSymbol.Float), value ?? 0.0);
            }

            return new SumValue(errorVariant ?? new SumVariantSymbol("Error", resultType, TypeSymbol.String), error ?? "route error");
        }

        private static object? BuildTimeoutResult(TypeSymbol? returnType, int timeoutMilliseconds, object? fallback)
        {
            if (returnType is null
                || returnType.ResultValueType is null
                || returnType.ResultErrorType != TypeSymbol.String)
            {
                return fallback;
            }

            return new SumValue(
                new SumVariantSymbol("Error", returnType, TypeSymbol.String),
                $"timeout after {timeoutMilliseconds}ms");
        }

        private static string ApplyFormat(object? value, string formatSpecifier)
        {
            if (string.IsNullOrEmpty(formatSpecifier))
            {
                return FormatValue(value);
            }

            if (value is IFormattable formattable)
            {
                try
                {
                    return formattable.ToString(formatSpecifier, CultureInfo.InvariantCulture) ?? string.Empty;
                }
                catch (FormatException)
                {
                }
            }

            if (value is string text)
            {
                if (string.Equals(formatSpecifier, "upper", StringComparison.OrdinalIgnoreCase))
                {
                    return text.ToUpperInvariant();
                }

                if (string.Equals(formatSpecifier, "lower", StringComparison.OrdinalIgnoreCase))
                {
                    return text.ToLowerInvariant();
                }
            }

            return FormatValue(value);
        }

        private sealed class ReturnSignal : Exception
        {
            public object? Value { get; }

            public ReturnSignal(object? value)
            {
                Value = value;
            }
        }

        private sealed class TailCallSignal : Exception
        {
            public object?[] Arguments { get; }

            public TailCallSignal(object?[] arguments)
            {
                Arguments = arguments;
            }
        }

        private sealed class FunctionValue
        {
            public IReadOnlyList<VariableSymbol> Parameters { get; }
            public LoweredBlockStatement Body { get; }
            public IDictionary<VariableSymbol, object?> Captures { get; }

            public FunctionValue(
                IReadOnlyList<VariableSymbol> parameters,
                LoweredBlockStatement body,
                IDictionary<VariableSymbol, object?> captures)
            {
                Parameters = parameters;
                Body = body;
                Captures = captures;
            }
        }

        private sealed class RecordValue
        {
            private readonly Dictionary<string, object?> fields;
            public TypeSymbol Type { get; }

            public RecordValue(TypeSymbol type, Dictionary<string, object?> fields)
            {
                Type = type;
                this.fields = fields;
            }

            public bool TryGet(string name, out object? value) => fields.TryGetValue(name, out value);

            public override string ToString()
            {
                var parts = fields.Select(field => $"{field.Key}: {FormatValue(field.Value)}");
                return $"{Type.Name} {{ {string.Join(", ", parts)} }}";
            }
        }

        private sealed class HttpClientValue
        {
            public string BaseUrl { get; }
            public IReadOnlyDictionary<string, string> Headers { get; }
            public int TimeoutMs { get; }
            public int RetryMax { get; }

            public HttpClientValue(string baseUrl, IReadOnlyDictionary<string, string> headers, int timeoutMs, int retryMax)
            {
                BaseUrl = baseUrl;
                Headers = headers;
                TimeoutMs = timeoutMs;
                RetryMax = retryMax;
            }

            public override string ToString()
            {
                return $"Http {{ baseUrl: {BaseUrl}, headers: {Headers.Count}, timeoutMs: {TimeoutMs}, retryMax: {RetryMax} }}";
            }
        }

        private sealed class HttpRequestValue
        {
            public string Method { get; }
            public string Url { get; }
            public IReadOnlyDictionary<string, string> Headers { get; }
            public string? Body { get; }
            public int TimeoutMs { get; }
            public int RetryMax { get; }

            public HttpRequestValue(
                string method,
                string url,
                IReadOnlyDictionary<string, string> headers,
                string? body,
                int timeoutMs,
                int retryMax)
            {
                Method = method;
                Url = url;
                Headers = headers;
                Body = body;
                TimeoutMs = timeoutMs;
                RetryMax = retryMax;
            }

            public override string ToString()
            {
                return $"Request {{ method: {Method}, url: {Url} }}";
            }
        }

        private sealed class HttpResponseValue
        {
            public int StatusCode { get; }
            public string Body { get; }
            public IReadOnlyDictionary<string, string> Headers { get; }

            public HttpResponseValue(int statusCode, string body, IReadOnlyDictionary<string, string> headers)
            {
                StatusCode = statusCode;
                Body = body;
                Headers = headers;
            }

            public override string ToString()
            {
                return $"Response {{ status: {StatusCode}, body: {Body} }}";
            }
        }

        private sealed class SumValue
        {
            public SumVariantSymbol Variant { get; }
            public object? Payload { get; }

            public SumValue(SumVariantSymbol variant, object? payload)
            {
                Variant = variant;
                Payload = payload;
            }

            public override string ToString()
            {
                if (Variant.PayloadType is null)
                {
                    return Variant.Name;
                }

                return $"{Variant.Name}({FormatValue(Payload)})";
            }
        }

        private sealed class SpawnHandle
        {
            private readonly Task<SpawnOutcome> task;
            private SpawnOutcome? outcome;

            public bool HasMergedOutcome { get; private set; }
            public ScopeFrame? OwnerScope { get; }

            public SpawnHandle(Task<SpawnOutcome> task, ScopeFrame? ownerScope)
            {
                this.task = task;
                OwnerScope = ownerScope;
            }

            public SpawnOutcome GetOrWait()
            {
                outcome ??= task.GetAwaiter().GetResult();
                return outcome;
            }

            public void MarkMerged()
            {
                HasMergedOutcome = true;
            }
        }

        private sealed class ScopeFrame
        {
            public List<SpawnHandle> Handles { get; } = new();
            public List<ChannelState> Channels { get; } = new();
            private readonly CancellationTokenSource cancellation = new();
            public CancellationToken CancellationToken => cancellation.Token;

            public void Cancel()
            {
                if (!cancellation.IsCancellationRequested)
                {
                    cancellation.Cancel();
                }
            }

            public void Dispose()
            {
                cancellation.Dispose();
            }
        }

        private sealed class ChannelState
        {
            public BlockingCollection<object?> Queue { get; }
            private readonly CancellationToken cancellationToken;

            public ChannelState(int capacity, CancellationToken cancellationToken)
            {
                Queue = new BlockingCollection<object?>(new ConcurrentQueue<object?>(), capacity);
                this.cancellationToken = cancellationToken;
            }

            public void Close()
            {
                if (!Queue.IsAddingCompleted)
                {
                    Queue.CompleteAdding();
                }
            }

            public bool TrySend(object? value, out string? error)
            {
                try
                {
                    Queue.Add(value, cancellationToken);
                    error = null;
                    return true;
                }
                catch (OperationCanceledException)
                {
                    error = "cancelled";
                    return false;
                }
                catch (InvalidOperationException)
                {
                    error = "channel closed";
                    return false;
                }
            }

            public ChannelReceiveStatus TryReceive(out object? value)
            {
                try
                {
                    if (Queue.TryTake(out value, Timeout.Infinite, cancellationToken))
                    {
                        return ChannelReceiveStatus.Success;
                    }

                    return ChannelReceiveStatus.Closed;
                }
                catch (OperationCanceledException)
                {
                    value = null;
                    return ChannelReceiveStatus.Cancelled;
                }
            }
        }

        private sealed class ChannelSender
        {
            private readonly ChannelState state;

            public ChannelSender(ChannelState state)
            {
                this.state = state;
            }

            public bool TrySend(object? value, out string? error)
            {
                return state.TrySend(value, out error);
            }
        }

        private sealed class ChannelReceiver
        {
            private readonly ChannelState state;

            public ChannelReceiver(ChannelState state)
            {
                this.state = state;
            }

            public ChannelReceiveStatus TryReceive(out object? value)
            {
                return state.TryReceive(out value);
            }
        }

        private enum ChannelReceiveStatus
        {
            Success,
            Closed,
            Cancelled
        }

        private sealed record SpawnOutcome(object? Value, string Output, List<Diagnostic> Diagnostics);
    }
}
