namespace Axom.Compiler.Binding;

using System.Linq;

public sealed class FunctionSymbol
{
    public string Name { get; }
    public IReadOnlyList<ParameterSymbol> Parameters { get; }
    public IReadOnlyList<TypeSymbol> GenericParameters { get; }
    public TypeSymbol ReturnType { get; private set; }
    public bool IsBuiltin { get; }
    public bool EnableLogging { get; }

    public FunctionSymbol(
        string name,
        IReadOnlyList<ParameterSymbol> parameters,
        IReadOnlyList<TypeSymbol> genericParameters,
        TypeSymbol returnType,
        bool isBuiltin = false,
        bool enableLogging = false)
    {
        Name = name;
        Parameters = parameters;
        GenericParameters = genericParameters;
        ReturnType = returnType;
        IsBuiltin = isBuiltin;
        EnableLogging = enableLogging;
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
