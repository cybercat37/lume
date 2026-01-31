namespace Lume.Compiler.Binding;

public sealed class FunctionSymbol
{
    public string Name { get; }
    public IReadOnlyList<TypeSymbol> ParameterTypes { get; }
    public TypeSymbol ReturnType { get; }

    public FunctionSymbol(string name, IReadOnlyList<TypeSymbol> parameterTypes, TypeSymbol returnType)
    {
        Name = name;
        ParameterTypes = parameterTypes;
        ReturnType = returnType;
    }
}
