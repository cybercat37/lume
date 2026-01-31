using Lume.Compiler.Parsing;
using Lume.Compiler.Syntax;
using Lume.Compiler.Text;

public class ParserBlockNewlineTests
{
    [Fact]
    public void Block_with_newline_separators_parses()
    {
        var sourceText = new SourceText(@"
{
print ""a""
print ""b""
}
", "test.lume");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var block = Assert.IsType<BlockStatementSyntax>(syntaxTree.Root.Statement);

        Assert.Equal(2, block.Statements.Count);
        Assert.Empty(syntaxTree.Diagnostics);
    }
}
