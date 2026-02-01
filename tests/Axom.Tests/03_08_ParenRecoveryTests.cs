using Axom.Compiler.Parsing;
using Axom.Compiler.Syntax;
using Axom.Compiler.Text;

public class ParenRecoveryTests
{
    [Fact]
    public void Missing_close_paren_produces_diagnostic_and_recovers()
    {
        var sourceText = new SourceText(@"
print (1 + 2
print ""ok""
", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        Assert.NotEmpty(syntaxTree.Diagnostics);
        Assert.Contains(syntaxTree.Root.Statements, statement => statement is PrintStatementSyntax);
    }
}
