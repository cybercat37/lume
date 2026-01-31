using Lume.Compiler.Binding;
using Lume.Compiler.Parsing;
using Lume.Compiler.Text;

public class LiteralTypeTests
{
    [Fact]
    public void Integer_literal_binds_with_int_type()
    {
        var sourceText = new SourceText("print 1", "test.lume");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        var printStatement = Assert.IsType<BoundPrintStatement>(result.Program.Statements[0]);
        Assert.Same(TypeSymbol.Int, printStatement.Expression.Type);
    }
}
