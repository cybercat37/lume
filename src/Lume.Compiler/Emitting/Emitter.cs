using System.Text;
using System.Linq;
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
                writer.WriteLine($"var {EscapeIdentifier(declaration.Symbol.Name)} = {WriteExpression(declaration.Initializer)};");
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

    private static string WriteExpression(BoundExpression expression, int parentPrecedence = 0)
    {
        return expression switch
        {
            BoundLiteralExpression literal => FormatLiteral(literal.Value),
            BoundNameExpression name => EscapeIdentifier(name.Symbol.Name),
            BoundAssignmentExpression assignment => WrapIfNeeded(
                $"{EscapeIdentifier(assignment.Symbol.Name)} = {WriteExpression(assignment.Expression, GetAssignmentPrecedence())}",
                GetAssignmentPrecedence(),
                parentPrecedence),
            BoundUnaryExpression unary => WrapIfNeeded(
                $"{OperatorText(unary.OperatorKind)}{WriteExpression(unary.Operand, GetUnaryPrecedence())}",
                GetUnaryPrecedence(),
                parentPrecedence),
            BoundBinaryExpression binary => WriteBinaryExpression(binary, parentPrecedence),
            BoundInputExpression => "Console.ReadLine()",
            BoundCallExpression call => WriteCallExpression(call),
            _ => throw new InvalidOperationException($"Unexpected expression: {expression.GetType().Name}")
        };
    }

    private static string WriteCallExpression(BoundCallExpression call)
    {
        var args = string.Join(", ", call.Arguments.Select(arg => WriteExpression(arg)));
        return call.Function.Name switch
        {
            "println" => $"Console.WriteLine({args})",
            "print" => $"Console.WriteLine({args})",
            "input" => "Console.ReadLine()",
            "len" => $"{args}.Length",
            "abs" => $"Math.Abs({args})",
            "min" => $"Math.Min({args})",
            "max" => $"Math.Max({args})",
            _ => $"{EscapeIdentifier(call.Function.Name)}({args})"
        };
    }

    private static string WriteBinaryExpression(BoundBinaryExpression binary, int parentPrecedence)
    {
        var precedence = GetBinaryPrecedence(binary.OperatorKind);
        var left = WriteExpression(binary.Left, precedence);
        var right = WriteExpression(binary.Right, precedence);
        var text = $"{left} {OperatorText(binary.OperatorKind)} {right}";
        return WrapIfNeeded(text, precedence, parentPrecedence);
    }

    private static string WrapIfNeeded(string text, int precedence, int parentPrecedence)
    {
        return precedence < parentPrecedence ? $"({text})" : text;
    }

    private static int GetAssignmentPrecedence() => 1;

    private static int GetUnaryPrecedence() => 4;

    private static int GetBinaryPrecedence(TokenKind kind)
    {
        return kind switch
        {
            TokenKind.Star => 3,
            TokenKind.Slash => 3,
            TokenKind.Plus => 2,
            TokenKind.Minus => 2,
            _ => 0
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

    private static string EscapeIdentifier(string name)
    {
        return CSharpKeywords.Contains(name) ? $"@{name}" : name;
    }

    private static readonly HashSet<string> CSharpKeywords = new(StringComparer.Ordinal)
    {
        "abstract",
        "as",
        "base",
        "bool",
        "break",
        "byte",
        "case",
        "catch",
        "char",
        "checked",
        "class",
        "const",
        "continue",
        "decimal",
        "default",
        "delegate",
        "do",
        "double",
        "else",
        "enum",
        "event",
        "explicit",
        "extern",
        "false",
        "finally",
        "fixed",
        "float",
        "for",
        "foreach",
        "goto",
        "if",
        "implicit",
        "in",
        "int",
        "interface",
        "internal",
        "is",
        "lock",
        "long",
        "namespace",
        "new",
        "null",
        "object",
        "operator",
        "out",
        "override",
        "params",
        "private",
        "protected",
        "public",
        "readonly",
        "ref",
        "return",
        "sbyte",
        "sealed",
        "short",
        "sizeof",
        "stackalloc",
        "static",
        "string",
        "struct",
        "switch",
        "this",
        "throw",
        "true",
        "try",
        "typeof",
        "uint",
        "ulong",
        "unchecked",
        "unsafe",
        "ushort",
        "using",
        "virtual",
        "void",
        "volatile",
        "while",
        "var"
    };

    private static string EscapeString(string value)
    {
        return value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\t", "\\t")
            .Replace("\r", "\\r");
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
