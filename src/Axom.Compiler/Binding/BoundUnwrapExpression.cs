namespace Axom.Compiler.Binding;

public sealed class BoundUnwrapExpression : BoundExpression
{
    public BoundExpression Expression { get; }
    public SumVariantSymbol SuccessVariant { get; }
    public SumVariantSymbol FailureVariant { get; }
    public override TypeSymbol Type { get; }

    public BoundUnwrapExpression(
        BoundExpression expression,
        SumVariantSymbol successVariant,
        SumVariantSymbol failureVariant,
        TypeSymbol type)
    {
        Expression = expression;
        SuccessVariant = successVariant;
        FailureVariant = failureVariant;
        Type = type;
    }
}
