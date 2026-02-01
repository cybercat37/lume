using System;
using Lume.Compiler.Binding;
using Lume.Compiler.Parsing;
using Lume.Compiler.Text;

public class FunctionBindingTests
{
    [Fact]
    public void Function_call_with_correct_arity_binds()
    {
        var sourceText = new SourceText(@"
fn add(x: Int, y: Int) => x + y
let z = add(1, 2)
", "test.lume");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Function_call_with_wrong_arity_produces_diagnostic()
    {
        var sourceText = new SourceText(@"
fn add(x: Int, y: Int) => x + y
add(1)
", "test.lume");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.NotEmpty(result.Diagnostics);
        Assert.Contains("expects 2", result.Diagnostics[0].Message);
    }

    [Fact]
    public void Return_type_mismatch_produces_diagnostic()
    {
        var sourceText = new SourceText(@"
fn bad(x: Int) -> Int {
  return ""oops""
}
", "test.lume");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.NotEmpty(result.Diagnostics);
        Assert.Contains("return", result.Diagnostics[0].Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Inconsistent_return_types_produce_diagnostic()
    {
        var sourceText = new SourceText(@"
fn bad(x: Int) {
  return 1
  return ""oops""
}
", "test.lume");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.NotEmpty(result.Diagnostics);
        Assert.Contains("inconsistent return", result.Diagnostics[0].Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Return_outside_function_produces_diagnostic()
    {
        var sourceText = new SourceText("return 1", "test.lume");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.NotEmpty(result.Diagnostics);
        Assert.Contains("return", result.Diagnostics[0].Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Recursive_function_bind_is_allowed()
    {
        var sourceText = new SourceText(@"
fn self(n: Int) -> Int { self(n) }
", "test.lume");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Capturing_mutable_variable_in_lambda_produces_diagnostic()
    {
        var sourceText = new SourceText(@"
let mut x = 1
let f = fn(y: Int) => x + y
", "test.lume");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.NotEmpty(result.Diagnostics);
        Assert.Contains("mutable", result.Diagnostics[0].Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Input_call_binds_without_diagnostic()
    {
        var sourceText = new SourceText("print input()", "test.lume");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.Empty(result.Diagnostics);
    }
}
