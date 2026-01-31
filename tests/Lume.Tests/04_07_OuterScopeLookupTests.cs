using Lume.Compiler.Binding;
using Lume.Compiler.Parsing;
using Lume.Compiler.Text;

public class OuterScopeLookupTests
{
    [Fact]
    public void Inner_scope_can_read_outer_variable()
    {
        var sourceText = new SourceText(@"
let x = 1
{
print x
}
", "test.lume");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.Empty(result.Diagnostics);
    }
}
