using Axom.Compiler.Parsing;
using Axom.Compiler.Syntax;
using Axom.Compiler.Text;

public class SemicolonSyncTests
{
    [Fact]
    public void Parser_synchronizes_on_semicolon()
    {
        var sourceText = new SourceText("let = 1; print \"ok\"", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        Assert.Contains(syntaxTree.Root.Statements, statement => statement is PrintStatementSyntax);
        Assert.NotEmpty(syntaxTree.Diagnostics);
    }
}
