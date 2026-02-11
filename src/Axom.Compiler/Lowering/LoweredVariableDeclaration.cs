using Axom.Compiler.Binding;

namespace Axom.Compiler.Lowering;

public sealed class LoweredVariableDeclaration : LoweredStatement
{
    public LoweredIntentAnnotation? IntentAnnotation { get; }
    public VariableSymbol Symbol { get; }
    public LoweredExpression Initializer { get; }

    public LoweredVariableDeclaration(VariableSymbol symbol, LoweredExpression initializer, LoweredIntentAnnotation? intentAnnotation = null)
    {
        IntentAnnotation = intentAnnotation;
        Symbol = symbol;
        Initializer = initializer;
    }
}
