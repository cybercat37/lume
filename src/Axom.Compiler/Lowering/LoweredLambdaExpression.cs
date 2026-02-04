using Axom.Compiler.Binding;

namespace Axom.Compiler.Lowering;

public sealed class LoweredLambdaExpression : LoweredExpression
{
    public IReadOnlyList<VariableSymbol> Parameters { get; }
    public LoweredBlockStatement Body { get; }
    public IReadOnlyList<VariableSymbol> Captures { get; }
    public TypeSymbol FunctionType { get; }

    public LoweredLambdaExpression(
        IReadOnlyList<VariableSymbol> parameters,
        LoweredBlockStatement body,
        IReadOnlyList<VariableSymbol> captures,
        TypeSymbol functionType)
    {
        Parameters = parameters;
        Body = body;
        Captures = captures;
        FunctionType = functionType;
    }

    public override TypeSymbol Type => FunctionType;
}
