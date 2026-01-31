using Lume.Compiler.Parsing;
using Lume.Compiler.Syntax;
using Lume.Compiler.Text;

public class ParserAssignmentTests
{
    [Fact]
    public void Assignment_expression_parses()
    {
        var sourceText = new SourceText("print x = 3", "test.lume");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var statement = Assert.IsType<PrintStatementSyntax>(syntaxTree.Root.Statement);
        var assignment = Assert.IsType<AssignmentExpressionSyntax>(statement.Expression);

        Assert.Equal("x", assignment.IdentifierToken.Text);
        Assert.Empty(syntaxTree.Diagnostics);
    }
}
