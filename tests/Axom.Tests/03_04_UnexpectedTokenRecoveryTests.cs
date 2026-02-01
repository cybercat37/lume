using Axom.Compiler.Parsing;
using Axom.Compiler.Syntax;
using Axom.Compiler.Text;

public class UnexpectedTokenRecoveryTests
{
    [Fact]
    public void Unexpected_token_does_not_stop_parsing()
    {
        var sourceText = new SourceText(@"
@
print ""ok""
", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        Assert.NotEmpty(syntaxTree.Diagnostics);
        Assert.Contains(syntaxTree.Root.Statements, statement => statement is PrintStatementSyntax);
    }
}
