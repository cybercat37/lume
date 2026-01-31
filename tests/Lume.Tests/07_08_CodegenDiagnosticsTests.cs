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

    [Fact]
    public void Compile_csharp_keyword_as_variable_escapes_identifier()
    {
        // "class" è una keyword C# - il codice generato deve usare @class
        // altrimenti non compilerebbe: "var class = 1;" è sintassi C# invalida
        var compiler = new CompilerDriver();
        var result = compiler.Compile("let class = 1\nprint class", "test.lume");

        Assert.True(result.Success);
        // Il codice generato NON deve contenere "var class =" (invalido in C#)
        // Deve usare "@class" per fare escape della keyword
        Assert.DoesNotContain("var class =", result.GeneratedCode);
    }
}
