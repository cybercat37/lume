using Lume.Compiler.Binding;
using Lume.Compiler.Parsing;
using Lume.Compiler.Text;

public class InputTypeTests
{
    [Fact]
    public void Input_binds_as_string_variable()
    {
        var sourceText = new SourceText("let x = input\nprint x", "test.lume");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.Empty(result.Diagnostics);
    }
}
