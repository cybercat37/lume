namespace Lume.Compiler.Binding;

public sealed class VariableSymbol
{
    public string Name { get; }
    public bool IsMutable { get; }

    public VariableSymbol(string name, bool isMutable)
    {
        Name = name;
        IsMutable = isMutable;
    }
}
