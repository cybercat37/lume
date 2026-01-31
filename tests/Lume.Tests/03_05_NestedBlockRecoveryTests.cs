using Lume.Compiler.Parsing;
using Lume.Compiler.Syntax;
using Lume.Compiler.Text;

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
", "test.lume");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        Assert.NotEmpty(syntaxTree.Diagnostics);
        Assert.Contains(syntaxTree.Root.Statements, statement => statement is BlockStatementSyntax);
    }
}
