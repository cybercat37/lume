using Lume.Compiler;

namespace Lume.Tests;

public class CompilerCacheDeterminismTests
{
    [Fact]
    public void Cached_and_uncached_outputs_match()
    {
        var compiler = new CompilerDriver();
        var cache = new CompilerCache();
        var source = "let mut x = 1\nprint x\nx = 2\nprint x";

        var uncached = compiler.Compile(source, "test.lume");
        var cached = compiler.CompileCached(source, "test.lume", cache);

        Assert.True(uncached.Success);
        Assert.True(cached.Success);
        Assert.Equal(uncached.GeneratedCode, cached.GeneratedCode);
    }
}
