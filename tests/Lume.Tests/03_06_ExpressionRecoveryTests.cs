using Lume.Compiler.Parsing;
using Lume.Compiler.Text;

public class ExpressionRecoveryTests
{
    [Fact]
    public void Invalid_operator_sequence_produces_diagnostic()
    {
        var sourceText = new SourceText("print + * 2", "test.lume");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        Assert.NotEmpty(syntaxTree.Diagnostics);
    }
}
