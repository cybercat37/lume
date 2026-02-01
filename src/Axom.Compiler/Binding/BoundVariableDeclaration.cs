namespace Axom.Compiler.Binding;

public sealed class BoundVariableDeclaration : BoundStatement
{
    public VariableSymbol Symbol { get; }
    public BoundExpression Initializer { get; }

    public BoundVariableDeclaration(VariableSymbol symbol, BoundExpression initializer)
    {
        Symbol = symbol;
        Initializer = initializer;
    }
}
