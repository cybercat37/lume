using Lume.Compiler;

public class CompilerSmokeTests
{
    [Fact]
    public void Empty_source_produces_error()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile("", "test.lume");

        Assert.False(result.Success);
        Assert.NotEmpty(result.Diagnostics);
    }
}
