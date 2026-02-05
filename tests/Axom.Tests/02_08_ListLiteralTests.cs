using Axom.Compiler.Binding;
using Axom.Compiler.Interpreting;
using Axom.Compiler.Parsing;
using Axom.Compiler.Text;

public class ListLiteralTests
{
    [Fact]
    public void List_literal_parses()
    {
        var sourceText = new SourceText("let xs = [1, 2, 3]", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        Assert.Empty(syntaxTree.Diagnostics);
    }

    [Fact]
    public void List_literal_requires_consistent_types()
    {
        var sourceText = new SourceText("let xs = [1, \"two\"]", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.NotEmpty(result.Diagnostics);
    }

    [Fact]
    public void List_literal_prints()
    {
        var sourceText = new SourceText("print [1, 2, 3]", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        Assert.Equal("[1, 2, 3]", result.Output.Trim());
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void List_indexing_prints_element()
    {
        var sourceText = new SourceText("print [10, 20, 30][1]", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        Assert.Equal("20", result.Output.Trim());
        Assert.Empty(result.Diagnostics);
    }
}
