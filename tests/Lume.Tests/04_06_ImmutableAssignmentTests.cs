using Lume.Compiler.Binding;
using Lume.Compiler.Parsing;
using Lume.Compiler.Text;

public class ImmutableAssignmentTests
{
    [Fact]
    public void Assigning_to_immutable_variable_produces_diagnostic()
    {
        var sourceText = new SourceText(@"
let x = 1
x = 2
", "test.lume");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.NotEmpty(result.Diagnostics);
    }
}
