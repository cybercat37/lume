namespace Lume.Compiler.Binding;

public sealed class ParameterSymbol
{
    public string Name { get; }
    public TypeSymbol Type { get; }

    public ParameterSymbol(string name, TypeSymbol type)
    {
        Name = name;
        Type = type;
    }
}
