using Axom.Compiler.Binding;
using Axom.Compiler.Parsing;
using Axom.Compiler.Text;

public class LiteralTypeTests
{
    [Fact]
    public void Integer_literal_binds_with_int_type()
    {
        var sourceText = new SourceText("print 1", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        var printStatement = Assert.IsType<BoundPrintStatement>(result.Program.Statements[0]);
        Assert.Same(TypeSymbol.Int, printStatement.Expression.Type);
    }
}
