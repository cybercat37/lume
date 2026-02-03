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
        private readonly Dictionary<FunctionSymbol, BoundFunctionDeclaration> functions;
        private readonly StringBuilder output;
        private readonly List<Diagnostic> diagnostics;
        private readonly Queue<string> inputBuffer;

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

        private void EvaluateStatement(BoundStatement statement)
        {
            switch (statement)
            {
                case BoundBlockStatement block:
                    foreach (var inner in block.Statements)
                    {
                        EvaluateStatement(inner);
                    }
                    return;
                case BoundVariableDeclaration declaration:
                    values[declaration.Symbol] = EvaluateExpression(declaration.Initializer);
                    return;
                case BoundPrintStatement print:
                    var value = EvaluateExpression(print.Expression);
                    if (diagnostics.Count == 0)
                    {
                        output.AppendLine(FormatValue(value));
                    }
                    return;
                case BoundExpressionStatement expressionStatement:
                    EvaluateExpression(expressionStatement.Expression);
                    return;
                case BoundReturnStatement returnStatement:
                    var returnValue = returnStatement.Expression is null
                        ? null
                        : EvaluateExpression(returnStatement.Expression);
                    throw new ReturnSignal(returnValue);
                default:
                    throw new InvalidOperationException($"Unexpected statement: {statement.GetType().Name}");
            }
        }

        private object? EvaluateExpression(BoundExpression expression)
        {
            switch (expression)
            {
                case BoundLiteralExpression literal:
                    return literal.Value;
                case BoundNameExpression name:
                    if (values.TryGetValue(name.Symbol, out var value))
                    {
                        return value;
                    }

                    return null;
                case BoundAssignmentExpression assignment:
                    var assignedValue = EvaluateExpression(assignment.Expression);
                    values[assignment.Symbol] = assignedValue;
                    return assignedValue;
                case BoundUnaryExpression unary:
                    return EvaluateUnaryExpression(unary);
                case BoundBinaryExpression binary:
                    return EvaluateBinaryExpression(binary);
                case BoundInputExpression:
                    return inputBuffer.Count > 0 ? inputBuffer.Dequeue() : string.Empty;
                case BoundCallExpression call:
                    return EvaluateCall(call);
                case BoundFunctionExpression functionExpression:
                    return functionExpression.Function;
                case BoundLambdaExpression lambda:
                    return EvaluateLambda(lambda);
                case BoundMatchExpression match:
                    return EvaluateMatchExpression(match);
                case BoundTupleExpression tuple:
                    return EvaluateTupleExpression(tuple);
                case BoundRecordLiteralExpression record:
                    return EvaluateRecordLiteralExpression(record);
                case BoundFieldAccessExpression fieldAccess:
                    return EvaluateFieldAccessExpression(fieldAccess);
                case BoundSumConstructorExpression sum:
                    return EvaluateSumConstructorExpression(sum);
                default:
                    throw new InvalidOperationException($"Unexpected expression: {expression.GetType().Name}");
            }
        }

        private object? EvaluateTupleExpression(BoundTupleExpression tuple)
        {
            return tuple.Elements.Select(EvaluateExpression).ToArray();
        }

        private object? EvaluateMatchExpression(BoundMatchExpression match)
        {
            var value = EvaluateExpression(match.Expression);
            foreach (var arm in match.Arms)
            {
                var bindings = new Dictionary<VariableSymbol, object?>();
                if (TryMatchPattern(arm.Pattern, value, bindings))
                {
                    var previousValues = new Dictionary<VariableSymbol, object?>();
                    foreach (var binding in bindings)
                    {
                        if (values.TryGetValue(binding.Key, out var existing))
                        {
                            previousValues[binding.Key] = existing;
                        }

                        values[binding.Key] = binding.Value;
                    }

                    try
                    {
                        return EvaluateExpression(arm.Expression);
                    }
                    finally
                    {
                        foreach (var binding in bindings)
                        {
                            if (previousValues.TryGetValue(binding.Key, out var previous))
                            {
                                values[binding.Key] = previous;
                            }
                            else
                            {
                                values.Remove(binding.Key);
                            }
                        }
                    }
                }
            }

            diagnostics.Add(Diagnostic.Error(string.Empty, 1, 1, "Non-exhaustive match expression."));
            return null;
        }

        private object? EvaluateRecordLiteralExpression(BoundRecordLiteralExpression record)
        {
            var valuesByName = new Dictionary<string, object?>(StringComparer.Ordinal);
            foreach (var field in record.Fields)
            {
                valuesByName[field.Field.Name] = EvaluateExpression(field.Expression);
            }

            return new RecordValue(record.RecordType, valuesByName);
        }

        private object? EvaluateFieldAccessExpression(BoundFieldAccessExpression fieldAccess)
        {
            var target = EvaluateExpression(fieldAccess.Target);
            if (target is RecordValue record && record.TryGet(fieldAccess.Field.Name, out var value))
            {
                return value;
            }

            diagnostics.Add(Diagnostic.Error(string.Empty, 1, 1, "Field access failed."));
            return null;
        }

        private object? EvaluateSumConstructorExpression(BoundSumConstructorExpression sum)
        {
            var payload = sum.Payload is null ? null : EvaluateExpression(sum.Payload);
            return new SumValue(sum.Variant, payload);
        }

        private static bool TryMatchPattern(
            BoundPattern pattern,
            object? value,
            IDictionary<VariableSymbol, object?> bindings)
        {
            switch (pattern)
            {
                case BoundWildcardPattern:
                    return true;
                case BoundLiteralPattern literal:
                    return Equals(literal.Value, value);
                case BoundIdentifierPattern identifier:
                    bindings[identifier.Symbol] = value;
                    return true;
                case BoundTuplePattern tuple:
                    if (value is not object?[] elements)
                    {
                        return false;
                    }

                    if (elements.Length != tuple.Elements.Count)
                    {
                        return false;
                    }

                    for (var i = 0; i < elements.Length; i++)
                    {
                        if (!TryMatchPattern(tuple.Elements[i], elements[i], bindings))
                        {
                            return false;
                        }
                    }

                    return true;
                case BoundVariantPattern variant:
                    if (value is not SumValue sum)
                    {
                        return false;
                    }

                    if (!string.Equals(sum.Variant.Name, variant.Variant.Name, StringComparison.Ordinal))
                    {
                        return false;
                    }

                    if (variant.Payload is null)
                    {
                        return true;
                    }

                    return TryMatchPattern(variant.Payload, sum.Payload, bindings);
                default:
                    return false;
            }
        }

        private object? EvaluateCall(BoundCallExpression call)
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
                    return EvaluateUserFunction(declaration.Parameters, declaration.Body, arguments, new Dictionary<VariableSymbol, object?>());
                }

                return null;
            }

            if (callee is FunctionValue functionValue)
            {
                return EvaluateUserFunction(functionValue.Parameters, functionValue.Body, arguments, functionValue.Captures);
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

        private FunctionValue EvaluateLambda(BoundLambdaExpression lambda)
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
            IReadOnlyList<VariableSymbol> parameters,
            BoundBlockStatement body,
            object?[] arguments,
            IDictionary<VariableSymbol, object?> captures)
        {
            var previousValues = new Dictionary<VariableSymbol, object?>();
            var modifiedSymbols = new HashSet<VariableSymbol>();

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
                foreach (var statement in body.Statements)
                {
                    EvaluateStatement(statement);
                }
            }
            catch (ReturnSignal signal)
            {
                result = signal.Value;
            }
            finally
            {
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

        private object? EvaluateUnaryExpression(BoundUnaryExpression unary)
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

        private object? EvaluateBinaryExpression(BoundBinaryExpression binary)
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

        private sealed class FunctionValue
        {
            public IReadOnlyList<VariableSymbol> Parameters { get; }
            public BoundBlockStatement Body { get; }
            public IDictionary<VariableSymbol, object?> Captures { get; }

            public FunctionValue(
                IReadOnlyList<VariableSymbol> parameters,
                BoundBlockStatement body,
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
