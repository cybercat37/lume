using Axom.Compiler.Interpreting;
using Axom.Compiler.Parsing;
using Axom.Compiler.Text;

public class InterpreterTailRecursionTests
{
    [Fact]
    public void Tail_recursive_sum_runs_without_stack_overflow()
    {
        var sourceText = new SourceText(@"
fn sum(n: Int, acc: Int) -> Int => match n {
  0 -> acc
  _ -> sum(n - 1, acc + n)
}

print sum(5000, 0)
", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        Assert.Equal("12502500", result.Output.Trim());
        Assert.Empty(result.Diagnostics);
    }
}
