using Lume.Compiler.Parsing;
using Lume.Compiler.Text;

public class StatementStartTests
{
    [Fact]
    public void Non_statement_start_produces_diagnostic()
    {
        var sourceText = new SourceText(")", "test.lume");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        Assert.Empty(syntaxTree.Root.Statements);
        Assert.NotEmpty(syntaxTree.Diagnostics);
    }
}
