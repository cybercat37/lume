namespace Lume.Compiler.Binding;

public sealed class BoundFunctionDeclaration : BoundNode
{
    public FunctionSymbol Symbol { get; }
    public IReadOnlyList<VariableSymbol> Parameters { get; }
    public BoundBlockStatement Body { get; }

    public BoundFunctionDeclaration(
        FunctionSymbol symbol,
        IReadOnlyList<VariableSymbol> parameters,
        BoundBlockStatement body)
    {
        Symbol = symbol;
        Parameters = parameters;
        Body = body;
    }
}
