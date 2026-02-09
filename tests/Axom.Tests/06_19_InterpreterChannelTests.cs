using Axom.Compiler.Interpreting;
using Axom.Compiler.Parsing;
using Axom.Compiler.Text;

public class InterpreterChannelTests
{
    [Fact]
    public void Channel_send_recv_between_tasks_prints_sum()
    {
        var sourceText = new SourceText(@"
scope {
  let (tx, rx) = channel<Int>()

  let sender = spawn {
    tx.send(1)
    tx.send(2)
  }

  let worker = spawn {
    let a = rx.recv().unwrap()
    let b = rx.recv().unwrap()
    print a + b
  }

  sender.join()
  worker.join()
}
", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        Assert.Equal("3", result.Output.Trim());
        Assert.Empty(result.Diagnostics);
    }
}
