using Axom.Compiler.Binding;
using Axom.Compiler.Lowering;
using Axom.Compiler.Parsing;
using Axom.Compiler.Text;

namespace Axom.Tests;

public class LoweringPassTests
{
    [Fact]
    public void Lowering_pass_preserves_bound_program_structure()
    {
        var sourceText = new SourceText(@"
type User { name: String, age: Int }
fn add(x: Int, y: Int) => x + y
let user = User { name: ""Ada"", age: 36 }
print add(user.age, 1)
", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);
        var binder = new Binder();
        var bindResult = binder.Bind(syntaxTree);

        Assert.Empty(bindResult.Diagnostics);

        var lowerer = new Lowerer();
        var lowered = lowerer.Lower(bindResult.Program);

        Assert.Same(bindResult.Program, lowered.Source);
        Assert.Same(bindResult.Program.RecordTypes, lowered.RecordTypes);
        Assert.Same(bindResult.Program.SumTypes, lowered.SumTypes);
        Assert.Same(bindResult.Program.Functions, lowered.Functions);
        Assert.Same(bindResult.Program.Statements, lowered.Statements);
    }
}
