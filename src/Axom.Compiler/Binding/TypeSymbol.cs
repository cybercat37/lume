namespace Axom.Compiler.Binding;

using System.Linq;

public sealed class TypeSymbol
{
    public string Name { get; }
    public IReadOnlyList<TypeSymbol>? ParameterTypes { get; }
    public TypeSymbol? ReturnType { get; }
    public IReadOnlyList<TypeSymbol>? TupleElementTypes { get; }
    public TypeSymbol? ListElementType { get; }
    public TypeSymbol? MapValueType { get; }
    public TypeSymbol? TaskResultType { get; }
    public TypeSymbol? ChannelElementType { get; }
    public TypeSymbol? ResultValueType { get; }
    public TypeSymbol? ResultErrorType { get; }
    public bool IsChannelSender { get; }
    public bool IsChannelReceiver { get; }
    public IReadOnlyList<SumVariantSymbol>? SumVariants { get; }
    public bool IsGenericParameter { get; }

    private TypeSymbol(
        string name,
        IReadOnlyList<TypeSymbol>? parameterTypes = null,
        TypeSymbol? returnType = null,
        IReadOnlyList<TypeSymbol>? tupleElementTypes = null,
        TypeSymbol? listElementType = null,
        TypeSymbol? mapValueType = null,
        TypeSymbol? taskResultType = null,
        TypeSymbol? channelElementType = null,
        TypeSymbol? resultValueType = null,
        TypeSymbol? resultErrorType = null,
        bool isChannelSender = false,
        bool isChannelReceiver = false,
        IReadOnlyList<SumVariantSymbol>? sumVariants = null,
        bool isGenericParameter = false)
    {
        Name = name;
        ParameterTypes = parameterTypes;
        ReturnType = returnType;
        TupleElementTypes = tupleElementTypes;
        ListElementType = listElementType;
        MapValueType = mapValueType;
        TaskResultType = taskResultType;
        ChannelElementType = channelElementType;
        ResultValueType = resultValueType;
        ResultErrorType = resultErrorType;
        IsChannelSender = isChannelSender;
        IsChannelReceiver = isChannelReceiver;
        SumVariants = sumVariants;
        IsGenericParameter = isGenericParameter;
    }

    public static TypeSymbol Int { get; } = new("Int");
    public static TypeSymbol Float { get; } = new("Float");
    public static TypeSymbol Bool { get; } = new("Bool");
    public static TypeSymbol String { get; } = new("String");
    public static TypeSymbol Instant { get; } = new("Instant");
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

    public static TypeSymbol List(TypeSymbol elementType)
    {
        var name = $"List<{elementType.Name}>";
        return new TypeSymbol(name, listElementType: elementType);
    }

    public static TypeSymbol Map(TypeSymbol valueType)
    {
        var name = $"Map<String, {valueType.Name}>";
        return new TypeSymbol(name, mapValueType: valueType);
    }

    public static TypeSymbol Task(TypeSymbol resultType)
    {
        var name = $"Task<{resultType.Name}>";
        return new TypeSymbol(name, taskResultType: resultType);
    }

    public static TypeSymbol Sender(TypeSymbol elementType)
    {
        var name = $"Sender<{elementType.Name}>";
        return new TypeSymbol(name, channelElementType: elementType, isChannelSender: true);
    }

    public static TypeSymbol Receiver(TypeSymbol elementType)
    {
        var name = $"Receiver<{elementType.Name}>";
        return new TypeSymbol(name, channelElementType: elementType, isChannelReceiver: true);
    }

    public static TypeSymbol Result(TypeSymbol valueType, TypeSymbol errorType)
    {
        var name = $"Result<{valueType.Name}, {errorType.Name}>";
        return new TypeSymbol(name, resultValueType: valueType, resultErrorType: errorType);
    }

    public static TypeSymbol Record(string name)
    {
        return new TypeSymbol(name);
    }

    public static TypeSymbol Generic(string name)
    {
        return new TypeSymbol(name, isGenericParameter: true);
    }

    public static TypeSymbol Sum(string name)
    {
        return new TypeSymbol(name, sumVariants: Array.Empty<SumVariantSymbol>());
    }

    public override string ToString() => Name;

    public override bool Equals(object? obj)
    {
        return obj is TypeSymbol other && string.Equals(Name, other.Name, StringComparison.Ordinal);
    }

    public static bool operator ==(TypeSymbol? left, TypeSymbol? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(TypeSymbol? left, TypeSymbol? right)
    {
        return !Equals(left, right);
    }

    public override int GetHashCode()
    {
        return StringComparer.Ordinal.GetHashCode(Name);
    }
}
