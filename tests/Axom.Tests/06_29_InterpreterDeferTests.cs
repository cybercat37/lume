using Axom.Compiler.Interpreting;
using Axom.Compiler.Parsing;
using Axom.Compiler.Text;

public class InterpreterDeferTests
{
    [Fact]
    public void Defer_runs_on_early_return()
    {
        var sourceText = new SourceText(@"
fn work() -> Int {
  defer {
    print ""cleanup""
  }
  return 7
}

print work()
", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        Assert.Equal("cleanup\n7", result.Output.Trim());
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Multiple_defers_run_in_lifo_order()
    {
        var sourceText = new SourceText(@"
fn work() -> Int {
  defer {
    print 1
  }
  defer {
    print 2
  }
  return 3
}

print work()
", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        Assert.Equal("2\n1\n3", result.Output.Trim());
        Assert.Empty(result.Diagnostics);
    }
}
