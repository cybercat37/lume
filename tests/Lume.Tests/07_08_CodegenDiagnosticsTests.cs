using Lume.Compiler;

public class CodegenDiagnosticsTests
{
    [Fact]
    public void Compile_with_diagnostics_returns_no_code()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile("print x", "test.lume");

        Assert.False(result.Success);
        Assert.NotEmpty(result.Diagnostics);
        Assert.Equal(string.Empty, result.GeneratedCode);
    }
}
