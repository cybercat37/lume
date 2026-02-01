using Axom.Compiler.Binding;
using Axom.Compiler.Parsing;
using Axom.Compiler.Text;

public class TypeMismatchMessageTests
{
    [Fact]
    public void Assignment_type_mismatch_message_includes_types()
    {
        var sourceText = new SourceText(@"
let mut x = 1
x = ""hi""
", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.NotEmpty(result.Diagnostics);
        Assert.Contains("Cannot assign expression of type", result.Diagnostics[0].Message);
        Assert.Contains("Int", result.Diagnostics[0].Message);
        Assert.Contains("String", result.Diagnostics[0].Message);
    }
}
