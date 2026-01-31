using Lume.Compiler.Binding;
using Lume.Compiler.Parsing;
using Lume.Compiler.Text;

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
", "test.lume");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.NotEmpty(result.Diagnostics);
    }
}
