namespace Axom.Compiler.Binding;

using System.Linq;

public static class BuiltinFunctions
{
    private static readonly TypeSymbol GenericT = TypeSymbol.Generic("T");
    private static readonly TypeSymbol GenericU = TypeSymbol.Generic("U");

    public static readonly FunctionSymbol Println = new(
        "println",
        new[] { new ParameterSymbol("value", TypeSymbol.Int) },
        Array.Empty<TypeSymbol>(),
        TypeSymbol.Unit,
        isBuiltin: true);

    public static readonly FunctionSymbol Print = new(
        "print",
        new[] { new ParameterSymbol("value", TypeSymbol.Int) },
        Array.Empty<TypeSymbol>(),
        TypeSymbol.Unit,
        isBuiltin: true);

    public static readonly FunctionSymbol Input = new(
        "input",
        Array.Empty<ParameterSymbol>(),
        Array.Empty<TypeSymbol>(),
        TypeSymbol.String,
        isBuiltin: true);

    public static readonly FunctionSymbol Len = new(
        "len",
        new[] { new ParameterSymbol("text", TypeSymbol.String) },
        Array.Empty<TypeSymbol>(),
        TypeSymbol.Int,
        isBuiltin: true);

    public static readonly FunctionSymbol Abs = new(
        "abs",
        new[] { new ParameterSymbol("value", GenericT) },
        new[] { GenericT },
        GenericT,
        isBuiltin: true);

    public static readonly FunctionSymbol Min = new(
        "min",
        new[] { new ParameterSymbol("left", GenericT), new ParameterSymbol("right", GenericT) },
        new[] { GenericT },
        GenericT,
        isBuiltin: true);

    public static readonly FunctionSymbol Max = new(
        "max",
        new[] { new ParameterSymbol("left", GenericT), new ParameterSymbol("right", GenericT) },
        new[] { GenericT },
        GenericT,
        isBuiltin: true);

    public static readonly FunctionSymbol Float = new(
        "float",
        new[] { new ParameterSymbol("value", TypeSymbol.Int) },
        Array.Empty<TypeSymbol>(),
        TypeSymbol.Float,
        isBuiltin: true);

    public static readonly FunctionSymbol Int = new(
        "int",
        new[] { new ParameterSymbol("value", TypeSymbol.Float) },
        Array.Empty<TypeSymbol>(),
        TypeSymbol.Int,
        isBuiltin: true);

    public static readonly FunctionSymbol Map = new(
        "map",
        new[]
        {
            new ParameterSymbol("items", TypeSymbol.List(GenericT)),
            new ParameterSymbol("transform", TypeSymbol.Function(new[] { GenericT }, GenericU))
        },
        new[] { GenericT, GenericU },
        TypeSymbol.List(GenericU),
        isBuiltin: true);

    public static readonly FunctionSymbol Filter = new(
        "filter",
        new[]
        {
            new ParameterSymbol("items", TypeSymbol.List(GenericT)),
            new ParameterSymbol("predicate", TypeSymbol.Function(new[] { GenericT }, TypeSymbol.Bool))
        },
        new[] { GenericT },
        TypeSymbol.List(GenericT),
        isBuiltin: true);

    public static readonly FunctionSymbol Fold = new(
        "fold",
        new[]
        {
            new ParameterSymbol("items", TypeSymbol.List(GenericT)),
            new ParameterSymbol("seed", GenericU),
            new ParameterSymbol("reducer", TypeSymbol.Function(new[] { GenericU, GenericT }, GenericU))
        },
        new[] { GenericT, GenericU },
        GenericU,
        isBuiltin: true);

    public static readonly FunctionSymbol Each = new(
        "each",
        new[]
        {
            new ParameterSymbol("items", TypeSymbol.List(GenericT)),
            new ParameterSymbol("action", TypeSymbol.Function(new[] { GenericT }, TypeSymbol.Unit))
        },
        new[] { GenericT },
        TypeSymbol.Unit,
        isBuiltin: true);

    private static readonly Dictionary<string, FunctionSymbol> ByName = new(StringComparer.Ordinal)
    {
        [Print.Name] = Print,
        [Println.Name] = Println,
        [Input.Name] = Input,
        [Len.Name] = Len,
        [Abs.Name] = Abs,
        [Min.Name] = Min,
        [Max.Name] = Max,
        [Float.Name] = Float,
        [Int.Name] = Int,
        [Map.Name] = Map,
        [Filter.Name] = Filter,
        [Fold.Name] = Fold,
        [Each.Name] = Each
    };

    public static bool TryLookup(string name, out FunctionSymbol? function) =>
        ByName.TryGetValue(name, out function);

    public static IReadOnlyList<FunctionSymbol> All => ByName.Values.ToList();
}
