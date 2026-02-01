namespace Lume.Compiler.Binding;

using System.Linq;

public sealed class FunctionSymbol
{
    public string Name { get; }
    public IReadOnlyList<ParameterSymbol> Parameters { get; }
    public TypeSymbol ReturnType { get; private set; }
    public bool IsBuiltin { get; }

    public FunctionSymbol(
        string name,
        IReadOnlyList<ParameterSymbol> parameters,
        TypeSymbol returnType,
        bool isBuiltin = false)
    {
        Name = name;
        Parameters = parameters;
        ReturnType = returnType;
        IsBuiltin = isBuiltin;
    }

    public void SetReturnType(TypeSymbol returnType)
    {
        if (IsBuiltin)
        {
            return;
        }

        ReturnType = returnType;
    }

    public IReadOnlyList<TypeSymbol> ParameterTypes =>
        Parameters.Select(parameter => parameter.Type).ToList();

    public TypeSymbol Type => TypeSymbol.Function(ParameterTypes, ReturnType);
}
