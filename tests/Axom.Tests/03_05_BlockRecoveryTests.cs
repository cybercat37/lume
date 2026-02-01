using Axom.Compiler.Parsing;
using Axom.Compiler.Syntax;
using Axom.Compiler.Text;

public class BlockRecoveryTests
{
    [Fact]
    public void Missing_close_brace_produces_diagnostic_and_recovers()
    {
        var sourceText = new SourceText(@"
{
print ""a""
print ""b""
print ""ok""
", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        Assert.NotEmpty(syntaxTree.Diagnostics);
        Assert.Contains(syntaxTree.Root.Statements, statement => statement is BlockStatementSyntax);
    }
}
