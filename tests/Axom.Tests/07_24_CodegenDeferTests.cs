using Axom.Compiler;

public class CodegenDeferTests
{
    [Fact]
    public void Compile_defer_with_return_emits_cleanup_before_return()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile(
            "fn work() -> Int {\n  defer {\n    print 1\n  }\n  return 2\n}\nprint work()",
            "test.axom");

        Assert.True(result.Success);

        var cleanupIndex = result.GeneratedCode.IndexOf("Console.WriteLine(1);", StringComparison.Ordinal);
        var returnIndex = result.GeneratedCode.IndexOf("return __axomReturn;", StringComparison.Ordinal);

        Assert.True(cleanupIndex >= 0);
        Assert.True(returnIndex > cleanupIndex);
    }
}
