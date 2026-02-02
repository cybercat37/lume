namespace Axom.Compiler.Binding;

public sealed class BoundSumTypeDeclaration : BoundNode
{
    public TypeSymbol Type { get; }
    public IReadOnlyList<SumVariantSymbol> Variants { get; }

    public BoundSumTypeDeclaration(TypeSymbol type, IReadOnlyList<SumVariantSymbol> variants)
    {
        Type = type;
        Variants = variants;
    }
}
