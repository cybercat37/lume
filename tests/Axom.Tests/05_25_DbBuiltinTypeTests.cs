using Axom.Compiler.Parsing;
using Axom.Compiler.Text;

namespace Axom.Tests;

public class DbBuiltinTypeTests
{
    [Fact]
    public void Db_member_calls_bind_without_diagnostics_for_valid_signatures()
    {
        const string sourceText = """
let a = db.exec("select 1")
let b = db.query("select 1")
let c = db.scalar("select 1")
print a
print b
print c
""";

        var syntaxTree = SyntaxTree.Parse(new SourceText(sourceText, "test.axom"));
        var binder = new Axom.Compiler.Binding.Binder();

        var result = binder.Bind(syntaxTree);

        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == Axom.Compiler.Diagnostics.DiagnosticSeverity.Error);
    }

    [Fact]
    public void Db_exec_reports_type_error_for_non_string_sql()
    {
        const string sourceText = "print db.exec(123)";

        var syntaxTree = SyntaxTree.Parse(new SourceText(sourceText, "test.axom"));
        var binder = new Axom.Compiler.Binding.Binder();

        var result = binder.Bind(syntaxTree);

        Assert.Contains(result.Diagnostics, diagnostic =>
            diagnostic.Severity == Axom.Compiler.Diagnostics.DiagnosticSeverity.Error
            && diagnostic.Message.Contains("db_exec() expects String sql", StringComparison.Ordinal));
    }

    [Fact]
    public void Db_query_reports_type_error_for_non_map_params()
    {
        const string sourceText = "print db.query(\"select 1\", 1)";

        var syntaxTree = SyntaxTree.Parse(new SourceText(sourceText, "test.axom"));
        var binder = new Axom.Compiler.Binding.Binder();

        var result = binder.Bind(syntaxTree);

        Assert.Contains(result.Diagnostics, diagnostic =>
            diagnostic.Severity == Axom.Compiler.Diagnostics.DiagnosticSeverity.Error
            && diagnostic.Message.Contains("db_query() expects Map<String, String>", StringComparison.Ordinal));
    }
}
