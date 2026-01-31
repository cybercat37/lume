using System.Text;
using Lume.Compiler.Syntax;

namespace Lume.Compiler.Emitting;

public sealed class Emitter
{
    public string Emit(CompilationUnitSyntax root)
    {
        var statement = root.Statement as PrintStatementSyntax;
        var literalExpression = statement?.Expression as LiteralExpressionSyntax;
        var literalValue = literalExpression?.LiteralToken.Value as string ?? string.Empty;
        var escaped = EscapeString(literalValue);

        var builder = new StringBuilder();
        builder.AppendLine("using System;");
        builder.AppendLine();
        builder.AppendLine("class Program");
        builder.AppendLine("{");
        builder.AppendLine("    static void Main()");
        builder.AppendLine("    {");
        builder.AppendLine($"        Console.WriteLine(\"{escaped}\");");
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
}
