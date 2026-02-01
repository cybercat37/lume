using Axom.Compiler.Binding;
using Axom.Compiler.Parsing;
using Axom.Compiler.Text;

public class ImmutableAssignmentTests
{
    [Fact]
    public void Assigning_to_immutable_variable_produces_diagnostic()
    {
        var sourceText = new SourceText(@"
let x = 1
x = 2
", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.NotEmpty(result.Diagnostics);
    }
}
