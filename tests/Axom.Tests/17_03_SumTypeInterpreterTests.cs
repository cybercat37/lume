using Axom.Compiler.Interpreting;
using Axom.Compiler.Parsing;
using Axom.Compiler.Text;

public class SumTypeInterpreterTests
{
    [Fact]
    public void Sum_type_match_binds_payload()
    {
        var sourceText = new SourceText(@"
type Result { Ok(Int) Error(String) }
print match Ok(2) {
  Ok(x) -> x
  Error(_) -> 0
}
", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        Assert.Equal("2", result.Output.Trim());
        Assert.Empty(result.Diagnostics);
    }
}
