using Axom.Compiler;

public class CodegenStdlibCollectionResultTests
{
    [Fact]
    public void Compile_count_any_all_emit_linq_helpers()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile("print count([1, 2, 3])\nprint any([1, 2, 3], fn(x: Int) => x > 1)\nprint all([1, 2, 3], fn(x: Int) => x > 0)", "test.axom");

        Assert.True(result.Success);
        Assert.Contains("System.Linq.Enumerable.Count", result.GeneratedCode);
        Assert.Contains("System.Linq.Enumerable.Any", result.GeneratedCode);
        Assert.Contains("System.Linq.Enumerable.All", result.GeneratedCode);
    }

    [Fact]
    public void Compile_sum_emits_linq_sum()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile("print sum([1.0, 2.5])", "test.axom");

        Assert.True(result.Success);
        Assert.Contains("System.Linq.Enumerable.Sum", result.GeneratedCode);
    }

    [Fact]
    public void Compile_result_map_emits_axom_result_mapping()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile("print result_map(rand_int(10), fn(x: Int) => x + 1)", "test.axom");

        Assert.True(result.Success);
        Assert.Contains(".Tag == \"Ok\"", result.GeneratedCode);
        Assert.Contains("AxomResult<int>.Ok", result.GeneratedCode);
    }
}
