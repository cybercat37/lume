using Axom.Compiler.Binding;
using Axom.Compiler.Parsing;
using Axom.Compiler.Text;

public class ShadowingTests
{
    [Fact]
    public void Inner_scope_can_shadow_outer_variable()
    {
        var sourceText = new SourceText(@"
let x = 1
{
let x = 2
print x
}
", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.Empty(result.Diagnostics);
    }
}
