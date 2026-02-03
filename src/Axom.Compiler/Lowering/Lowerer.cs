using Axom.Compiler.Binding;

namespace Axom.Compiler.Lowering;

public sealed class Lowerer
{
    public LoweredProgram Lower(BoundProgram program)
    {
        return new LoweredProgram(
            program,
            program.RecordTypes,
            program.SumTypes,
            program.Functions,
            program.Statements);
    }
}
