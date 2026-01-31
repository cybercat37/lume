using System.Text;
using Lume.Compiler.Binding;
using Lume.Compiler.Diagnostics;
using Lume.Compiler.Parsing;

namespace Lume.Compiler.Interpreting;

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
        private readonly StringBuilder output;
        private readonly List<Diagnostic> diagnostics;
        private readonly Queue<string> inputBuffer;

        public Evaluator(BoundProgram program, Queue<string> inputBuffer)
        {
            this.program = program;
            values = new Dictionary<VariableSymbol, object?>();
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
                default:
                    throw new InvalidOperationException($"Unexpected expression: {expression.GetType().Name}");
            }
        }

        private object? EvaluateCall(BoundCallExpression call)
        {
            var arguments = call.Arguments.Select(EvaluateExpression).ToArray();
            switch (call.Function.Name)
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
    }
}
