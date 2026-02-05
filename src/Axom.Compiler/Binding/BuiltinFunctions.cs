namespace Axom.Compiler.Binding;

using System.Linq;

public static class BuiltinFunctions
{
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
        new[] { new ParameterSymbol("value", TypeSymbol.Int) },
        Array.Empty<TypeSymbol>(),
        TypeSymbol.Int,
        isBuiltin: true);

    public static readonly FunctionSymbol Min = new(
        "min",
        new[] { new ParameterSymbol("left", TypeSymbol.Int), new ParameterSymbol("right", TypeSymbol.Int) },
        Array.Empty<TypeSymbol>(),
        TypeSymbol.Int,
        isBuiltin: true);

    public static readonly FunctionSymbol Max = new(
        "max",
        new[] { new ParameterSymbol("left", TypeSymbol.Int), new ParameterSymbol("right", TypeSymbol.Int) },
        Array.Empty<TypeSymbol>(),
        TypeSymbol.Int,
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
        [Int.Name] = Int
    };

    public static bool TryLookup(string name, out FunctionSymbol? function) =>
        ByName.TryGetValue(name, out function);

    public static IReadOnlyList<FunctionSymbol> All => ByName.Values.ToList();
}
