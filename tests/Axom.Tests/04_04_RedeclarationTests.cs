using Axom.Compiler.Binding;
using Axom.Compiler.Parsing;
using Axom.Compiler.Text;

public class RedeclarationTests
{
    [Fact]
    public void Redeclaration_in_same_scope_produces_diagnostic()
    {
        var sourceText = new SourceText(@"
let x = 1
let x = 2
", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.NotEmpty(result.Diagnostics);
    }
}
