using Axom.Compiler.Interpreting;
using Axom.Compiler.Parsing;
using Axom.Compiler.Text;

public class RecordInterpreterTests
{
    [Fact]
    public void Record_field_access_returns_value()
    {
        var sourceText = new SourceText(@"
type User { name: String, age: Int }
let user = User { name: ""Ada"", age: 36 }
print user.age
", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        Assert.Equal("36", result.Output.Trim());
        Assert.Empty(result.Diagnostics);
    }
}
