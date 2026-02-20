using Axom.Compiler.Interpreting;
using Axom.Compiler.Parsing;
using Axom.Compiler.Text;
using Axom.Runtime.Db;
using Microsoft.Data.Sqlite;

namespace Axom.Tests;

public class InterpreterDbBuiltinTests
{
    [Fact]
    public void Db_builtins_execute_through_configured_adapter()
    {
        using var fixture = SqliteFixture.Create();
        var adapter = new AdoNetDbAdapter(fixture.CreateConnection);
        DbBuiltinGateway.Configure(adapter);

        try
        {
            const string source = """
let created = db.exec("create table users (id integer primary key, name text not null)")
print match created {
  Ok(v) -> v
  Error(_) -> -1
}

let inserted = db.exec("insert into users (id, name) values (@id, @name)", ["id": "1", "name": "Ada"])
print match inserted {
  Ok(v) -> v
  Error(_) -> -1
}

let rows = db.query("select name from users where id = @id", ["id": "1"])
print match rows {
  Ok(_) -> 1
  Error(_) -> -1
}

let scalar = db.scalar("select name from users where id = @id", ["id": "1"])
print match scalar {
  Ok(v) -> v
  Error(e) -> e
}
""";

            var syntaxTree = SyntaxTree.Parse(new SourceText(source, "test.axom"));
            var interpreter = new Interpreter();
            var result = interpreter.Run(syntaxTree);

            Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == Axom.Compiler.Diagnostics.DiagnosticSeverity.Error);
            Assert.Equal("0\n1\n1\nAda", result.Output);
        }
        finally
        {
            DbBuiltinGateway.Reset();
        }
    }

    [Fact]
    public void Db_builtins_return_error_when_adapter_is_not_configured()
    {
        DbBuiltinGateway.Reset();
        const string source = """
let value = db.scalar("select 1")
print match value {
  Ok(v) -> v
  Error(e) -> e
}
""";

        var syntaxTree = SyntaxTree.Parse(new SourceText(source, "test.axom"));
        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == Axom.Compiler.Diagnostics.DiagnosticSeverity.Error);
        Assert.Contains("db adapter is not configured", result.Output, StringComparison.Ordinal);
    }

    private sealed class SqliteFixture : IDisposable
    {
        private readonly string dbPath;

        private SqliteFixture(string dbPath)
        {
            this.dbPath = dbPath;
        }

        public static SqliteFixture Create()
        {
            var tempRoot = Path.Combine(Path.GetTempPath(), "axom_sqlite_interpreter_tests", Guid.NewGuid().ToString("N", System.Globalization.CultureInfo.InvariantCulture));
            Directory.CreateDirectory(tempRoot);
            var dbPath = Path.Combine(tempRoot, "test.db");
            return new SqliteFixture(dbPath);
        }

        public SqliteConnection CreateConnection()
        {
            return new SqliteConnection($"Data Source={dbPath}");
        }

        public void Dispose()
        {
            var parent = Path.GetDirectoryName(dbPath);
            if (!string.IsNullOrWhiteSpace(parent) && Directory.Exists(parent))
            {
                Directory.Delete(parent, recursive: true);
            }
        }
    }
}
