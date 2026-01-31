using Lume.Compiler.Parsing;
using Lume.Compiler.Syntax;
using Lume.Compiler.Text;

public class AssignmentRecoveryTests
{
    [Fact]
    public void Missing_assignment_rhs_produces_diagnostic()
    {
        var sourceText = new SourceText(@"
x =
print ""ok""
", "test.lume");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        Assert.NotEmpty(syntaxTree.Diagnostics);
        Assert.Contains(syntaxTree.Root.Statements, statement => statement is PrintStatementSyntax);
    }
}
