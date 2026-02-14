using Axom.Compiler.Interpreting;
using Axom.Compiler.Parsing;
using Axom.Compiler.Text;

namespace Axom.Tests;

public class InterpreterRouteParamTests
{
    [Fact]
    public void Route_param_returns_ok_when_value_exists()
    {
        var sourceText = new SourceText(@"
print match route_param(""id"") {
  Ok(value) -> value
  Error(_) -> ""missing""
}
", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        interpreter.SetRouteParameters(new Dictionary<string, string>
        {
            ["id"] = "42"
        });

        var result = interpreter.Run(syntaxTree);

        Assert.Equal("42", result.Output.Trim());
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Route_param_returns_error_when_value_missing()
    {
        var sourceText = new SourceText(@"
print route_param(""id"")
", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        Assert.Equal("Error(route parameter 'id' not found)", result.Output.Trim());
        Assert.Empty(result.Diagnostics);
    }
}
