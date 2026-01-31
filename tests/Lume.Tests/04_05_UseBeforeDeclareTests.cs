using Lume.Compiler.Binding;
using Lume.Compiler.Parsing;
using Lume.Compiler.Text;

public class UseBeforeDeclareTests
{
    [Fact]
    public void Use_before_declare_produces_diagnostic()
    {
        var sourceText = new SourceText(@"
print x
let x = 1
", "test.lume");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.NotEmpty(result.Diagnostics);
    }
}
