using Lume.Compiler.Binding;

namespace Lume.Compiler.Emitting;

public sealed class EmitCache
{
    private readonly Dictionary<int, string> cache = new();

    public bool TryGet(BoundProgram program, out string? code) =>
        cache.TryGetValue(program.GetHashCode(), out code);

    public void Store(BoundProgram program, string code)
    {
        cache[program.GetHashCode()] = code;
    }
}
