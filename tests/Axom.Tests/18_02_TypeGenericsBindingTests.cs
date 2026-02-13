using Axom.Compiler.Binding;
using Axom.Compiler.Parsing;
using Axom.Compiler.Text;

public class TypeGenericsBindingTests
{
    [Fact]
    public void Generic_type_annotations_bind_for_record_and_builtin_generics()
    {
        var sourceText = new SourceText(@"
type Box<T> { value: T }
fn consume(box: Box<Int>, xs: List<Int>, outcome: Result<Int, String>) -> Int => sum(xs)
", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Generic_type_with_missing_arguments_produces_diagnostic()
    {
        var sourceText = new SourceText(@"
type Box<T> { value: T }
fn consume(box: Box) -> Int => 0
", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.Contains(result.Diagnostics, diagnostic =>
            diagnostic.Message.Contains("expects 1 type arguments", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Builtin_generic_type_with_wrong_arity_produces_diagnostic()
    {
        var sourceText = new SourceText("fn consume(xs: List<Int, String>) -> Int => 0", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.Contains(result.Diagnostics, diagnostic =>
            diagnostic.Message.Contains("Type 'List' expects 1 type argument", StringComparison.OrdinalIgnoreCase));
    }
}
