namespace Axom.Compiler.Lowering;

public sealed class LoweredIntentAnnotation
{
    public string Message { get; }
    public IReadOnlyList<string> Effects { get; }

    public LoweredIntentAnnotation(string message, IReadOnlyList<string> effects)
    {
        Message = message;
        Effects = effects;
    }
}
