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

    [Fact]
    public void Route_param_int_returns_ok_when_value_is_int()
    {
        var sourceText = new SourceText(@"
print match route_param_int(""id"") {
  Ok(value) -> value
  Error(_) -> -1
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
    public void Route_param_float_returns_error_when_value_is_invalid()
    {
        var sourceText = new SourceText(@"
print route_param_float(""score"")
", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        interpreter.SetRouteParameters(new Dictionary<string, string>
        {
            ["score"] = "abc"
        });
        var result = interpreter.Run(syntaxTree);

        Assert.Equal("Error(route parameter 'score' is not a valid Float)", result.Output.Trim());
        Assert.Empty(result.Diagnostics);
    }
}
