using Axom.Compiler.Parsing;
using Axom.Compiler.Syntax;
using Axom.Compiler.Text;

public class ShorthandLambdaParserTests
{
    [Fact]
    public void Shorthand_lambda_in_call_parses()
    {
        var sourceText = new SourceText("print map([1, 2, 3], x -> x * 2)", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var print = Assert.IsType<PrintStatementSyntax>(syntaxTree.Root.Statements.Single());
        var call = Assert.IsType<CallExpressionSyntax>(print.Expression);

        Assert.IsType<ShorthandLambdaExpressionSyntax>(call.Arguments[1]);
        Assert.Empty(syntaxTree.Diagnostics);
    }
}
