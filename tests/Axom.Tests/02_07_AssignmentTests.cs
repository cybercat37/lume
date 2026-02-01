using Axom.Compiler.Parsing;
using Axom.Compiler.Syntax;
using Axom.Compiler.Text;

public class AssignmentTests
{
    [Fact]
    public void Assignment_expression_parses()
    {
        var sourceText = new SourceText("print x = 3", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var statement = Assert.IsType<PrintStatementSyntax>(syntaxTree.Root.Statements.Single());
        var assignment = Assert.IsType<AssignmentExpressionSyntax>(statement.Expression);

        Assert.Equal("x", assignment.IdentifierToken.Text);
        Assert.Empty(syntaxTree.Diagnostics);
    }

    [Fact]
    public void Assignment_statement_parses()
    {
        var sourceText = new SourceText("x = 3", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var statement = Assert.IsType<ExpressionStatementSyntax>(syntaxTree.Root.Statements.Single());
        var assignment = Assert.IsType<AssignmentExpressionSyntax>(statement.Expression);

        Assert.Equal("x", assignment.IdentifierToken.Text);
        Assert.Empty(syntaxTree.Diagnostics);
    }
}
