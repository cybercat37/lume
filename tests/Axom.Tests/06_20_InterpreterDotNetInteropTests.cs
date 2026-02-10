using Axom.Compiler.Interpreting;
using Axom.Compiler.Parsing;
using Axom.Compiler.Text;

public class InterpreterDotNetInteropTests
{
    [Fact]
    public void Dotnet_call_invokes_system_math_max()
    {
        var sourceText = new SourceText("print dotnet.call<Int>(\"System.Math\", \"Max\", 3, 7)", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        Assert.Equal("7", result.Output.Trim());
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Dotnet_try_call_returns_error_for_disallowed_type()
    {
        var sourceText = new SourceText(@"
print match dotnet.try_call<Int>(""System.IO.File"", ""Exists"", ""x"") {
  Ok(v) -> v
  Error(_) -> -1
}
", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        Assert.Equal("-1", result.Output.Trim());
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Dotnet_try_call_returns_error_for_disallowed_method()
    {
        var sourceText = new SourceText(@"
print match dotnet.try_call<Int>(""System.Math"", ""Sin"", 1) {
  Ok(v) -> v
  Error(_) -> -1
}
", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        Assert.Equal("-1", result.Output.Trim());
        Assert.Empty(result.Diagnostics);
    }
}
