using Axom.Compiler.Binding;
using Axom.Compiler.Parsing;
using Axom.Compiler.Text;

public class UseBeforeDeclareTests
{
    [Fact]
    public void Use_before_declare_produces_diagnostic()
    {
        var sourceText = new SourceText(@"
print x
let x = 1
", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.NotEmpty(result.Diagnostics);
    }
}
