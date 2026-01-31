using Lume.Compiler.Parsing;
using Lume.Compiler.Syntax;
using Lume.Compiler.Text;

public class TopLevelStatementsTests
{
    [Fact]
    public void Multiple_top_level_statements_parse()
    {
        var sourceText = new SourceText(@"
print ""a""
print ""b""
", "test.lume");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        Assert.Equal(2, syntaxTree.Root.Statements.Count);
        Assert.All(syntaxTree.Root.Statements, statement => Assert.IsType<PrintStatementSyntax>(statement));
        Assert.Empty(syntaxTree.Diagnostics);
    }
}
