using Axom.Compiler.Diagnostics;
using Axom.Compiler.Interpreting;
using Axom.Compiler.Parsing;
using Axom.Compiler.Text;

public class InterpreterClearBuiltinTests
{
    [Fact]
    public void Clear_builtin_resets_visible_output()
    {
        var sourceText = new SourceText("print 1\nclear()\nprint 2", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        Assert.Equal("2", result.Output.Trim());
        Assert.Empty(result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
    }
}
