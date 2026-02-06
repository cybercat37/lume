namespace Axom.Compiler.Binding;

public sealed class BoundSpawnExpression : BoundExpression
{
    public BoundBlockStatement Body { get; }
    public override TypeSymbol Type { get; }

    public BoundSpawnExpression(BoundBlockStatement body, TypeSymbol type)
    {
        Body = body;
        Type = type;
    }
}
