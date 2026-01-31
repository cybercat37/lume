using Lume.Compiler.Binding;
using Lume.Compiler.Parsing;
using Lume.Compiler.Text;

public class UnaryTypeMessageTests
{
    [Fact]
    public void Unary_operator_type_mismatch_message_includes_type()
    {
        var sourceText = new SourceText("print -true", "test.lume");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.NotEmpty(result.Diagnostics);
        Assert.Contains("Operator '-'", result.Diagnostics[0].Message);
        Assert.Contains("Bool", result.Diagnostics[0].Message);
    }
}
