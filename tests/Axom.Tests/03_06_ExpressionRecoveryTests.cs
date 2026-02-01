using Axom.Compiler.Parsing;
using Axom.Compiler.Text;

public class ExpressionRecoveryTests
{
    [Fact]
    public void Invalid_operator_sequence_produces_diagnostic()
    {
        var sourceText = new SourceText("print + * 2", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        Assert.NotEmpty(syntaxTree.Diagnostics);
    }
}
