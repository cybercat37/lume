using Axom.Compiler.Lowering;

namespace Axom.Compiler.Emitting;

public sealed class EmitCache
{
    private readonly Dictionary<int, string> cache = new();

    public bool TryGet(LoweredProgram program, out string? code) =>
        cache.TryGetValue(program.GetHashCode(), out code);

    public void Store(LoweredProgram program, string code)
    {
        cache[program.GetHashCode()] = code;
    }
}
