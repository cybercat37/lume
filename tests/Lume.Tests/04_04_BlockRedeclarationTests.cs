using Lume.Compiler.Binding;
using Lume.Compiler.Parsing;
using Lume.Compiler.Text;

public class BlockRedeclarationTests
{
    [Fact]
    public void Redeclaration_in_same_block_produces_diagnostic()
    {
        var sourceText = new SourceText(@"
{
let x = 1
let x = 2
}
", "test.lume");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.NotEmpty(result.Diagnostics);
    }
}
