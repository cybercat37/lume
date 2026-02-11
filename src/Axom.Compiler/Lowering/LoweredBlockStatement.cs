namespace Axom.Compiler.Lowering;

public sealed class LoweredBlockStatement : LoweredStatement
{
    public LoweredIntentAnnotation? IntentAnnotation { get; }
    public IReadOnlyList<LoweredStatement> Statements { get; }
    public bool IsScopeBlock { get; }
    public bool IsTransparent { get; }

    public LoweredBlockStatement(
        IReadOnlyList<LoweredStatement> statements,
        bool isScopeBlock = false,
        bool isTransparent = false,
        LoweredIntentAnnotation? intentAnnotation = null)
    {
        IntentAnnotation = intentAnnotation;
        Statements = statements;
        IsScopeBlock = isScopeBlock;
        IsTransparent = isTransparent;
    }
}
