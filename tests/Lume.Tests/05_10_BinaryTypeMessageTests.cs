using Lume.Compiler.Binding;
using Lume.Compiler.Parsing;
using Lume.Compiler.Text;

public class BinaryTypeMessageTests
{
    [Fact]
    public void Binary_operator_type_mismatch_message_includes_types()
    {
        var sourceText = new SourceText("print \"a\" + 1", "test.lume");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.NotEmpty(result.Diagnostics);
        Assert.Contains("Operator '+' is not defined", result.Diagnostics[0].Message);
        Assert.Contains("String", result.Diagnostics[0].Message);
        Assert.Contains("Int", result.Diagnostics[0].Message);
    }
}
