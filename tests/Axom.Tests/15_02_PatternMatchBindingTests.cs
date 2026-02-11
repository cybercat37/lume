using Axom.Compiler.Binding;
using Axom.Compiler.Parsing;
using Axom.Compiler.Text;

public class PatternMatchBindingTests
{
    [Fact]
    public void Match_arms_with_consistent_types_bind()
    {
        var sourceText = new SourceText(@"
let result = match 2 {
  1 -> 10
  2 -> 20
  _ -> 30
}
", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Match_arms_with_inconsistent_types_produce_diagnostic()
    {
        var sourceText = new SourceText(@"
let result = match true {
  true -> 1
  false -> ""no""
}
", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.NotEmpty(result.Diagnostics);
    }

    [Fact]
    public void Match_record_pattern_binds()
    {
        var sourceText = new SourceText(@"
type User { name: String, age: Int }
let user = User { name: ""Ada"", age: 36 }
let result = match user {
  User { name: n, age: a } -> n
}
", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Match_guard_requires_bool()
    {
        var sourceText = new SourceText(@"
let result = match 2 {
  2 if 123 -> ""two""
  _ -> ""other""
}
", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.NotEmpty(result.Diagnostics);
    }

    [Fact]
    public void Match_relational_pattern_binds()
    {
        var sourceText = new SourceText(@"
let result = match 2 {
  <= 1 -> ""small""
  > 1 -> ""big""
  _ -> ""other""
}
", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Match_relational_pattern_type_mismatch_produces_diagnostic()
    {
        var sourceText = new SourceText(@"
let result = match ""x"" {
  <= 1 -> ""small""
  _ -> ""other""
}
", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.NotEmpty(result.Diagnostics);
    }
}
