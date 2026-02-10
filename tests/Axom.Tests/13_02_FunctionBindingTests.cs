using System;
using Axom.Compiler.Binding;
using Axom.Compiler.Parsing;
using Axom.Compiler.Text;

public class FunctionBindingTests
{
    [Fact]
    public void Function_call_with_correct_arity_binds()
    {
        var sourceText = new SourceText(@"
fn add(x: Int, y: Int) => x + y
let z = add(1, 2)
", "test.axom");
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
", "test.axom");
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
", "test.axom");
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
", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.NotEmpty(result.Diagnostics);
        Assert.Contains("inconsistent return", result.Diagnostics[0].Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Return_outside_function_produces_diagnostic()
    {
        var sourceText = new SourceText("return 1", "test.axom");
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
", "test.axom");
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
", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.NotEmpty(result.Diagnostics);
        Assert.Contains("mutable", result.Diagnostics[0].Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Input_call_binds_without_diagnostic()
    {
        var sourceText = new SourceText("print input()", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Spawn_outside_scope_produces_diagnostic()
    {
        var sourceText = new SourceText("let task = spawn { 1 }", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.NotEmpty(result.Diagnostics);
        Assert.Contains("inside a scope", result.Diagnostics[0].Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Spawn_inside_scope_binds_without_diagnostic()
    {
        var sourceText = new SourceText(@"
scope {
  let task = spawn { 1 }
  print task.join()
}
", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Channel_send_recv_bind_without_diagnostic()
    {
        var sourceText = new SourceText(@"
scope {
  let (tx, rx) = channel<Int>()
  tx.send(1)
  print rx.recv()
}
", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Channel_send_type_mismatch_produces_diagnostic()
    {
        var sourceText = new SourceText(@"
scope {
  let (tx, rx) = channel<Int>()
  tx.send(""x"")
}
", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.NotEmpty(result.Diagnostics);
        Assert.Contains("send() expects", result.Diagnostics[0].Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Recv_result_must_be_handled()
    {
        var sourceText = new SourceText(@"
scope {
  let (tx, rx) = channel<Int>()
  rx.recv()
}
", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.NotEmpty(result.Diagnostics);
        Assert.Contains(result.Diagnostics, diagnostic =>
            diagnostic.Message.Contains("must be handled", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Recv_result_can_be_handled_with_match()
    {
        var sourceText = new SourceText(@"
scope {
  let (tx, rx) = channel<Int>()
  let value = match rx.recv() {
    Ok(x) -> x
    Error(_) -> 0
  }
  print value
}
", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Channel_capacity_must_be_positive_integer_literal()
    {
        var sourceText = new SourceText(@"
scope {
  let (tx, rx) = channel<Int>(0)
}
", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.NotEmpty(result.Diagnostics);
        Assert.Contains(result.Diagnostics, diagnostic =>
            diagnostic.Message.Contains("greater than zero", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Channel_capacity_non_literal_produces_diagnostic()
    {
        var sourceText = new SourceText(@"
scope {
  let size = 4
  let (tx, rx) = channel<Int>(size)
}
", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.NotEmpty(result.Diagnostics);
        Assert.Contains(result.Diagnostics, diagnostic =>
            diagnostic.Message.Contains("integer literal", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Returning_channel_endpoint_from_nested_scope_produces_diagnostic()
    {
        var sourceText = new SourceText(@"
fn leak() {
  scope {
    let (tx, rx) = channel<Int>()
    return tx
  }
}
", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.NotEmpty(result.Diagnostics);
        Assert.Contains(result.Diagnostics, diagnostic =>
            diagnostic.Message.Contains("deeper scope", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Assigning_inner_channel_endpoint_to_outer_variable_produces_diagnostic()
    {
        var sourceText = new SourceText(@"
scope {
  let (tx, rx) = channel<Int>()
  let mut sink = tx
  {
    let (innerTx, innerRx) = channel<Int>()
    sink = innerTx
  }
}
", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.NotEmpty(result.Diagnostics);
        Assert.Contains(result.Diagnostics, diagnostic =>
            diagnostic.Message.Contains("escapes", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Dotnet_call_binds_without_diagnostic()
    {
        var sourceText = new SourceText("let x = dotnet.call<Int>(\"System.Math\", \"Max\", 3, 7)", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Dotnet_call_with_unsupported_argument_type_produces_diagnostic()
    {
        var sourceText = new SourceText("let x = dotnet.call<Int>(\"System.Math\", \"Max\", [1], 7)", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.NotEmpty(result.Diagnostics);
        Assert.Contains(result.Diagnostics, diagnostic =>
            diagnostic.Message.Contains("arguments must be Int, Float, Bool, or String", StringComparison.OrdinalIgnoreCase));
    }

}
