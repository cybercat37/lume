using Axom.Compiler;
using Axom.Compiler.Binding;
using Axom.Compiler.Emitting;
using Axom.Compiler.Lowering;

namespace Axom.Tests;

public class EmitCacheTests
{
    [Fact]
    public void Emit_cached_reuses_code_for_same_program()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile("print 1", "test.axom");

        Assert.True(result.Success);

        var binder = new Binder();
        var bindResult = binder.Bind(result.SyntaxTree);
        var lowerer = new Lowerer();
        var loweredProgram = lowerer.Lower(bindResult.Program);
        var cache = new EmitCache();
        var emitter = new Emitter();

        var first = emitter.EmitCached(loweredProgram, cache);
        var second = emitter.EmitCached(loweredProgram, cache);

        Assert.Same(first, second);
    }
}
