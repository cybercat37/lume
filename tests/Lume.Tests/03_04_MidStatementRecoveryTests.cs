using Lume.Compiler.Parsing;
using Lume.Compiler.Syntax;
using Lume.Compiler.Text;

public class MidStatementRecoveryTests
{
    [Fact]
    public void Junk_token_between_statements_does_not_stop_parsing()
    {
        var sourceText = new SourceText(@"
print ""a""
@
print ""b""
", "test.lume");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        Assert.Equal(2, syntaxTree.Root.Statements.Count);
        Assert.All(syntaxTree.Root.Statements, statement => Assert.IsType<PrintStatementSyntax>(statement));
        Assert.NotEmpty(syntaxTree.Diagnostics);
    }
}
