namespace Axom.Compiler.Binding;

public sealed class BoundListPattern : BoundPattern
{
    public IReadOnlyList<BoundPattern> PrefixElements { get; }
    public BoundPattern? RestPattern { get; }
    public IReadOnlyList<BoundPattern> SuffixElements { get; }
    public override TypeSymbol Type { get; }

    public bool HasRest => RestPattern is not null;

    public BoundListPattern(
        IReadOnlyList<BoundPattern> prefixElements,
        BoundPattern? restPattern,
        IReadOnlyList<BoundPattern> suffixElements,
        TypeSymbol type)
    {
        PrefixElements = prefixElements;
        RestPattern = restPattern;
        SuffixElements = suffixElements;
        Type = type;
    }
}
