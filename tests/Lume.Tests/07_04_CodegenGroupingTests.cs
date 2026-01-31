using Lume.Compiler;

public class CodegenGroupingTests
{
    [Fact]
    public void Compile_nested_grouping_preserves_parentheses()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile("print (1 + 2) * (3 + 4)", "test.lume");

        Assert.True(result.Success);
        Assert.Contains("(1 + 2) * (3 + 4)", result.GeneratedCode);
    }
}
