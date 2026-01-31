using Lume.Compiler.Parsing;
using Lume.Compiler.Syntax;
using Lume.Compiler.Text;

public class ParserPrintTests
{
    [Fact]
    public void Print_statement_builds_ast()
    {
        var sourceText = new SourceText("print \"hello\"", "test.lume");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var statement = Assert.IsType<PrintStatementSyntax>(syntaxTree.Root.Statements.Single());
        var literal = Assert.IsType<LiteralExpressionSyntax>(statement.Expression);

        Assert.Equal("hello", literal.LiteralToken.Value);
        Assert.Empty(syntaxTree.Diagnostics);
    }

    [Fact]
    public void Empty_source_produces_diagnostic()
    {
        var sourceText = new SourceText(string.Empty, "test.lume");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        Assert.NotEmpty(syntaxTree.Diagnostics);
    }
}
