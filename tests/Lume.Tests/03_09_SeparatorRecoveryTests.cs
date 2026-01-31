using Lume.Compiler.Parsing;
using Lume.Compiler.Syntax;
using Lume.Compiler.Text;

public class SeparatorRecoveryTests
{
    [Fact]
    public void Extra_separators_are_ignored()
    {
        var sourceText = new SourceText(@"
print ""a""

;
;
print ""b""
", "test.lume");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        Assert.Equal(2, syntaxTree.Root.Statements.Count);
        Assert.All(syntaxTree.Root.Statements, statement => Assert.IsType<PrintStatementSyntax>(statement));
    }
}
