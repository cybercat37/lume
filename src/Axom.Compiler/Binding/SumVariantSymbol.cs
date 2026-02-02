namespace Axom.Compiler.Binding;

public sealed class SumVariantSymbol
{
    public string Name { get; }
    public TypeSymbol DeclaringType { get; }
    public TypeSymbol? PayloadType { get; }

    public SumVariantSymbol(string name, TypeSymbol declaringType, TypeSymbol? payloadType)
    {
        Name = name;
        DeclaringType = declaringType;
        PayloadType = payloadType;
    }
}
