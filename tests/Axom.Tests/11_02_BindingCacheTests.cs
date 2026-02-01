using Axom.Compiler.Binding;
using Axom.Compiler.Parsing;
using Axom.Compiler.Text;

namespace Axom.Tests;

public class BindingCacheTests
{
    [Fact]
    public void Bind_cached_reuses_result_for_same_tree()
    {
        var cache = new BindingCache();
        var source = new SourceText("print 1", "test.axom");
        var syntaxTree = SyntaxTree.Parse(source);
        var binder = new Binder();

        var first = binder.BindCached(syntaxTree, cache);
        var second = binder.BindCached(syntaxTree, cache);

        Assert.Same(first, second);
    }
}
