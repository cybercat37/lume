using Axom.Compiler;

public class CodegenClearBuiltinTests
{
    [Fact]
    public void Compile_clear_emits_console_clear_call()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile("clear()", "test.axom");

        Assert.True(result.Success);
        Assert.Contains("Console.Clear()", result.GeneratedCode);
    }
}
