using Axom.Compiler.Parsing;
using Axom.Compiler.Text;

public class RecordParserTests
{
    [Fact]
    public void Record_type_declaration_parses()
    {
        var sourceText = new SourceText(@"
type User {
  name: String,
  age: Int,
}
", "test.axom");

        var syntaxTree = SyntaxTree.Parse(sourceText);

        Assert.Empty(syntaxTree.Diagnostics);
    }

    [Fact]
    public void Record_literal_parses()
    {
        var sourceText = new SourceText(@"
type User { name: String, age: Int }
let user = User { name: ""Ada"", age: 36 }
", "test.axom");

        var syntaxTree = SyntaxTree.Parse(sourceText);

        Assert.Empty(syntaxTree.Diagnostics);
    }

    [Fact]
    public void Record_literal_allows_trailing_comma()
    {
        var sourceText = new SourceText(@"
type User { name: String, age: Int }
let user = User {
  name: ""Ada"",
  age: 36,
}
", "test.axom");

        var syntaxTree = SyntaxTree.Parse(sourceText);

        Assert.Empty(syntaxTree.Diagnostics);
    }

    [Fact]
    public void Record_type_allows_single_field_trailing_comma()
    {
        var sourceText = new SourceText(@"
type User {
  name: String,
}
", "test.axom");

        var syntaxTree = SyntaxTree.Parse(sourceText);

        Assert.Empty(syntaxTree.Diagnostics);
    }

    [Fact]
    public void Record_literal_with_spread_parses()
    {
        var sourceText = new SourceText(@"
type User { name: String, age: Int }
let base = User { name: ""Ada"", age: 36 }
let user = User { ...base }
", "test.axom");

        var syntaxTree = SyntaxTree.Parse(sourceText);

        Assert.Empty(syntaxTree.Diagnostics);
    }

    [Fact]
    public void Record_update_with_with_keyword_parses()
    {
        var sourceText = new SourceText(@"
type User { name: String, age: Int }
let user = User { name: ""Ada"", age: 36 }
let updated = user with { age: 37 }
", "test.axom");

        var syntaxTree = SyntaxTree.Parse(sourceText);

        Assert.Empty(syntaxTree.Diagnostics);
    }
}
