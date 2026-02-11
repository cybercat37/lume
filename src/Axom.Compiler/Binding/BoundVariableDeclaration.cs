namespace Axom.Compiler.Binding;

public sealed class BoundVariableDeclaration : BoundStatement
{
    public BoundIntentAnnotation? IntentAnnotation { get; }
    public VariableSymbol Symbol { get; }
    public BoundExpression Initializer { get; }

    public BoundVariableDeclaration(VariableSymbol symbol, BoundExpression initializer, BoundIntentAnnotation? intentAnnotation = null)
    {
        IntentAnnotation = intentAnnotation;
        Symbol = symbol;
        Initializer = initializer;
    }
}
