using Axom.Compiler.Binding;
using Axom.Compiler.Parsing;
using Axom.Compiler.Text;

public class VariableTypeTests
{
    [Fact]
    public void Inner_scope_assignment_uses_declared_type()
    {
        var sourceText = new SourceText(@"
let mut x = 1
{
x = 2
}
", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.Empty(result.Diagnostics);
    }
}
