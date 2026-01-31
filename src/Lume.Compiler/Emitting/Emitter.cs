using System.Text;
using Lume.Compiler.Syntax;

namespace Lume.Compiler.Emitting;

public sealed class Emitter
{
    public string Emit(CompilationUnitSyntax root)
    {
        var statement = root.Statement as PrintStatementSyntax;
        var expression = statement?.Expression;
        var expressionText = RenderLiteralExpression(expression);

        var builder = new StringBuilder();
        builder.AppendLine("using System;");
        builder.AppendLine();
        builder.AppendLine("class Program");
        builder.AppendLine("{");
        builder.AppendLine("    static void Main()");
        builder.AppendLine("    {");
        builder.AppendLine($"        Console.WriteLine({expressionText});");
        builder.AppendLine("    }");
        builder.AppendLine("}");

        return builder.ToString();
    }

    private static string EscapeString(string value)
    {
        return value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"");
    }

    private static string RenderLiteralExpression(ExpressionSyntax? expression)
    {
        if (expression is LiteralExpressionSyntax literal)
        {
            if (literal.LiteralToken.Value is string stringValue)
            {
                return $"\"{EscapeString(stringValue)}\"";
            }

            if (literal.LiteralToken.Value is bool boolValue)
            {
                return boolValue ? "true" : "false";
            }

            if (literal.LiteralToken.Value is int intValue)
            {
                return intValue.ToString();
            }
        }

        return "\"\"";
    }
}
