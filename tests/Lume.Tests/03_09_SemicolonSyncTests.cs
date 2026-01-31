using Lume.Compiler.Parsing;
using Lume.Compiler.Syntax;
using Lume.Compiler.Text;

public class SemicolonSyncTests
{
    [Fact]
    public void Parser_synchronizes_on_semicolon()
    {
        var sourceText = new SourceText("let = 1; print \"ok\"", "test.lume");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        Assert.Contains(syntaxTree.Root.Statements, statement => statement is PrintStatementSyntax);
        Assert.NotEmpty(syntaxTree.Diagnostics);
    }
}
