using Axom.Compiler.Parsing;
using Axom.Compiler.Syntax;
using Axom.Compiler.Text;

public class BlockStatementTests
{
    [Fact]
    public void Block_statement_parses_multiple_statements()
    {
        var sourceText = new SourceText("{ print \"a\"; print \"b\"; }", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var block = Assert.IsType<BlockStatementSyntax>(syntaxTree.Root.Statements.Single());

        Assert.Equal(2, block.Statements.Count);
        Assert.Empty(syntaxTree.Diagnostics);
    }

    [Fact]
    public void Block_with_newline_separators_parses()
    {
        var sourceText = new SourceText(@"
{
print ""a""
print ""b""
}
", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var block = Assert.IsType<BlockStatementSyntax>(syntaxTree.Root.Statements.Single());

        Assert.Equal(2, block.Statements.Count);
        Assert.Empty(syntaxTree.Diagnostics);
    }
}
