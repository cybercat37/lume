using System.Globalization;
using System.Text;
using System.Linq;
using Axom.Compiler.Binding;
using Axom.Compiler.Diagnostics;
using Axom.Compiler.Lowering;
using Axom.Compiler.Parsing;

namespace Axom.Compiler.Interpreting;

public sealed class Interpreter
{
    private readonly Queue<string> inputBuffer = new();

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
        if (bindResult.Diagnostics.Count > 0)
        {
            return new InterpreterResult(string.Empty, bindResult.Diagnostics);
        }

        var lowerer = new Lowerer();
        var loweredProgram = lowerer.Lower(bindResult.Program);
        var evaluator = new Evaluator(loweredProgram, inputBuffer);
        return evaluator.Evaluate();
    }

    private sealed class Evaluator
    {
        private readonly LoweredProgram program;
        private readonly Dictionary<VariableSymbol, object?> values;
        private readonly Dictionary<FunctionSymbol, LoweredFunctionDeclaration> functions;
        private readonly StringBuilder output;
        private readonly List<Diagnostic> diagnostics;
        private readonly Queue<string> inputBuffer;
        private FunctionSymbol? currentFunction;

        public Evaluator(LoweredProgram program, Queue<string> inputBuffer)
        {
            this.program = program;
            values = new Dictionary<VariableSymbol, object?>();
            functions = program.Functions.ToDictionary(func => func.Symbol, func => func);
            output = new StringBuilder();
            diagnostics = new List<Diagnostic>();
            this.inputBuffer = inputBuffer;
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

            return new InterpreterResult(output.ToString().TrimEnd('\n', '\r'), diagnostics);
        }

        private void EvaluateStatement(LoweredStatement statement)
        {
            switch (statement)
            {
                case LoweredBlockStatement block:
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
                        throw new ReturnSignal(null);
                    }

                    var returnValue = EvaluateTailExpression(returnStatement.Expression);
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
                    return inputBuffer.Count > 0 ? inputBuffer.Dequeue() : string.Empty;
                case LoweredCallExpression call:
                    return EvaluateCall(call);
                case LoweredFunctionExpression functionExpression:
                    return functionExpression.Function;
                case LoweredLambdaExpression lambda:
                    return EvaluateLambda(lambda);
                case LoweredTupleExpression tuple:
                    return EvaluateTupleExpression(tuple);
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
                case "len":
                    return arguments[0] is string text ? text.Length : 0;
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

                        return null;
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
                RecordValue record => record.ToString(),
                SumValue sum => sum.ToString(),
                _ => value.ToString() ?? string.Empty
            };
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
    }
}
