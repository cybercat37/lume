using Lume.Compiler;

public class CodegenStringEscapeTests
{
    [Fact]
    public void Compile_string_with_escape_sequences_emits_escaped_literal()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile("print \"a\\n\\t\\r\"", "test.lume");

        Assert.True(result.Success);
        Assert.Contains("Console.WriteLine(\"a\\n\\t\\r\");", result.GeneratedCode);
    }
}
