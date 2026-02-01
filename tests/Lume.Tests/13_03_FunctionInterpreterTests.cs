using Lume.Compiler.Interpreting;
using Lume.Compiler.Parsing;
using Lume.Compiler.Text;

public class FunctionInterpreterTests
{
    [Fact]
    public void Function_return_implicit_outputs_value()
    {
        var sourceText = new SourceText(@"
fn add(x: Int, y: Int) {
  x + y
}
print add(1, 2)
", "test.lume");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        Assert.Equal("3", result.Output.Trim());
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Return_statement_exits_function_early()
    {
        var sourceText = new SourceText(@"
fn foo(x: Int) -> Int {
  return x
  x + 1
}
print foo(5)
", "test.lume");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        Assert.Equal("5", result.Output.Trim());
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Lambda_value_can_be_called()
    {
        var sourceText = new SourceText(@"
let f = fn(x: Int) => x + 1
print f(2)
", "test.lume");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        Assert.Equal("3", result.Output.Trim());
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Closure_captures_outer_value_by_value()
    {
        var sourceText = new SourceText(@"
let x = 10
let f = fn(y: Int) => x + y
{
  let x = 20
  print f(1)
}
", "test.lume");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        Assert.Equal("11", result.Output.Trim());
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Input_can_be_called_as_function()
    {
        var sourceText = new SourceText("print input()", "test.lume");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        interpreter.SetInput("hi");
        var result = interpreter.Run(syntaxTree);

        Assert.Equal("hi", result.Output.Trim());
        Assert.Empty(result.Diagnostics);
    }
}
