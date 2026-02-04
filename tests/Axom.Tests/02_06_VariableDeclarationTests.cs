using Axom.Compiler.Parsing;
using Axom.Compiler.Syntax;
using Axom.Compiler.Text;

public class VariableDeclarationTests
{
    [Fact]
    public void Let_declaration_parses()
    {
        var sourceText = new SourceText("let x = 1", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var statement = Assert.IsType<VariableDeclarationSyntax>(syntaxTree.Root.Statements.Single());

        var identifier = Assert.IsType<IdentifierPatternSyntax>(statement.Pattern);
        Assert.Equal("x", identifier.IdentifierToken.Text);
        Assert.Empty(syntaxTree.Diagnostics);
    }

    [Fact]
    public void Let_mut_declaration_parses()
    {
        var sourceText = new SourceText("let mut x = 1", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var statement = Assert.IsType<VariableDeclarationSyntax>(syntaxTree.Root.Statements.Single());

        var identifier = Assert.IsType<IdentifierPatternSyntax>(statement.Pattern);
        Assert.Equal("x", identifier.IdentifierToken.Text);
        Assert.Empty(syntaxTree.Diagnostics);
    }

    [Fact]
    public void Let_tuple_declaration_parses()
    {
        var sourceText = new SourceText("let (x, y) = (1, 2)", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var statement = Assert.IsType<VariableDeclarationSyntax>(syntaxTree.Root.Statements.Single());

        var tuplePattern = Assert.IsType<TuplePatternSyntax>(statement.Pattern);
        Assert.Equal(2, tuplePattern.Elements.Count);
        Assert.Empty(syntaxTree.Diagnostics);
    }
}
