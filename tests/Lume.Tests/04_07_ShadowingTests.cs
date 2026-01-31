using Lume.Compiler.Binding;
using Lume.Compiler.Parsing;
using Lume.Compiler.Text;

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
", "test.lume");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.Empty(result.Diagnostics);
    }
}
