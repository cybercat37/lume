using Axom.Compiler.Binding;
using Axom.Compiler.Parsing;
using Axom.Compiler.Text;

public class SumTypeExhaustivenessTests
{
    [Fact]
    public void Sum_type_missing_variant_produces_diagnostic()
    {
        var sourceText = new SourceText(@"
type Result { Ok(Int) Error(String) }
let value = match Ok(1) {
  Ok(x) -> x
}
", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.NotEmpty(result.Diagnostics);
    }
}
