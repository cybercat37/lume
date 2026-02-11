using Axom.Compiler.Binding;
using Axom.Compiler.Interpreting;
using Axom.Compiler.Parsing;
using Axom.Compiler.Text;

public class PatternMatchExhaustivenessTests
{
    [Fact]
    public void Match_identifier_pattern_binds_value()
    {
        var sourceText = new SourceText(@"
print match 2 {
  x -> x + 1
}
", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        Assert.Equal("3", result.Output.Trim());
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Match_missing_bool_arm_produces_diagnostic()
    {
        var sourceText = new SourceText(@"
let result = match true {
  true -> 1
}
", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.NotEmpty(result.Diagnostics);
    }

    [Fact]
    public void Match_unreachable_arm_after_catch_all_produces_diagnostic()
    {
        var sourceText = new SourceText(@"
let result = match 1 {
  _ -> 1
  2 -> 2
}
", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.NotEmpty(result.Diagnostics);
    }

    [Fact]
    public void Match_duplicate_literal_arm_produces_diagnostic()
    {
        var sourceText = new SourceText(@"
let result = match 1 {
  1 -> 1
  1 -> 2
  _ -> 3
}
", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.NotEmpty(result.Diagnostics);
    }

    [Fact]
    public void Match_relational_pattern_alone_is_non_exhaustive()
    {
        var sourceText = new SourceText(@"
let result = match 1 {
  <= 1 -> 1
}
", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.NotEmpty(result.Diagnostics);
    }
}
