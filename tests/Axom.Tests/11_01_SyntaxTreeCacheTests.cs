using Axom.Compiler.Parsing;
using Axom.Compiler.Text;

namespace Axom.Tests;

public class SyntaxTreeCacheTests
{
    [Fact]
    public void Parse_cached_reuses_tree_for_same_text()
    {
        var cache = new SyntaxTreeCache();
        var sourceA = new SourceText("print 1", "a.axom");
        var sourceB = new SourceText("print 1", "b.axom");

        var first = SyntaxTree.ParseCached(sourceA, cache);
        var second = SyntaxTree.ParseCached(sourceB, cache);

        Assert.Same(first, second);
    }

    [Fact]
    public void Parse_cached_returns_new_tree_for_different_text()
    {
        var cache = new SyntaxTreeCache();
        var sourceA = new SourceText("print 1", "a.axom");
        var sourceB = new SourceText("print 2", "b.axom");

        var first = SyntaxTree.ParseCached(sourceA, cache);
        var second = SyntaxTree.ParseCached(sourceB, cache);

        Assert.NotSame(first, second);
    }
}
