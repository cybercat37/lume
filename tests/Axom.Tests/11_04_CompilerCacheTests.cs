using Axom.Compiler;

namespace Axom.Tests;

public class CompilerCacheTests
{
    [Fact]
    public void Compile_cached_reuses_outputs_for_identical_source()
    {
        var cache = new CompilerCache();
        var compiler = new CompilerDriver();

        var first = compiler.CompileCached("print 1", "test.axom", cache);
        var second = compiler.CompileCached("print 1", "test.axom", cache);

        Assert.True(first.Success);
        Assert.True(second.Success);
        Assert.Same(first.SyntaxTree, second.SyntaxTree);
        Assert.Same(first.GeneratedCode, second.GeneratedCode);
    }
}
