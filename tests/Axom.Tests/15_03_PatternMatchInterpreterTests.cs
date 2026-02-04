using Axom.Compiler.Interpreting;
using Axom.Compiler.Parsing;
using Axom.Compiler.Text;

public class PatternMatchInterpreterTests
{
    [Fact]
    public void Match_expression_selects_first_matching_arm()
    {
        var sourceText = new SourceText(@"
print match 2 {
  1 -> ""one""
  2 -> ""two""
  _ -> ""many""
}
", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        Assert.Equal("two", result.Output.Trim());
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Match_record_pattern_selects_field()
    {
        var sourceText = new SourceText(@"
type User { name: String, age: Int }
let user = User { name: ""Ada"", age: 36 }
print match user {
  User { name: n, age: a } -> n
}
", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        Assert.Equal("Ada", result.Output.Trim());
        Assert.Empty(result.Diagnostics);
    }
}
