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
    public void Dotnet_try_call_with_disallowed_literal_type_reports_diagnostic()
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

        Assert.Contains(result.Diagnostics, diagnostic =>
            diagnostic.Message.Contains("Allowed types", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Dotnet_try_call_with_disallowed_literal_method_reports_diagnostic()
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

        Assert.Contains(result.Diagnostics, diagnostic =>
            diagnostic.Message.Contains("Allowed methods", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Dotnet_call_invokes_system_string_instance_method()
    {
        var sourceText = new SourceText("print dotnet.call<Bool>(\"System.String\", \"Contains\", \"axom\", \"xo\")", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        Assert.Equal("true", result.Output.Trim());
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Dotnet_call_invokes_system_convert_method()
    {
        var sourceText = new SourceText("print dotnet.call<Int>(\"System.Convert\", \"ToInt32\", \"42\")", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        Assert.Equal("42", result.Output.Trim());
        Assert.Empty(result.Diagnostics);
    }
}
