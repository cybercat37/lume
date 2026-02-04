namespace Axom.Compiler.Binding;

public sealed class BoundDeconstructionStatement : BoundStatement
{
    public BoundPattern Pattern { get; }
    public BoundExpression Initializer { get; }

    public BoundDeconstructionStatement(BoundPattern pattern, BoundExpression initializer)
    {
        Pattern = pattern;
        Initializer = initializer;
    }
}
