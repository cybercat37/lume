using Axom.Compiler.Binding;
using Axom.Compiler.Parsing;
using Axom.Compiler.Text;

public class StringConcatTypeTests
{
    [Fact]
    public void String_concatenation_is_allowed()
    {
        var sourceText = new SourceText("print \"a\" + \"b\"", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void String_concatenation_rejects_non_strings()
    {
        var sourceText = new SourceText("print \"a\" + 1", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.NotEmpty(result.Diagnostics);
    }
}
