using Axom.Compiler;

public class CompilerSmokeTests
{
    [Fact]
    public void Empty_source_produces_error()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile("", "test.axom");

        Assert.False(result.Success);
        Assert.NotEmpty(result.Diagnostics);
    }
}
