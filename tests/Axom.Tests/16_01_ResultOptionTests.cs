using Axom.Compiler.Binding;
using Axom.Compiler.Interpreting;
using Axom.Compiler.Parsing;
using Axom.Compiler.Text;

public class ResultOptionTests
{
    [Fact]
    public void Question_operator_requires_function()
    {
        var sourceText = new SourceText(@"
type Option { Some(Int) None }
let x = Some(1)?
", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.NotEmpty(result.Diagnostics);
    }

    [Fact]
    public void Question_operator_on_option_short_circuits()
    {
        var sourceText = new SourceText(@"
type Option { Some(Int) None }

fn maybe(x: Int) -> Option => match x {
  0 -> None
  _ -> Some(x)
}

fn add_one(x: Int) -> Option {
  let value = maybe(x)?
  return Some(value + 1)
}

print add_one(0)
print add_one(2)
", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        var lines = result.Output.Split('\n');
        Assert.Equal("None", lines[0].Trim());
        Assert.Equal("Some(3)", lines[1].Trim());
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Question_operator_on_result_short_circuits()
    {
        var sourceText = new SourceText(@"
type Result { Ok(Int) Err(String) }

fn maybe(x: Int) -> Result => match x {
  0 -> Err(""bad"")
  _ -> Ok(x)
}

fn add_one(x: Int) -> Result {
  let value = maybe(x)?
  return Ok(value + 1)
}

print add_one(0)
print add_one(2)
", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        var lines = result.Output.Split('\n');
        Assert.Equal("Err(bad)", lines[0].Trim());
        Assert.Equal("Ok(3)", lines[1].Trim());
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Unwrap_reads_payload()
    {
        var sourceText = new SourceText(@"
type Option { Some(Int) None }
print Some(2).unwrap()
", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        Assert.Equal("2", result.Output.Trim());
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Generic_identity_infers_type()
    {
        var sourceText = new SourceText(@"
fn id<T>(x: T) -> T => x
print id(5)
print id(""hi"")
", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        var lines = result.Output.Split('\n');
        Assert.Equal("5", lines[0].Trim());
        Assert.Equal("hi", lines[1].Trim());
        Assert.Empty(result.Diagnostics);
    }
}
