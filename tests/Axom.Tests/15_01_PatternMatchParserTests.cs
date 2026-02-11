using Axom.Compiler.Parsing;
using Axom.Compiler.Text;

public class PatternMatchParserTests
{
    [Fact]
    public void Match_expression_parses()
    {
        var sourceText = new SourceText(@"
let result = match 2 {
  1 -> ""one""
  2 -> ""two""
  _ -> ""many""
}
", "test.axom");

        var syntaxTree = SyntaxTree.Parse(sourceText);

        Assert.Empty(syntaxTree.Diagnostics);
    }

    [Fact]
    public void Match_missing_arrow_produces_diagnostic()
    {
        var sourceText = new SourceText(@"
let result = match 1 {
  0 ""zero""
}
", "test.axom");

        var syntaxTree = SyntaxTree.Parse(sourceText);

        Assert.NotEmpty(syntaxTree.Diagnostics);
    }

    [Fact]
    public void Match_record_pattern_parses()
    {
        var sourceText = new SourceText(@"
type User { name: String, age: Int }
let user = User { name: ""Ada"", age: 36 }
let result = match user {
  User { name: n, age: a } -> n
}
", "test.axom");

        var syntaxTree = SyntaxTree.Parse(sourceText);

        Assert.Empty(syntaxTree.Diagnostics);
    }

    [Fact]
    public void Match_guard_parses()
    {
        var sourceText = new SourceText(@"
let result = match 2 {
  2 when true -> ""two""
  _ -> ""other""
}
", "test.axom");

        var syntaxTree = SyntaxTree.Parse(sourceText);

        Assert.Empty(syntaxTree.Diagnostics);
    }

    [Fact]
    public void Match_relational_pattern_parses()
    {
        var sourceText = new SourceText(@"
let result = match 2 {
  <= 1 -> ""small""
  > 1 -> ""big""
}
", "test.axom");

        var syntaxTree = SyntaxTree.Parse(sourceText);

        Assert.Empty(syntaxTree.Diagnostics);
    }
}
