using Axom.Compiler.Binding;
using Axom.Compiler.Parsing;
using Axom.Compiler.Text;

public class AssignmentBoolToIntTests
{
    [Fact]
    public void Assigning_bool_to_int_produces_diagnostic()
    {
        var sourceText = new SourceText(@"
let mut x = 1
x = true
", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.NotEmpty(result.Diagnostics);
    }
}
