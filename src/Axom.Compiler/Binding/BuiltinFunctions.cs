namespace Axom.Compiler.Binding;

using System.Linq;

public static class BuiltinFunctions
{
    public static readonly FunctionSymbol Println = new(
        "println",
        new[] { new ParameterSymbol("value", TypeSymbol.Int) },
        TypeSymbol.Unit,
        isBuiltin: true);

    public static readonly FunctionSymbol Print = new(
        "print",
        new[] { new ParameterSymbol("value", TypeSymbol.Int) },
        TypeSymbol.Unit,
        isBuiltin: true);

    public static readonly FunctionSymbol Input = new(
        "input",
        Array.Empty<ParameterSymbol>(),
        TypeSymbol.String,
        isBuiltin: true);

    public static readonly FunctionSymbol Len = new(
        "len",
        new[] { new ParameterSymbol("text", TypeSymbol.String) },
        TypeSymbol.Int,
        isBuiltin: true);

    public static readonly FunctionSymbol Abs = new(
        "abs",
        new[] { new ParameterSymbol("value", TypeSymbol.Int) },
        TypeSymbol.Int,
        isBuiltin: true);

    public static readonly FunctionSymbol Min = new(
        "min",
        new[] { new ParameterSymbol("left", TypeSymbol.Int), new ParameterSymbol("right", TypeSymbol.Int) },
        TypeSymbol.Int,
        isBuiltin: true);

    public static readonly FunctionSymbol Max = new(
        "max",
        new[] { new ParameterSymbol("left", TypeSymbol.Int), new ParameterSymbol("right", TypeSymbol.Int) },
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
        [Max.Name] = Max
    };

    public static bool TryLookup(string name, out FunctionSymbol? function) =>
        ByName.TryGetValue(name, out function);

    public static IReadOnlyList<FunctionSymbol> All => ByName.Values.ToList();
}
