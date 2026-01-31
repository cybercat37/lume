namespace Lume.Compiler.Binding;

public sealed class TypeSymbol
{
    public string Name { get; }

    private TypeSymbol(string name)
    {
        Name = name;
    }

    public static TypeSymbol Int { get; } = new("Int");
    public static TypeSymbol Bool { get; } = new("Bool");
    public static TypeSymbol String { get; } = new("String");
    public static TypeSymbol Error { get; } = new("Error");

    public override string ToString() => Name;

    public override bool Equals(object? obj)
    {
        return obj is TypeSymbol other && string.Equals(Name, other.Name, StringComparison.Ordinal);
    }

    public override int GetHashCode()
    {
        return StringComparer.Ordinal.GetHashCode(Name);
    }
}
