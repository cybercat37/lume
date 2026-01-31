using Lume.Compiler.Parsing;
using Lume.Compiler.Syntax;
using Lume.Compiler.Text;

public class UnexpectedTokenRecoveryTests
{
    [Fact]
    public void Unexpected_token_does_not_stop_parsing()
    {
        var sourceText = new SourceText(@"
@
print ""ok""
", "test.lume");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        Assert.NotEmpty(syntaxTree.Diagnostics);
        Assert.Contains(syntaxTree.Root.Statements, statement => statement is PrintStatementSyntax);
    }
}
