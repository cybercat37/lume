using Lume.Compiler.Binding;
using Lume.Compiler.Parsing;
using Lume.Compiler.Text;

public class ShadowedMutabilityTests
{
    [Fact]
    public void Assignment_uses_nearest_shadowed_symbol()
    {
        var sourceText = new SourceText(@"
let x = 1
{
let mut x = 2
x = 3
}
", "test.lume");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.Empty(result.Diagnostics);
    }
}
