using Axom.Compiler.Interpreting;
using Axom.Compiler.Parsing;
using Axom.Compiler.Text;

public class InterpreterTimeStdlibTests
{
    [Fact]
    public void Time_now_add_diff_iso_roundtrip_works()
    {
        var sourceText = new SourceText(
            "let now = time_now_utc()\nlet later = time_add_ms(now, 250)\nprint time_diff_ms(later, now)\nlet iso = time_to_iso(now)\nprint result_map(time_from_iso(iso), fn(value: Instant) => time_to_iso(value))",
            "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        var lines = result.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        Assert.Equal("250", lines[0]);
        Assert.StartsWith("Ok(", lines[1]);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Time_from_iso_returns_error_for_invalid_input()
    {
        var sourceText = new SourceText("print time_from_iso(\"not-a-date\")", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        Assert.Equal("Error(invalid ISO-8601 instant)", result.Output.Trim());
        Assert.Empty(result.Diagnostics);
    }
}
