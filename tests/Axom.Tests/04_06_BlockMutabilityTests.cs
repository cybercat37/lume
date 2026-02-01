using Axom.Compiler.Binding;
using Axom.Compiler.Parsing;
using Axom.Compiler.Text;

public class BlockMutabilityTests
{
    [Fact]
    public void Assignment_to_mutable_variable_inside_block_is_allowed()
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
