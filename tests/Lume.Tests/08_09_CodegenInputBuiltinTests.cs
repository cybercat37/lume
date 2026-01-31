using Lume.Compiler;

public class CodegenInputBuiltinTests
{
    [Fact]
    public void Codegen_input_emits_console_readline()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile("print input", "test.lume");

        Assert.True(result.Success);
        Assert.Contains("Console.ReadLine()", result.GeneratedCode);
    }
}
