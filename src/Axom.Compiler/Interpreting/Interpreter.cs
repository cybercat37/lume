using System.Text;
using System.Linq;
using Axom.Compiler.Binding;
using Axom.Compiler.Diagnostics;
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

        var evaluator = new Evaluator(bindResult.Program, inputBuffer);
        return evaluator.Evaluate();
    }

    private sealed class Evaluator
    {
        private readonly BoundProgram program;
        private readonly Dictionary<VariableSymbol, object?> values;
        private readonly Dictionary<FunctionSymbol, BoundFunctionDeclaration> functions;
        private readonly StringBuilder output;
        private readonly List<Diagnostic> diagnostics;
        private readonly Queue<string> inputBuffer;

        public Evaluator(BoundProgram program, Queue<string> inputBuffer)
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
                    return arguments[0] is int value ? Math.Abs(value) : 0;
                case "min":
                    return arguments[0] is int left && arguments[1] is int right ? Math.Min(left, right) : 0;
                case "max":
                    return arguments[0] is int leftMax && arguments[1] is int rightMax ? Math.Max(leftMax, rightMax) : 0;
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

            return null;
        }

        private object? EvaluateBinaryExpression(BoundBinaryExpression binary)
        {
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
    }
}
