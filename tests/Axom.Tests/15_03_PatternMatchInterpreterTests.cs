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
}
