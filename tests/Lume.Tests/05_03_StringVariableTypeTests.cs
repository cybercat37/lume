using Lume.Compiler.Binding;
using Lume.Compiler.Parsing;
using Lume.Compiler.Text;

public class StringVariableTypeTests
{
    [Fact]
    public void String_variable_accepts_string_assignment()
    {
        var sourceText = new SourceText(@"
let mut x = ""hi""
x = ""ok""
", "test.lume");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.Empty(result.Diagnostics);
    }
}
