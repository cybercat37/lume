namespace Axom.Compiler.Binding;

using System.Linq;

public static class BuiltinFunctions
{
    private static readonly TypeSymbol GenericT = TypeSymbol.Generic("T");
    private static readonly TypeSymbol GenericU = TypeSymbol.Generic("U");
    private static readonly TypeSymbol GenericV = TypeSymbol.Generic("V");

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

    public static readonly FunctionSymbol Str = new(
        "str",
        new[] { new ParameterSymbol("value", GenericT) },
        new[] { GenericT },
        TypeSymbol.String,
        isBuiltin: true);

    public static readonly FunctionSymbol Format = new(
        "format",
        new[] { new ParameterSymbol("value", GenericT), new ParameterSymbol("specifier", TypeSymbol.String) },
        new[] { GenericT },
        TypeSymbol.String,
        isBuiltin: true);

    public static readonly FunctionSymbol Sleep = new(
        "sleep",
        new[] { new ParameterSymbol("ms", TypeSymbol.Int) },
        Array.Empty<TypeSymbol>(),
        TypeSymbol.Unit,
        isBuiltin: true);

    public static readonly FunctionSymbol TimeNowUtc = new(
        "time_now_utc",
        Array.Empty<ParameterSymbol>(),
        Array.Empty<TypeSymbol>(),
        TypeSymbol.Instant,
        isBuiltin: true);

    public static readonly FunctionSymbol TimeAddMs = new(
        "time_add_ms",
        new[]
        {
            new ParameterSymbol("value", TypeSymbol.Instant),
            new ParameterSymbol("ms", TypeSymbol.Int)
        },
        Array.Empty<TypeSymbol>(),
        TypeSymbol.Instant,
        isBuiltin: true);

    public static readonly FunctionSymbol TimeDiffMs = new(
        "time_diff_ms",
        new[]
        {
            new ParameterSymbol("left", TypeSymbol.Instant),
            new ParameterSymbol("right", TypeSymbol.Instant)
        },
        Array.Empty<TypeSymbol>(),
        TypeSymbol.Int,
        isBuiltin: true);

    public static readonly FunctionSymbol TimeToIso = new(
        "time_to_iso",
        new[] { new ParameterSymbol("value", TypeSymbol.Instant) },
        Array.Empty<TypeSymbol>(),
        TypeSymbol.String,
        isBuiltin: true);

    public static readonly FunctionSymbol TimeToLocalIso = new(
        "time_to_local_iso",
        new[] { new ParameterSymbol("value", TypeSymbol.Instant) },
        Array.Empty<TypeSymbol>(),
        TypeSymbol.String,
        isBuiltin: true);

    public static readonly FunctionSymbol TimeFromIso = new(
        "time_from_iso",
        new[] { new ParameterSymbol("text", TypeSymbol.String) },
        Array.Empty<TypeSymbol>(),
        TypeSymbol.Result(TypeSymbol.Instant, TypeSymbol.String),
        isBuiltin: true);

    public static readonly FunctionSymbol RandFloat = new(
        "rand_float",
        Array.Empty<ParameterSymbol>(),
        Array.Empty<TypeSymbol>(),
        TypeSymbol.Float,
        isBuiltin: true);

    public static readonly FunctionSymbol RandInt = new(
        "rand_int",
        new[] { new ParameterSymbol("max", TypeSymbol.Int) },
        Array.Empty<TypeSymbol>(),
        TypeSymbol.Result(TypeSymbol.Int, TypeSymbol.String),
        isBuiltin: true);

    public static readonly FunctionSymbol RandSeed = new(
        "rand_seed",
        new[] { new ParameterSymbol("seed", TypeSymbol.Int) },
        Array.Empty<TypeSymbol>(),
        TypeSymbol.Unit,
        isBuiltin: true);

