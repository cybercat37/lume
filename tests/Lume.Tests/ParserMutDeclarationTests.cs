using Lume.Compiler.Parsing;
using Lume.Compiler.Syntax;
using Lume.Compiler.Text;

public class ParserMutDeclarationTests
{
    [Fact]
    public void Let_mut_declaration_parses()
    {
        var sourceText = new SourceText("let mut x = 1", "test.lume");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var statement = Assert.IsType<VariableDeclarationSyntax>(syntaxTree.Root.Statement);

        Assert.Equal("x", statement.IdentifierToken.Text);
        Assert.Empty(syntaxTree.Diagnostics);
    }
}
