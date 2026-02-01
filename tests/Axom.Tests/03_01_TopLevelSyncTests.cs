using Axom.Compiler.Parsing;
using Axom.Compiler.Syntax;
using Axom.Compiler.Text;

public class TopLevelSyncTests
{
    [Fact]
    public void Parser_synchronizes_on_newline()
    {
        var sourceText = new SourceText(@"
let =
print ""ok""
", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        Assert.Contains(syntaxTree.Root.Statements, statement => statement is PrintStatementSyntax);
        Assert.NotEmpty(syntaxTree.Diagnostics);
    }
}
