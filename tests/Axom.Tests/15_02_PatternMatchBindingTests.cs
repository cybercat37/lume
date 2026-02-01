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
}
