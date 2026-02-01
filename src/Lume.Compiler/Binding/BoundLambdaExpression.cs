namespace Lume.Compiler.Binding;

public sealed class BoundLambdaExpression : BoundExpression
{
    public IReadOnlyList<VariableSymbol> Parameters { get; }
    public BoundBlockStatement Body { get; }
    public IReadOnlyList<VariableSymbol> Captures { get; }
    public TypeSymbol FunctionType { get; }

    public BoundLambdaExpression(
        IReadOnlyList<VariableSymbol> parameters,
        BoundBlockStatement body,
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
