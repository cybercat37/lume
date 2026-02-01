using Axom.Compiler.Binding;
using Axom.Compiler.Parsing;
using Axom.Compiler.Text;

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
", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.Empty(result.Diagnostics);
    }
}
