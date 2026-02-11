using Axom.Compiler;

public class CodegenCombinatorTests
{
    [Fact]
    public void Compile_map_emits_select_to_list()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile("print map([1, 2, 3], fn(x: Int) => x * 2)", "test.axom");

        Assert.True(result.Success);
        Assert.Contains("System.Linq.Enumerable.Select", result.GeneratedCode);
        Assert.Contains("System.Linq.Enumerable.ToList", result.GeneratedCode);
    }

    [Fact]
    public void Compile_fold_emits_aggregate()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile("print fold([1, 2, 3], 0, fn(acc: Int, x: Int) => acc + x)", "test.axom");

        Assert.True(result.Success);
        Assert.Contains("System.Linq.Enumerable.Aggregate", result.GeneratedCode);
    }
}
