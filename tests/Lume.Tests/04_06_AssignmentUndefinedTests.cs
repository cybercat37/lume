using Lume.Compiler.Binding;
using Lume.Compiler.Parsing;
using Lume.Compiler.Text;

public class AssignmentUndefinedTests
{
    [Fact]
    public void Assigning_to_undefined_variable_produces_diagnostic()
    {
        var sourceText = new SourceText("x = 1", "test.lume");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.NotEmpty(result.Diagnostics);
    }
}
