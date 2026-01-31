using System.Text;
using Lume.Compiler.Binding;
using Lume.Compiler.Lexing;

namespace Lume.Compiler.Emitting;

public sealed class Emitter
{
    public string Emit(BoundProgram program)
    {
        var builder = new StringBuilder();
        builder.AppendLine("using System;");
        builder.AppendLine();
        builder.AppendLine("class Program");
        builder.AppendLine("{");
        builder.AppendLine("    static void Main()");
        builder.AppendLine("    {");
        var writer = new IndentedWriter(builder, 2);
        foreach (var statement in program.Statements)
        {
            WriteStatement(writer, statement);
        }
        builder.AppendLine("    }");
        builder.AppendLine("}");

        return builder.ToString();
    }

    private static void WriteStatement(IndentedWriter writer, BoundStatement statement)
    {
        switch (statement)
        {
            case BoundBlockStatement block:
                writer.WriteLine("{");
                writer.Indent();
                foreach (var inner in block.Statements)
                {
                    WriteStatement(writer, inner);
                }
                writer.Unindent();
                writer.WriteLine("}");
                return;
            case BoundVariableDeclaration declaration:
                writer.WriteLine($"var {declaration.Symbol.Name} = {WriteExpression(declaration.Initializer)};");
                return;
            case BoundPrintStatement print:
                writer.WriteLine($"Console.WriteLine({WriteExpression(print.Expression)});");
                return;
            case BoundExpressionStatement expressionStatement:
                writer.WriteLine($"{WriteExpression(expressionStatement.Expression)};");
                return;
            default:
                throw new InvalidOperationException($"Unexpected statement: {statement.GetType().Name}");
        }
    }

    private static string WriteExpression(BoundExpression expression)
    {
        return expression switch
        {
            BoundLiteralExpression literal => FormatLiteral(literal.Value),
            BoundNameExpression name => name.Symbol.Name,
            BoundAssignmentExpression assignment =>
                $"{assignment.Symbol.Name} = {WriteExpression(assignment.Expression)}",
            BoundUnaryExpression unary =>
                $"{OperatorText(unary.OperatorKind)}{WriteExpression(unary.Operand)}",
            BoundBinaryExpression binary =>
                $"{WriteExpression(binary.Left)} {OperatorText(binary.OperatorKind)} {WriteExpression(binary.Right)}",
            _ => throw new InvalidOperationException($"Unexpected expression: {expression.GetType().Name}")
        };
    }

    private static string OperatorText(TokenKind kind)
    {
        return kind switch
        {
            TokenKind.Plus => "+",
            TokenKind.Minus => "-",
            TokenKind.Star => "*",
            TokenKind.Slash => "/",
            _ => throw new InvalidOperationException($"Unexpected operator: {kind}")
        };
    }

    private static string FormatLiteral(object? value)
    {
        return value switch
        {
            null => "null",
            bool boolValue => boolValue ? "true" : "false",
            string stringValue => $"\"{EscapeString(stringValue)}\"",
            _ => value.ToString() ?? "null"
        };
    }

    private static string EscapeString(string value)
    {
        return value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"");
    }

    private sealed class IndentedWriter
    {
        private readonly StringBuilder builder;
        private int indentLevel;

        public IndentedWriter(StringBuilder builder, int indentLevel)
        {
            this.builder = builder;
            this.indentLevel = indentLevel;
        }

        public void Indent()
        {
            indentLevel++;
        }

        public void Unindent()
        {
            indentLevel--;
        }

        public void WriteLine(string text)
        {
            builder.Append(' ', indentLevel * 4);
            builder.AppendLine(text);
        }
    }
}
