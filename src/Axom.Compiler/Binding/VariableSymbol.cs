namespace Axom.Compiler.Binding;

public sealed class VariableSymbol
{
    public string Name { get; }
    public bool IsMutable { get; }
    public TypeSymbol Type { get; }
    public int DeclaredScopeDepth { get; }

    public VariableSymbol(string name, bool isMutable, TypeSymbol type, int declaredScopeDepth = 0)
    {
        Name = name;
        IsMutable = isMutable;
        Type = type;
        DeclaredScopeDepth = declaredScopeDepth;
    }
}
