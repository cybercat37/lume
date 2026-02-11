namespace Axom.Compiler.Binding;

public sealed class BoundBlockStatement : BoundStatement
{
    public BoundIntentAnnotation? IntentAnnotation { get; }
    public IReadOnlyList<BoundStatement> Statements { get; }
    public bool IsScopeBlock { get; }

    public BoundBlockStatement(IReadOnlyList<BoundStatement> statements, bool isScopeBlock = false, BoundIntentAnnotation? intentAnnotation = null)
    {
        IntentAnnotation = intentAnnotation;
        Statements = statements;
        IsScopeBlock = isScopeBlock;
    }
}
