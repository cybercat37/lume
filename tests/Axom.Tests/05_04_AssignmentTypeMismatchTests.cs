using Axom.Compiler.Binding;
using Axom.Compiler.Parsing;
using Axom.Compiler.Text;

public class AssignmentTypeMismatchTests
{
    [Fact]
    public void Assigning_string_to_int_produces_diagnostic()
    {
        var sourceText = new SourceText(@"
let x = 1
x = ""hello""
", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.NotEmpty(result.Diagnostics);
    }
}
