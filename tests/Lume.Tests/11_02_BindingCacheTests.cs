using Lume.Compiler.Binding;
using Lume.Compiler.Parsing;
using Lume.Compiler.Text;

namespace Lume.Tests;

public class BindingCacheTests
{
    [Fact]
    public void Bind_cached_reuses_result_for_same_tree()
    {
        var cache = new BindingCache();
        var source = new SourceText("print 1", "test.lume");
        var syntaxTree = SyntaxTree.Parse(source);
        var binder = new Binder();

        var first = binder.BindCached(syntaxTree, cache);
        var second = binder.BindCached(syntaxTree, cache);

        Assert.Same(first, second);
    }
}
