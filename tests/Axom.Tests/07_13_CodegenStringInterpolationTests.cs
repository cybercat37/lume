using Axom.Compiler;

public class CodegenStringInterpolationTests
{
    [Fact]
    public void Compile_interpolated_string_emits_stringify_helper_call()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile("let n = 7\nprint f\"n={n}\"", "test.axom");

        Assert.True(result.Success);
        Assert.Contains("AxomStringify", result.GeneratedCode);
    }

    [Fact]
    public void Compile_interpolated_string_with_format_emits_format_helper_call()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile("let n = 7\nprint f\"{n:000}\"", "test.axom");

        Assert.True(result.Success);
        Assert.Contains("AxomFormat", result.GeneratedCode);
    }
}
