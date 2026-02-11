using Axom.Compiler.Lexing;
using Axom.Compiler.Parsing;
using Axom.Compiler.Syntax;
using Axom.Compiler.Text;

public class PipelineParserTests
{
    [Fact]
    public void Pipeline_expression_parses()
    {
        var sourceText = new SourceText("print 1 |> abs", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var statement = Assert.IsType<PrintStatementSyntax>(syntaxTree.Root.Statements.Single());
        var pipeline = Assert.IsType<PipelineExpressionSyntax>(statement.Expression);

        Assert.Equal(TokenKind.PipeGreater, pipeline.OperatorToken.Kind);
        Assert.IsType<LiteralExpressionSyntax>(pipeline.Left);
        Assert.IsType<NameExpressionSyntax>(pipeline.Right);
    }

    [Fact]
    public void Pipeline_with_call_rhs_parses()
    {
        var sourceText = new SourceText("print 1 |> max(2)", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var statement = Assert.IsType<PrintStatementSyntax>(syntaxTree.Root.Statements.Single());
        var pipeline = Assert.IsType<PipelineExpressionSyntax>(statement.Expression);

        Assert.IsType<CallExpressionSyntax>(pipeline.Right);
    }
}
