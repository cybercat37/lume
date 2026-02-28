using Axom.Compiler.Parsing;
using Axom.Compiler.Syntax;
using Axom.Compiler.Text;

public class ParserTransactionTests
{
    [Fact]
    public void Transaction_block_parses_as_transaction_statement()
    {
        var source = new SourceText("transaction { print 1 }", "test.axom");
        var tree = SyntaxTree.Parse(source);

        var statement = Assert.IsType<TransactionStatementSyntax>(tree.Root.Statements[0]);
        Assert.Single(statement.Body.Statements);
        Assert.Empty(tree.Diagnostics);
    }
}
