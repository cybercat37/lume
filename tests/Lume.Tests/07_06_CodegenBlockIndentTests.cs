using Lume.Compiler;

public class CodegenBlockIndentTests
{
    [Fact]
    public void Compile_block_emits_indented_statements()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile("{\nprint 1\n}", "test.lume");

        Assert.True(result.Success);
        Assert.Contains("{", result.GeneratedCode);
        Assert.Contains("    Console.WriteLine(1);", result.GeneratedCode);
        Assert.Contains("}", result.GeneratedCode);
    }
}
