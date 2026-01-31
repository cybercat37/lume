using Lume.Compiler;

public class CompilerPipelineTests
{
    [Fact]
    public void Compile_print_string_generates_console_write()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile("print \"hello\"", "test.lume");

        Assert.True(result.Success);
        Assert.Contains("Console.WriteLine(\"hello\");", result.GeneratedCode);
    }

    [Fact]
    public void Compile_unterminated_string_fails()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile("print \"oops", "test.lume");

        Assert.False(result.Success);
        Assert.NotEmpty(result.Diagnostics);
    }
}
