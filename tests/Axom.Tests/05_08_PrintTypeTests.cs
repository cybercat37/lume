using Axom.Compiler.Binding;
using Axom.Compiler.Parsing;
using Axom.Compiler.Text;

public class PrintTypeTests
{
    [Fact]
    public void Print_string_and_bool_bind_without_diagnostics()
    {
        var sourceText = new SourceText(@"
print ""hi""
print true
", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.Empty(result.Diagnostics);
    }
}
