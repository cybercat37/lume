using Axom.Compiler;

public class CodegenBuiltinTests
{
    [Fact]
    public void Codegen_println_emits_console_writeline()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile("println 1", "test.axom");

        Assert.True(result.Success);
        Assert.Contains("Console.WriteLine(1);", result.GeneratedCode);
    }
}
