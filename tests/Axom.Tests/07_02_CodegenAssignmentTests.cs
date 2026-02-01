using Axom.Compiler;

public class CodegenAssignmentTests
{
    [Fact]
    public void Compile_assignment_emits_statement()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile("let mut x = 1\nx = 2", "test.axom");

        Assert.True(result.Success);
        Assert.Contains("var x = 1;", result.GeneratedCode);
        Assert.Contains("x = 2;", result.GeneratedCode);
    }
}
