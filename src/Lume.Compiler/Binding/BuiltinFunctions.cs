namespace Lume.Compiler.Binding;

public static class BuiltinFunctions
{
    public static readonly FunctionSymbol Println = new(
        "println",
        new[] { TypeSymbol.Int },
        TypeSymbol.Unit);

    public static readonly FunctionSymbol Print = new(
        "print",
        new[] { TypeSymbol.Int },
        TypeSymbol.Unit);

    public static readonly FunctionSymbol Input = new(
        "input",
        Array.Empty<TypeSymbol>(),
        TypeSymbol.String);

    public static readonly FunctionSymbol Len = new(
        "len",
        new[] { TypeSymbol.String },
        TypeSymbol.Int);

    public static readonly FunctionSymbol Abs = new(
        "abs",
        new[] { TypeSymbol.Int },
        TypeSymbol.Int);

    public static readonly FunctionSymbol Min = new(
        "min",
        new[] { TypeSymbol.Int, TypeSymbol.Int },
        TypeSymbol.Int);

    public static readonly FunctionSymbol Max = new(
        "max",
        new[] { TypeSymbol.Int, TypeSymbol.Int },
        TypeSymbol.Int);

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
}
