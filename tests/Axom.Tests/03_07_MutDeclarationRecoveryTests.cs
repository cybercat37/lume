using Axom.Compiler.Parsing;
using Axom.Compiler.Syntax;
using Axom.Compiler.Text;

public class MutDeclarationRecoveryTests
{
    [Fact]
    public void Missing_identifier_after_mut_produces_diagnostic()
    {
        var sourceText = new SourceText(@"
let mut = 1
print ""ok""
", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        Assert.NotEmpty(syntaxTree.Diagnostics);
        Assert.Contains(syntaxTree.Root.Statements, statement => statement is PrintStatementSyntax);
    }
}
