using Axom.Compiler;

public class CodegenListPatternTests
{
    [Fact]
    public void Compile_advanced_list_rest_pattern_emits_count_and_slice_helpers()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile(
            "print match [1, 2, 3, 4] {\n  [first, ...middle, last] -> first + fold(middle, 0, fn(acc: Int, x: Int) => acc + x) + last\n  _ -> 0\n}",
            "test.axom");

        Assert.True(result.Success);
        Assert.Contains("System.Linq.Enumerable.Count", result.GeneratedCode);
        Assert.Contains("System.Linq.Enumerable.Skip", result.GeneratedCode);
        Assert.Contains("System.Linq.Enumerable.Take", result.GeneratedCode);
    }
}
