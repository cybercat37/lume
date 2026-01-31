using Lume.Compiler;

public class CodegenPrintMixTests
{
    [Fact]
    public void Codegen_print_and_println_emit_writeline()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile("print 1\nprintln 2", "test.lume");

        Assert.True(result.Success);
        Assert.Contains("Console.WriteLine(1);", result.GeneratedCode);
        Assert.Contains("Console.WriteLine(2);", result.GeneratedCode);
    }
}
