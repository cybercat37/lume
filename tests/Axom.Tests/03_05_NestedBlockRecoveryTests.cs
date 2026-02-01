using Axom.Compiler.Parsing;
using Axom.Compiler.Syntax;
using Axom.Compiler.Text;

public class NestedBlockRecoveryTests
{
    [Fact]
    public void Missing_inner_close_brace_recovers()
    {
        var sourceText = new SourceText(@"
{
{
print ""inner""
print ""outer""
}
print ""after""
", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        Assert.NotEmpty(syntaxTree.Diagnostics);
        Assert.Contains(syntaxTree.Root.Statements, statement => statement is BlockStatementSyntax);
    }
}
