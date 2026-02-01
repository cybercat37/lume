using Axom.Compiler.Binding;
using Axom.Compiler.Parsing;
using Axom.Compiler.Text;

public class UndefinedVariableInBlockTests
{
    [Fact]
    public void Undefined_name_inside_block_produces_diagnostic()
    {
        var sourceText = new SourceText(@"
{
print x
}
", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.NotEmpty(result.Diagnostics);
    }
}
