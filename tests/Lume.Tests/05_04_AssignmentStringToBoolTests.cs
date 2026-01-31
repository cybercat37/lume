using Lume.Compiler.Binding;
using Lume.Compiler.Parsing;
using Lume.Compiler.Text;

public class AssignmentStringToBoolTests
{
    [Fact]
    public void Assigning_string_to_bool_produces_diagnostic()
    {
        var sourceText = new SourceText(@"
let mut x = true
x = ""hi""
", "test.lume");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.NotEmpty(result.Diagnostics);
    }
}
