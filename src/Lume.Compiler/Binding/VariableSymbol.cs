namespace Lume.Compiler.Binding;

public sealed class VariableSymbol
{
    public string Name { get; }
    public bool IsMutable { get; }
    public TypeSymbol Type { get; }

    public VariableSymbol(string name, bool isMutable, TypeSymbol type)
    {
        Name = name;
        IsMutable = isMutable;
        Type = type;
    }
}
