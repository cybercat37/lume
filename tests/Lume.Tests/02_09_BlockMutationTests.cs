using Lume.Compiler.Parsing;
using Lume.Compiler.Syntax;
using Lume.Compiler.Text;

public class BlockMutationTests
{
    [Fact]
    public void Block_allows_mutable_declaration_and_assignment()
    {
        var sourceText = new SourceText(@"
{
let mut x = 1
x = 2
}
", "test.lume");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var block = Assert.IsType<BlockStatementSyntax>(syntaxTree.Root.Statements.Single());
        Assert.Equal(2, block.Statements.Count);

        Assert.IsType<VariableDeclarationSyntax>(block.Statements[0]);
        var assignmentStatement = Assert.IsType<ExpressionStatementSyntax>(block.Statements[1]);
        Assert.IsType<AssignmentExpressionSyntax>(assignmentStatement.Expression);

        Assert.Empty(syntaxTree.Diagnostics);
    }
}
