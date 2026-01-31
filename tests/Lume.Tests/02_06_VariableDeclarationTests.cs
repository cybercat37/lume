using Lume.Compiler.Parsing;
using Lume.Compiler.Syntax;
using Lume.Compiler.Text;

public class VariableDeclarationTests
{
    [Fact]
    public void Let_declaration_parses()
    {
        var sourceText = new SourceText("let x = 1", "test.lume");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var statement = Assert.IsType<VariableDeclarationSyntax>(syntaxTree.Root.Statements.Single());

        Assert.Equal("x", statement.IdentifierToken.Text);
        Assert.Empty(syntaxTree.Diagnostics);
    }

    [Fact]
    public void Let_mut_declaration_parses()
    {
        var sourceText = new SourceText("let mut x = 1", "test.lume");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var statement = Assert.IsType<VariableDeclarationSyntax>(syntaxTree.Root.Statements.Single());

        Assert.Equal("x", statement.IdentifierToken.Text);
        Assert.Empty(syntaxTree.Diagnostics);
    }
}
