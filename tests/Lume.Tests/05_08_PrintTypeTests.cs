using Lume.Compiler.Binding;
using Lume.Compiler.Parsing;
using Lume.Compiler.Text;

public class PrintTypeTests
{
    [Fact]
    public void Print_string_and_bool_bind_without_diagnostics()
    {
        var sourceText = new SourceText(@"
print ""hi""
print true
", "test.lume");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.Empty(result.Diagnostics);
    }
}