    public static readonly FunctionSymbol Range = new(
        "range",
        new[]
        {
            new ParameterSymbol("start", TypeSymbol.Int),
            new ParameterSymbol("end", TypeSymbol.Int)
        },
        Array.Empty<TypeSymbol>(),
        TypeSymbol.List(TypeSymbol.Int),
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

    public static readonly FunctionSymbol Take = new(
        "take",
        new[]
        {
            new ParameterSymbol("items", TypeSymbol.List(GenericT)),
            new ParameterSymbol("count", TypeSymbol.Int)
        },
        new[] { GenericT },
        TypeSymbol.List(GenericT),
        isBuiltin: true);

    public static readonly FunctionSymbol Skip = new(
        "skip",
        new[]
        {
            new ParameterSymbol("items", TypeSymbol.List(GenericT)),
            new ParameterSymbol("count", TypeSymbol.Int)
        },
        new[] { GenericT },
        TypeSymbol.List(GenericT),
        isBuiltin: true);

    public static readonly FunctionSymbol TakeWhile = new(
        "take_while",
        new[]
        {
            new ParameterSymbol("items", TypeSymbol.List(GenericT)),
            new ParameterSymbol("predicate", TypeSymbol.Function(new[] { GenericT }, TypeSymbol.Bool))
        },
        new[] { GenericT },
        TypeSymbol.List(GenericT),
        isBuiltin: true);

    public static readonly FunctionSymbol SkipWhile = new(
        "skip_while",
        new[]
        {
            new ParameterSymbol("items", TypeSymbol.List(GenericT)),
            new ParameterSymbol("predicate", TypeSymbol.Function(new[] { GenericT }, TypeSymbol.Bool))
        },
        new[] { GenericT },
        TypeSymbol.List(GenericT),
        isBuiltin: true);

    public static readonly FunctionSymbol Enumerate = new(
        "enumerate",
        new[]
        {
            new ParameterSymbol("items", TypeSymbol.List(GenericT))
        },
        new[] { GenericT },
        TypeSymbol.List(TypeSymbol.Tuple(new[] { TypeSymbol.Int, GenericT })),
        isBuiltin: true);

    public static readonly FunctionSymbol Count = new(
        "count",
        new[]
        {
            new ParameterSymbol("items", TypeSymbol.List(GenericT))
        },
        new[] { GenericT },
        TypeSymbol.Int,
        isBuiltin: true);

    public static readonly FunctionSymbol Sum = new(
        "sum",
        new[]
        {
            new ParameterSymbol("items", TypeSymbol.List(GenericT))
        },
        new[] { GenericT },
        GenericT,
        isBuiltin: true);

    public static readonly FunctionSymbol Any = new(
        "any",
        new[]
        {
            new ParameterSymbol("items", TypeSymbol.List(GenericT)),
            new ParameterSymbol("predicate", TypeSymbol.Function(new[] { GenericT }, TypeSymbol.Bool))
        },
        new[] { GenericT },
        TypeSymbol.Bool,
        isBuiltin: true);

    public static readonly FunctionSymbol AllItems = new(
        "all",
        new[]
        {
            new ParameterSymbol("items", TypeSymbol.List(GenericT)),
            new ParameterSymbol("predicate", TypeSymbol.Function(new[] { GenericT }, TypeSymbol.Bool))
        },
        new[] { GenericT },
        TypeSymbol.Bool,
        isBuiltin: true);

    public static readonly FunctionSymbol ResultMap = new(
        "result_map",
        new[]
        {
            new ParameterSymbol("value", TypeSymbol.Result(GenericT, GenericU)),
            new ParameterSymbol("transform", TypeSymbol.Function(new[] { GenericT }, GenericV))
        },
        new[] { GenericT, GenericU, GenericV },
        TypeSymbol.Result(GenericV, GenericU),
        isBuiltin: true);

    public static readonly FunctionSymbol Zip = new(
        "zip",
        new[]
        {
            new ParameterSymbol("left", TypeSymbol.List(GenericT)),
            new ParameterSymbol("right", TypeSymbol.List(GenericU))
        },
        new[] { GenericT, GenericU },
        TypeSymbol.List(TypeSymbol.Tuple(new[] { GenericT, GenericU })),
        isBuiltin: true);

    public static readonly FunctionSymbol ZipWith = new(
        "zip_with",
        new[]
        {
            new ParameterSymbol("left", TypeSymbol.List(GenericT)),
            new ParameterSymbol("right", TypeSymbol.List(GenericU)),
            new ParameterSymbol("combine", TypeSymbol.Function(new[] { GenericT, GenericU }, GenericV))
        },
        new[] { GenericT, GenericU, GenericV },
        TypeSymbol.List(GenericV),
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
        [Str.Name] = Str,
        [Format.Name] = Format,
        [Sleep.Name] = Sleep,
        [TimeNowUtc.Name] = TimeNowUtc,
        [TimeAddMs.Name] = TimeAddMs,
        [TimeDiffMs.Name] = TimeDiffMs,
        [TimeToIso.Name] = TimeToIso,
        [TimeToLocalIso.Name] = TimeToLocalIso,
        [TimeFromIso.Name] = TimeFromIso,
        [RandFloat.Name] = RandFloat,
        [RandInt.Name] = RandInt,
        [RandSeed.Name] = RandSeed,
        [Range.Name] = Range,
        [Map.Name] = Map,
        [Filter.Name] = Filter,
        [Fold.Name] = Fold,
        [Each.Name] = Each,
        [Take.Name] = Take,
        [Skip.Name] = Skip,
        [TakeWhile.Name] = TakeWhile,
        [SkipWhile.Name] = SkipWhile,
        [Enumerate.Name] = Enumerate,
        [Count.Name] = Count,
        [Sum.Name] = Sum,
        [Any.Name] = Any,
        [AllItems.Name] = AllItems,
        [ResultMap.Name] = ResultMap,
        [Zip.Name] = Zip,
        [ZipWith.Name] = ZipWith
    };

    public static bool TryLookup(string name, out FunctionSymbol? function) =>
        ByName.TryGetValue(name, out function);

    public static IReadOnlyList<FunctionSymbol> All => ByName.Values.ToList();
}
