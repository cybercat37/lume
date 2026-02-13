using Axom.Compiler;

public class CodegenTakeSkipZipTests
{
    [Fact]
    public void Compile_take_emits_linq_take()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile("print take([1, 2, 3], 2)", "test.axom");

        Assert.True(result.Success);
        Assert.Contains("System.Linq.Enumerable.Take", result.GeneratedCode);
    }

    [Fact]
    public void Compile_skip_emits_linq_skip()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile("print skip([1, 2, 3], 1)", "test.axom");

        Assert.True(result.Success);
        Assert.Contains("System.Linq.Enumerable.Skip", result.GeneratedCode);
    }

    [Fact]
    public void Compile_take_while_emits_linq_take_while()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile("print take_while([1, 2, 3], fn(x: Int) => x < 3)", "test.axom");

        Assert.True(result.Success);
        Assert.Contains("System.Linq.Enumerable.TakeWhile", result.GeneratedCode);
    }

    [Fact]
    public void Compile_skip_while_emits_linq_skip_while()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile("print skip_while([1, 2, 3], fn(x: Int) => x < 3)", "test.axom");

        Assert.True(result.Success);
        Assert.Contains("System.Linq.Enumerable.SkipWhile", result.GeneratedCode);
    }

    [Fact]
    public void Compile_enumerate_emits_indexed_select()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile("print enumerate([10, 20])", "test.axom");

        Assert.True(result.Success);
        Assert.Contains("System.Linq.Enumerable.Select", result.GeneratedCode);
        Assert.Contains("(item, index) => (index, item)", result.GeneratedCode);
    }

    [Fact]
    public void Compile_zip_emits_linq_zip()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile("print zip([1, 2], [3, 4])", "test.axom");

        Assert.True(result.Success);
        Assert.Contains("System.Linq.Enumerable.Zip", result.GeneratedCode);
    }

    [Fact]
    public void Compile_zip_with_emits_linq_zip_selector_overload()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile("print zip_with([1, 2], [3, 4], fn(x: Int, y: Int) => x + y)", "test.axom");

        Assert.True(result.Success);
        Assert.Contains("System.Linq.Enumerable.Zip", result.GeneratedCode);
        Assert.Contains("(int x, int y) => x + y", result.GeneratedCode);
    }
}
