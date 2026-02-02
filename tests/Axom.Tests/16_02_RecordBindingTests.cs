using Axom.Compiler.Binding;
using Axom.Compiler.Parsing;
using Axom.Compiler.Text;

public class RecordBindingTests
{
    [Fact]
    public void Record_literal_with_all_fields_binds()
    {
        var sourceText = new SourceText(@"
type User { name: String, age: Int }
let user = User { name: ""Ada"", age: 36 }
", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Record_literal_missing_field_produces_diagnostic()
    {
        var sourceText = new SourceText(@"
type User { name: String, age: Int }
let user = User { name: ""Ada"" }
", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.NotEmpty(result.Diagnostics);
    }

    [Fact]
    public void Record_literal_unknown_field_produces_diagnostic()
    {
        var sourceText = new SourceText(@"
type User { name: String, age: Int }
let user = User { name: ""Ada"", active: true }
", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.NotEmpty(result.Diagnostics);
    }

    [Fact]
    public void Record_literal_type_mismatch_produces_diagnostic()
    {
        var sourceText = new SourceText(@"
type User { name: String, age: Int }
let user = User { name: 42, age: 36 }
", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.NotEmpty(result.Diagnostics);
    }
}
