using Axom.Compiler.Interpreting;
using Axom.Compiler.Parsing;
using Axom.Compiler.Text;

public class InterpreterTakeSkipZipTests
{
    [Fact]
    public void Take_returns_first_n_items()
    {
        var sourceText = new SourceText("print take([1, 2, 3, 4], 2)", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        Assert.Equal("[1, 2]", result.Output.Trim());
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Skip_discards_first_n_items()
    {
        var sourceText = new SourceText("print skip([1, 2, 3, 4], 2)", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        Assert.Equal("[3, 4]", result.Output.Trim());
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Zip_pairs_items_until_shortest_list_ends()
    {
        var sourceText = new SourceText("print zip([1, 2, 3], [10, 20])", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        Assert.Equal("[(1, 10), (2, 20)]", result.Output.Trim());
        Assert.Empty(result.Diagnostics);
    }
}
