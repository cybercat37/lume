using Axom.Compiler.Binding;
using Axom.Compiler.Parsing;
using Axom.Compiler.Text;

public class SumTypeBindingTests
{
    [Fact]
    public void Sum_type_constructor_binds()
    {
        var sourceText = new SourceText(@"
type Result { Ok(Int) Error(String) }
let value = Ok(1)
", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Sum_type_constructor_type_mismatch_produces_diagnostic()
    {
        var sourceText = new SourceText(@"
type Result { Ok(Int) Error(String) }
let value = Ok(""no"")
", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.NotEmpty(result.Diagnostics);
    }

    [Fact]
    public void Sum_type_payloadless_constructor_binds()
    {
        var sourceText = new SourceText(@"
type Status { Ready Error(String) }
let value = Ready
", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.Empty(result.Diagnostics);
    }
}
