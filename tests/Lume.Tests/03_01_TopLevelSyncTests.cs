using Lume.Compiler.Parsing;
using Lume.Compiler.Syntax;
using Lume.Compiler.Text;

public class TopLevelSyncTests
{
    [Fact]
    public void Parser_synchronizes_on_newline()
    {
        var sourceText = new SourceText(@"
let =
print ""ok""
", "test.lume");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        Assert.Contains(syntaxTree.Root.Statements, statement => statement is PrintStatementSyntax);
        Assert.NotEmpty(syntaxTree.Diagnostics);
    }
}
