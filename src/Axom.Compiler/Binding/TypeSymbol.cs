namespace Axom.Compiler.Binding;

using System.Linq;

public sealed class TypeSymbol
{
    public string Name { get; }
    public IReadOnlyList<TypeSymbol>? ParameterTypes { get; }
    public TypeSymbol? ReturnType { get; }

    private TypeSymbol(string name, IReadOnlyList<TypeSymbol>? parameterTypes = null, TypeSymbol? returnType = null)
    {
        Name = name;
        ParameterTypes = parameterTypes;
        ReturnType = returnType;
    }

    public static TypeSymbol Int { get; } = new("Int");
    public static TypeSymbol Bool { get; } = new("Bool");
    public static TypeSymbol String { get; } = new("String");
    public static TypeSymbol Error { get; } = new("Error");
    public static TypeSymbol Unit { get; } = new("Unit");

    public static TypeSymbol Function(IReadOnlyList<TypeSymbol> parameterTypes, TypeSymbol returnType)
    {
        var signature = string.Join(", ", parameterTypes.Select(type => type.Name));
        var name = $"fn({signature}) -> {returnType.Name}";
        return new TypeSymbol(name, parameterTypes, returnType);
    }

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
