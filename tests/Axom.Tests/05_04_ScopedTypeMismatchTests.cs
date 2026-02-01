using Axom.Compiler.Binding;
using Axom.Compiler.Parsing;
using Axom.Compiler.Text;

public class ScopedTypeMismatchTests
{
    [Fact]
    public void Assignment_type_mismatch_in_inner_scope_produces_diagnostic()
    {
        var sourceText = new SourceText(@"
let mut x = 1
{
x = ""hi""
}
", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.NotEmpty(result.Diagnostics);
    }
}
