using Axom.Compiler.Binding;

namespace Axom.Compiler.Lowering;

public sealed class LoweredFunctionDeclaration
{
    public FunctionSymbol Symbol { get; }
    public IReadOnlyList<VariableSymbol> Parameters { get; }
    public LoweredBlockStatement Body { get; }

    public LoweredFunctionDeclaration(
        FunctionSymbol symbol,
        IReadOnlyList<VariableSymbol> parameters,
        LoweredBlockStatement body)
    {
        Symbol = symbol;
        Parameters = parameters;
        Body = body;
    }
}
