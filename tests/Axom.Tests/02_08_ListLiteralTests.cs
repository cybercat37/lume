using Axom.Compiler.Binding;
using Axom.Compiler.Interpreting;
using Axom.Compiler.Parsing;
using Axom.Compiler.Text;

public class ListLiteralTests
{
    [Fact]
    public void List_literal_parses()
    {
        var sourceText = new SourceText("let xs = [1, 2, 3]", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        Assert.Empty(syntaxTree.Diagnostics);
    }

    [Fact]
    public void List_literal_requires_consistent_types()
    {
        var sourceText = new SourceText("let xs = [1, \"two\"]", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.NotEmpty(result.Diagnostics);
    }

    [Fact]
    public void List_literal_prints()
    {
        var sourceText = new SourceText("print [1, 2, 3]", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        Assert.Equal("[1, 2, 3]", result.Output.Trim());
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void List_indexing_prints_element()
    {
        var sourceText = new SourceText("print [10, 20, 30][1]", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        Assert.Equal("20", result.Output.Trim());
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Map_literal_prints()
    {
        var sourceText = new SourceText("print [\"a\": 1, \"b\": 2]", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        Assert.Contains("a: 1", result.Output.Trim());
        Assert.Contains("b: 2", result.Output.Trim());
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Map_literal_requires_string_keys()
    {
        var sourceText = new SourceText("let m = [1: 2]", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.NotEmpty(result.Diagnostics);
    }

    [Fact]
    public void Map_indexing_prints_value()
    {
        var sourceText = new SourceText("print [\"a\": 1, \"b\": 2][\"b\"]", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        Assert.Equal("2", result.Output.Trim());
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Spawn_join_prints_values()
    {
        var sourceText = new SourceText(@"
scope {
  let a = spawn { 1 + 1 }
  let b = spawn { 3 + 4 }
  print join a
  print join b
}
", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        var lines = result.Output.Split('\n');
        Assert.Equal("2", lines[0].Trim());
        Assert.Equal("7", lines[1].Trim());
        Assert.Empty(result.Diagnostics);
    }
}
