namespace Axom.Compiler.Binding;

public sealed class BoundDeferStatement : BoundStatement
{
    public BoundStatement Statement { get; }

    public BoundDeferStatement(BoundStatement statement)
    {
        Statement = statement;
    }
}
