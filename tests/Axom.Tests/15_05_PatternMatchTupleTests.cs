using Axom.Compiler.Binding;
using Axom.Compiler.Interpreting;
using Axom.Compiler.Parsing;
using Axom.Compiler.Text;

public class PatternMatchTupleTests
{
    [Fact]
    public void Match_tuple_pattern_parses()
    {
        var sourceText = new SourceText(@"
print match (1, 2) {
  (1, 2) -> 1
  _ -> 0
}
", "test.axom");

        var syntaxTree = SyntaxTree.Parse(sourceText);

        Assert.Empty(syntaxTree.Diagnostics);
    }

    [Fact]
    public void Match_tuple_identifier_pattern_binds_values()
    {
        var sourceText = new SourceText(@"
print match (1, 2) {
  (a, b) -> a + b
}
", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        Assert.Equal("3", result.Output.Trim());
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Match_nested_tuple_pattern_binds_values()
    {
        var sourceText = new SourceText(@"
print match (1, (2, 3)) {
  (1, (x, y)) -> x + y
  _ -> 0
}
", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        Assert.Equal("5", result.Output.Trim());
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Match_tuple_pattern_arity_mismatch_produces_diagnostic()
    {
        var sourceText = new SourceText(@"
let result = match (1, 2) {
  (x, y, z) -> x
}
", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.NotEmpty(result.Diagnostics);
    }

    [Fact]
    public void Match_tuple_catch_all_is_exhaustive()
    {
        var sourceText = new SourceText(@"
let result = match (1, 2) {
  (x, y) -> x + y
}
", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Match_tuple_unreachable_after_catch_all_produces_diagnostic()
    {
        var sourceText = new SourceText(@"
let result = match (1, 2) {
  (x, y) -> 1
  (1, 2) -> 2
}
", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.NotEmpty(result.Diagnostics);
    }
}
