using Lume.Compiler;

public class CodegenNameExpressionTests
{
    [Fact]
    public void Compile_print_name_emits_identifier()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile("let x = 1\nprint x", "test.lume");

        Assert.True(result.Success);
        Assert.Contains("Console.WriteLine(x);", result.GeneratedCode);
    }
}
