using Axom.Compiler.Interpreting;
using Axom.Compiler.Parsing;
using Axom.Compiler.Text;

public class InterpreterListCombinatorTests
{
    [Fact]
    public void Map_transforms_list_values()
    {
        var sourceText = new SourceText("print map([1, 2, 3], fn(x: Int) => x * 2)", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        Assert.Equal("[2, 4, 6]", result.Output.Trim());
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Filter_keeps_matching_values()
    {
        var sourceText = new SourceText("print filter([1, 2, 3, 4], fn(x: Int) => x > 2)", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        Assert.Equal("[3, 4]", result.Output.Trim());
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Fold_reduces_list_values()
    {
        var sourceText = new SourceText("print fold([1, 2, 3, 4], 0, fn(acc: Int, x: Int) => acc + x)", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        Assert.Equal("10", result.Output.Trim());
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Each_runs_action_for_every_element()
    {
        var sourceText = new SourceText("each([1, 2, 3], fn(x: Int) { print x })", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        Assert.Equal("1\n2\n3", result.Output.Trim());
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Pipeline_composes_with_map_and_fold()
    {
        var sourceText = new SourceText("print [1, 2, 3] |> map(fn(x: Int) => x * 2) |> fold(0, fn(acc: Int, x: Int) => acc + x)", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        Assert.Equal("12", result.Output.Trim());
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Pipeline_sum_of_squares_with_shorthand_lambda()
    {
        var sourceText = new SourceText("print [1, 2, 3, 4] |> map(x -> x * x) |> fold(0, fn(acc: Int, x: Int) => acc + x)", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        Assert.Equal("30", result.Output.Trim());
        Assert.Empty(result.Diagnostics);
    }
}
