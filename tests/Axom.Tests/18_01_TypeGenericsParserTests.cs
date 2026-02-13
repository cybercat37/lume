using Axom.Compiler.Parsing;
using Axom.Compiler.Text;

public class TypeGenericsParserTests
{
    [Fact]
    public void Generic_record_type_declaration_parses()
    {
        var sourceText = new SourceText("type Box<T> { value: T }", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        Assert.Empty(syntaxTree.Diagnostics);
    }

    [Fact]
    public void Generic_sum_type_declaration_parses()
    {
        var sourceText = new SourceText("type ResultLike<T, E> { Ok(T) Error(E) }", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        Assert.Empty(syntaxTree.Diagnostics);
    }

    [Fact]
    public void Generic_type_usage_in_function_signature_parses()
    {
        var sourceText = new SourceText("fn sum_all(xs: List<Int>) -> Int => sum(xs)", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        Assert.Empty(syntaxTree.Diagnostics);
    }
}
