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
    public void Take_while_takes_prefix_while_predicate_is_true()
    {
        var sourceText = new SourceText("print take_while([1, 2, 3, 1], fn(x: Int) => x < 3)", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        Assert.Equal("[1, 2]", result.Output.Trim());
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Skip_while_skips_prefix_while_predicate_is_true()
    {
        var sourceText = new SourceText("print skip_while([1, 2, 3, 1], fn(x: Int) => x < 3)", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        Assert.Equal("[3, 1]", result.Output.Trim());
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

    [Fact]
    public void Zip_with_combines_items_until_shortest_list_ends()
    {
        var sourceText = new SourceText("print zip_with([1, 2, 3], [10, 20], fn(x: Int, y: Int) => x + y)", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        Assert.Equal("[11, 22]", result.Output.Trim());
        Assert.Empty(result.Diagnostics);
    }
}
