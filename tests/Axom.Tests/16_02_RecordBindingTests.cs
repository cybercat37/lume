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

    [Fact]
    public void Record_literal_with_spread_binds()
    {
        var sourceText = new SourceText(@"
type User { name: String, age: Int }
let base = User { name: ""Ada"", age: 36 }
let user = User { ...base }
", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Record_literal_with_spread_and_override_produces_diagnostic()
    {
        var sourceText = new SourceText(@"
type User { name: String, age: Int }
let base = User { name: ""Ada"", age: 36 }
let user = User { ...base, age: 37 }
", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.NotEmpty(result.Diagnostics);
        Assert.Contains(result.Diagnostics, diagnostic =>
            diagnostic.Message.Contains("Use 'with", StringComparison.Ordinal));
    }

    [Fact]
    public void Record_literal_spread_type_mismatch_produces_diagnostic()
    {
        var sourceText = new SourceText(@"
type User { name: String, age: Int }
let user = User { ...42 }
", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.NotEmpty(result.Diagnostics);
    }

    [Fact]
    public void Record_update_with_with_keyword_binds()
    {
        var sourceText = new SourceText(@"
type User { name: String, age: Int }
let user = User { name: ""Ada"", age: 36 }
let updated = user with { age: 37 }
", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Record_update_requires_record_target()
    {
        var sourceText = new SourceText("let updated = 42 with { age: 37 }", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.NotEmpty(result.Diagnostics);
    }
}
