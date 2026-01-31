using Lume.Compiler.Parsing;
using Lume.Compiler.Syntax;
using Lume.Compiler.Text;

public class MutDeclarationRecoveryTests
{
    [Fact]
    public void Missing_identifier_after_mut_produces_diagnostic()
    {
        var sourceText = new SourceText(@"
let mut = 1
print ""ok""
", "test.lume");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        Assert.NotEmpty(syntaxTree.Diagnostics);
        Assert.Contains(syntaxTree.Root.Statements, statement => statement is PrintStatementSyntax);
    }
}
