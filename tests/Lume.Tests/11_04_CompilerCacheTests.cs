using Lume.Compiler;

namespace Lume.Tests;

public class CompilerCacheTests
{
    [Fact]
    public void Compile_cached_reuses_outputs_for_identical_source()
    {
        var cache = new CompilerCache();
        var compiler = new CompilerDriver();

        var first = compiler.CompileCached("print 1", "test.lume", cache);
        var second = compiler.CompileCached("print 1", "test.lume", cache);

        Assert.True(first.Success);
        Assert.True(second.Success);
        Assert.Same(first.SyntaxTree, second.SyntaxTree);
        Assert.Same(first.GeneratedCode, second.GeneratedCode);
    }
}
