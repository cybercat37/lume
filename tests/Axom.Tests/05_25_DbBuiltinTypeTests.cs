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
let tx1 = db.begin()
let tx2 = db.rollback()
print a
print b
print c
print tx1
print tx2
""";

        var syntaxTree = SyntaxTree.Parse(new SourceText(sourceText, "test.axom"));
        var binder = new Axom.Compiler.Binding.Binder();

        var result = binder.Bind(syntaxTree);

        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == Axom.Compiler.Diagnostics.DiagnosticSeverity.Error);
    }

    [Fact]
    public void Sql_literal_method_calls_bind_without_diagnostics_for_valid_signatures()
    {
        const string sourceText = """"
let a = sql"""select 1""".exec()
let b = sql"""select 1""".all()
let c = sql"""select 1""".one()
let d = sql"""select {id}""".one(["id": "1"])
print a
print b
print c
print d
"""";

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

    [Fact]
    public void Sql_literal_method_call_with_arguments_reports_diagnostic()
    {
        const string sourceText = "print sql\"\"\"select 1\"\"\".exec(1)";

        var syntaxTree = SyntaxTree.Parse(new SourceText(sourceText, "test.axom"));
        var binder = new Axom.Compiler.Binding.Binder();

        var result = binder.Bind(syntaxTree);

        Assert.Contains(result.Diagnostics, diagnostic =>
            diagnostic.Severity == Axom.Compiler.Diagnostics.DiagnosticSeverity.Error
            && diagnostic.Message.Contains("expects Map<String, String>", StringComparison.Ordinal));
    }

    [Fact]
    public void Sql_literal_with_param_placeholder_requires_params_map()
    {
        const string sourceText = "print sql\"\"\"select * from users where id = {id}\"\"\".one()";

        var syntaxTree = SyntaxTree.Parse(new SourceText(sourceText, "test.axom"));
        var binder = new Axom.Compiler.Binding.Binder();
        var result = binder.Bind(syntaxTree);

        Assert.Contains(result.Diagnostics, diagnostic =>
            diagnostic.Severity == Axom.Compiler.Diagnostics.DiagnosticSeverity.Error
            && diagnostic.Message.Contains("requires a params map", StringComparison.Ordinal));
    }

    [Fact]
    public void Sql_literal_map_argument_must_include_all_parameter_placeholders()
    {
        const string sourceText = "print sql\"\"\"select * from users where id = {id} and org = {org}\"\"\".all([\"id\": \"1\"])";

        var syntaxTree = SyntaxTree.Parse(new SourceText(sourceText, "test.axom"));
        var binder = new Axom.Compiler.Binding.Binder();
        var result = binder.Bind(syntaxTree);

        Assert.Contains(result.Diagnostics, diagnostic =>
            diagnostic.Severity == Axom.Compiler.Diagnostics.DiagnosticSeverity.Error
            && diagnostic.Message.Contains("missing SQL placeholders", StringComparison.Ordinal)
            && diagnostic.Message.Contains("org", StringComparison.Ordinal));
    }

    [Fact]
    public void Sql_literal_record_placeholder_requires_declared_record_type()
    {
        const string sourceText = "print sql\"\"\"select {User} from users\"\"\".all()";

        var syntaxTree = SyntaxTree.Parse(new SourceText(sourceText, "test.axom"));
        var binder = new Axom.Compiler.Binding.Binder();
        var result = binder.Bind(syntaxTree);

        Assert.Contains(result.Diagnostics, diagnostic =>
            diagnostic.Severity == Axom.Compiler.Diagnostics.DiagnosticSeverity.Error
            && diagnostic.Message.Contains("Unknown SQL record placeholder '{User}'", StringComparison.Ordinal));
    }

    [Fact]
    public void Sql_literal_record_placeholder_binds_when_record_type_is_declared()
    {
        const string sourceText = """"
type User { id: Int, name: String }
let rows = sql"""select {User} from users""".all()
print rows
"""";

        var syntaxTree = SyntaxTree.Parse(new SourceText(sourceText, "test.axom"));
        var binder = new Axom.Compiler.Binding.Binder();
        var result = binder.Bind(syntaxTree);

        Assert.DoesNotContain(result.Diagnostics, diagnostic =>
            diagnostic.Severity == Axom.Compiler.Diagnostics.DiagnosticSeverity.Error
            && diagnostic.Message.Contains("Unknown SQL record placeholder", StringComparison.Ordinal));
    }

    [Fact]
    public void Sql_literal_exec_rejects_record_projection_placeholder()
    {
        const string sourceText = """"
type User { id: Int, name: String }
print sql"""update users set name='x' returning {User}""".exec()
"""";

        var syntaxTree = SyntaxTree.Parse(new SourceText(sourceText, "test.axom"));
        var binder = new Axom.Compiler.Binding.Binder();
        var result = binder.Bind(syntaxTree);

        Assert.Contains(result.Diagnostics, diagnostic =>
            diagnostic.Severity == Axom.Compiler.Diagnostics.DiagnosticSeverity.Error
            && diagnostic.Message.Contains("exec() cannot use record projection placeholders", StringComparison.Ordinal));
    }

    [Fact]
    public void Db_transaction_members_reject_arguments()
    {
        const string sourceText = "print db.begin(1)\nprint db.commit(1)\nprint db.rollback(1)";

        var syntaxTree = SyntaxTree.Parse(new SourceText(sourceText, "test.axom"));
        var binder = new Axom.Compiler.Binding.Binder();
        var result = binder.Bind(syntaxTree);

        Assert.Contains(result.Diagnostics, diagnostic =>
            diagnostic.Severity == Axom.Compiler.Diagnostics.DiagnosticSeverity.Error
            && diagnostic.Message.Contains("expects no arguments", StringComparison.Ordinal));
    }

    [Fact]
    public void Transaction_statement_syntax_binds_successfully()
    {
        const string sourceText = """
transaction {
  let x = db.begin()
  print x
}
""";

        var syntaxTree = SyntaxTree.Parse(new SourceText(sourceText, "test.axom"));
        var binder = new Axom.Compiler.Binding.Binder();
        var result = binder.Bind(syntaxTree);

        Assert.DoesNotContain(result.Diagnostics, diagnostic =>
            diagnostic.Severity == Axom.Compiler.Diagnostics.DiagnosticSeverity.Error
            && diagnostic.Message.Contains("Unexpected", StringComparison.Ordinal));
    }
}
