using Axom.Compiler.Binding;

namespace Axom.Compiler.Lowering;

public sealed class LoweredBlockExpression : LoweredExpression
{
    public IReadOnlyList<LoweredStatement> Statements { get; }
    public LoweredExpression Result { get; }

    public LoweredBlockExpression(IReadOnlyList<LoweredStatement> statements, LoweredExpression result)
    {
        Statements = statements;
        Result = result;
    }

    public override TypeSymbol Type => Result.Type;
}
