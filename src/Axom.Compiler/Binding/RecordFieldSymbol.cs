namespace Axom.Compiler.Binding;

public sealed class RecordFieldSymbol
{
    public string Name { get; }
    public TypeSymbol Type { get; }

    public RecordFieldSymbol(string name, TypeSymbol type)
    {
        Name = name;
        Type = type;
    }
}
