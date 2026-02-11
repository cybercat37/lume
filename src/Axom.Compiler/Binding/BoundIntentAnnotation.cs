using Axom.Compiler.Text;

namespace Axom.Compiler.Binding;

public sealed class BoundIntentAnnotation
{
    public string Message { get; }
    public IReadOnlyList<string> Effects { get; }
    public TextSpan Span { get; }

    public BoundIntentAnnotation(string message, IReadOnlyList<string> effects, TextSpan span)
    {
        Message = message;
        Effects = effects;
        Span = span;
    }
}
