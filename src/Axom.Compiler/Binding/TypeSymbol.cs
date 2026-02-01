namespace Axom.Compiler.Binding;

using System.Linq;

public sealed class TypeSymbol
{
    public string Name { get; }
    public IReadOnlyList<TypeSymbol>? ParameterTypes { get; }
    public TypeSymbol? ReturnType { get; }
    public IReadOnlyList<TypeSymbol>? TupleElementTypes { get; }

    private TypeSymbol(
        string name,
        IReadOnlyList<TypeSymbol>? parameterTypes = null,
        TypeSymbol? returnType = null,
        IReadOnlyList<TypeSymbol>? tupleElementTypes = null)
    {
        Name = name;
        ParameterTypes = parameterTypes;
        ReturnType = returnType;
        TupleElementTypes = tupleElementTypes;
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

    public static TypeSymbol Tuple(IReadOnlyList<TypeSymbol> elementTypes)
    {
        var signature = string.Join(", ", elementTypes.Select(type => type.Name));
        var name = $"({signature})";
        return new TypeSymbol(name, tupleElementTypes: elementTypes);
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
