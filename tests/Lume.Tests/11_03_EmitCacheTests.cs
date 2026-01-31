using Lume.Compiler;
using Lume.Compiler.Binding;
using Lume.Compiler.Emitting;

namespace Lume.Tests;

public class EmitCacheTests
{
    [Fact]
    public void Emit_cached_reuses_code_for_same_program()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile("print 1", "test.lume");

        Assert.True(result.Success);

        var binder = new Binder();
        var bindResult = binder.Bind(result.SyntaxTree);
        var cache = new EmitCache();
        var emitter = new Emitter();

        var first = emitter.EmitCached(bindResult.Program, cache);
        var second = emitter.EmitCached(bindResult.Program, cache);

        Assert.Same(first, second);
    }
}
