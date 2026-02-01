using Axom.Compiler;

public class CodegenStringEscapeTests
{
    [Fact]
    public void Compile_string_with_escape_sequences_emits_escaped_literal()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile("print \"a\\n\\t\\r\"", "test.axom");

        Assert.True(result.Success);
        Assert.Contains("Console.WriteLine(\"a\\n\\t\\r\");", result.GeneratedCode);
    }

    [Fact]
    public void Compile_string_with_multiple_escape_sequences()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile("print \"line1\\nline2\\tindented\\rcarriage\"", "test.axom");

        Assert.True(result.Success);
        Assert.Contains("\"line1\\nline2\\tindented\\rcarriage\"", result.GeneratedCode);
    }

    [Fact]
    public void Compile_string_with_quotes_and_backslash()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile("print \"say \\\"hello\\\" and \\\\path\"", "test.axom");

        Assert.True(result.Success);
        Assert.Contains("\"say \\\"hello\\\" and \\\\path\"", result.GeneratedCode);
    }

    [Fact]
    public void Compile_empty_string()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile("print \"\"", "test.axom");

        Assert.True(result.Success);
        Assert.Contains("Console.WriteLine(\"\");", result.GeneratedCode);
    }
}
