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

    [Fact]
    public void Match_list_rest_only_missing_empty_list_is_non_exhaustive()
    {
        var sourceText = new SourceText(@"
let result = match [1, 2, 3] {
  [head, ...tail] -> head
}
", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.NotEmpty(result.Diagnostics.Where(d => d.Message.Contains("Non-exhaustive", StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public void Match_list_empty_and_rest_is_exhaustive()
    {
        var sourceText = new SourceText(@"
let result = match [1, 2, 3] {
  [] -> 0
  [head, ...tail] -> head
}
", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.Empty(result.Diagnostics.Where(d => d.Message.Contains("Non-exhaustive", StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public void Match_more_specific_list_arm_after_broader_rest_arm_is_unreachable()
    {
        var sourceText = new SourceText(@"
let result = match [1, 2, 3] {
  [first, ...rest] -> first
  [x, y, ...z] -> y
  _ -> 0
}
", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.NotEmpty(result.Diagnostics.Where(d => d.Message.Contains("Unreachable", StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public void Match_list_literal_specific_arm_after_general_arm_is_unreachable()
    {
        var sourceText = new SourceText(@"
let result = match [1, 2, 3] {
  [x, ...rest] -> x
  [1, ...rest] -> 1
  _ -> 0
}
", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.NotEmpty(result.Diagnostics.Where(d => d.Message.Contains("Unreachable", StringComparison.OrdinalIgnoreCase)));
    }
}
