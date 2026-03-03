using Axom.Compiler.Interpreting;
using Axom.Compiler.Parsing;
using Axom.Compiler.Text;
using Axom.Runtime.Db;
using Microsoft.Data.Sqlite;

namespace Axom.Tests;

[Collection("DbGatewayTests")]
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
    public void Sql_literal_methods_execute_through_configured_adapter()
    {
        using var fixture = SqliteFixture.Create();
        var adapter = new AdoNetDbAdapter(fixture.CreateConnection);
        DbBuiltinGateway.Configure(adapter);

        try
        {
            const string source = """"
let created = sql"""
create table users (id integer primary key, name text not null)
""".exec()
print match created {
  Ok(v) -> v
  Error(_) -> -1
}

let inserted = sql"""
insert into users (id, name) values (1, 'Ada')
""".exec()
print match inserted {
  Ok(v) -> v
  Error(_) -> -1
}

let scalar = sql"""
select name from users where id = 1
""".one()
print match scalar {
  Ok(v) -> v
  Error(e) -> e
}
"""";

            var syntaxTree = SyntaxTree.Parse(new SourceText(source, "test.axom"));
            var interpreter = new Interpreter();
            var result = interpreter.Run(syntaxTree);

            Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == Axom.Compiler.Diagnostics.DiagnosticSeverity.Error);
            Assert.Equal("0\n1\nAda", result.Output);
        }
        finally
        {
            DbBuiltinGateway.Reset();
        }
    }

    [Fact]
    public void Sql_literal_methods_bind_curly_parameters_with_map_argument()
    {
        using var fixture = SqliteFixture.Create();
        var adapter = new AdoNetDbAdapter(fixture.CreateConnection);
        DbBuiltinGateway.Configure(adapter);

        try
        {
            const string source = """"
let created = sql"""
create table users (id integer primary key, name text not null)
""".exec()
print match created {
  Ok(v) -> v
  Error(_) -> -1
}

let inserted = sql"""
insert into users (id, name) values ({id}, {name})
""".exec(["id": "2", "name": "Bob"])
print match inserted {
  Ok(v) -> v
  Error(_) -> -1
}

let scalar = sql"""
select name from users where id = {id}
""".one(["id": "2"])
print match scalar {
  Ok(v) -> v
  Error(e) -> e
}
"""";

            var syntaxTree = SyntaxTree.Parse(new SourceText(source, "test.axom"));
            var interpreter = new Interpreter();
            var result = interpreter.Run(syntaxTree);

            Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == Axom.Compiler.Diagnostics.DiagnosticSeverity.Error);
            Assert.Equal("0\n1\nBob", result.Output);
        }
        finally
        {
            DbBuiltinGateway.Reset();
        }
    }

    [Fact]
    public void Sql_literal_record_placeholder_returns_error_when_projection_resolver_is_missing()
    {
        using var fixture = SqliteFixture.Create();
        var adapter = new AdoNetDbAdapter(fixture.CreateConnection);
        DbBuiltinGateway.Configure(adapter);

        try
        {
            const string source = """"
let created = sql"""
create table users (id integer primary key, name text not null)
""".exec()
print match created {
  Ok(v) -> v
  Error(_) -> -1
}

let inserted = sql"""
insert into users (id, name) values (1, 'Ada')
""".exec()
print match inserted {
  Ok(v) -> v
  Error(_) -> -1
}

type User { id: Int, name: String }

let query = sql"""
select {User} from users where id = {id}
""".all(["id": "1"])
print match query {
  Ok(_) -> "ok"
  Error(e) -> e
}
"""";

            var syntaxTree = SyntaxTree.Parse(new SourceText(source, "test.axom"));
            var interpreter = new Interpreter();
            var result = interpreter.Run(syntaxTree);

            Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == Axom.Compiler.Diagnostics.DiagnosticSeverity.Error);
            Assert.Contains("requires a record projection resolver", result.Output, StringComparison.Ordinal);
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

    [Fact]
    public void Db_transaction_commit_and_rollback_control_visibility()
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

let tx1 = db.begin()
print match tx1 {
  Ok(v) -> v
  Error(_) -> -1
}

let inserted1 = db.exec("insert into users (id, name) values (1, 'Ada')")
print match inserted1 {
  Ok(v) -> v
  Error(_) -> -1
}

let rollbacked = db.rollback()
print match rollbacked {
  Ok(v) -> v
  Error(_) -> -1
}

let countAfterRollback = db.scalar("select count(*) from users")
print match countAfterRollback {
  Ok(v) -> v
  Error(e) -> e
}

let tx2 = db.begin()
print match tx2 {
  Ok(v) -> v
  Error(_) -> -1
}

let inserted2 = db.exec("insert into users (id, name) values (2, 'Bob')")
print match inserted2 {
  Ok(v) -> v
  Error(_) -> -1
}

let committed = db.commit()
print match committed {
  Ok(v) -> v
  Error(_) -> -1
}

let countAfterCommit = db.scalar("select count(*) from users")
print match countAfterCommit {
  Ok(v) -> v
  Error(e) -> e
}
""";

            var syntaxTree = SyntaxTree.Parse(new SourceText(source, "test.axom"));
            var interpreter = new Interpreter();
            var result = interpreter.Run(syntaxTree);

            Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == Axom.Compiler.Diagnostics.DiagnosticSeverity.Error);
            Assert.Equal("0\n1\n1\n1\n0\n1\n1\n1\n1", result.Output);
        }
        finally
        {
            DbBuiltinGateway.Reset();
        }
    }

    [Fact]
    public void Transaction_statement_commits_block_changes()
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

transaction {
  let inserted = db.exec("insert into users (id, name) values (7, 'Zoe')")
  print match inserted {
    Ok(v) -> v
    Error(_) -> -1
  }
}

let count = db.scalar("select count(*) from users")
print match count {
  Ok(v) -> v
  Error(e) -> e
}
""";

            var syntaxTree = SyntaxTree.Parse(new SourceText(source, "test.axom"));
            var interpreter = new Interpreter();
            var result = interpreter.Run(syntaxTree);

            Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == Axom.Compiler.Diagnostics.DiagnosticSeverity.Error);
            Assert.Equal("0\n1\n1", result.Output);
        }
        finally
        {
            DbBuiltinGateway.Reset();
        }
    }

    [Fact]
    public void Transaction_statement_rolls_back_automatically_on_early_return()
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

fn add_and_return() -> Int {
  transaction {
    let inserted = db.exec("insert into users (id, name) values (55, 'Ria')")
    print match inserted {
      Ok(v) -> v
      Error(_) -> -1
    }
    return 7
  }
  return -1
}

print add_and_return()

let count = db.scalar("select count(*) from users")
print match count {
  Ok(v) -> v
  Error(e) -> e
}
""";

            var syntaxTree = SyntaxTree.Parse(new SourceText(source, "test.axom"));
            var interpreter = new Interpreter();
            var result = interpreter.Run(syntaxTree);

            Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == Axom.Compiler.Diagnostics.DiagnosticSeverity.Error);
            Assert.Equal("0\n1\n7\n0", result.Output);
        }
        finally
        {
            DbBuiltinGateway.Reset();
        }
    }

    [Fact]
    public void Transaction_statement_rolls_back_when_runtime_diagnostic_occurs()
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

transaction {
  let inserted = db.exec("insert into users (id, name) values (77, 'Diag')")
  print match inserted {
    Ok(v) -> v
    Error(_) -> -1
  }

  let crash = 1 / 0
  print crash
}

let count = db.scalar("select count(*) from users")
print match count {
  Ok(v) -> v
  Error(e) -> e
}
""";

            var syntaxTree = SyntaxTree.Parse(new SourceText(source, "test.axom"));
            var interpreter = new Interpreter();
            var result = interpreter.Run(syntaxTree);

            Assert.Contains(result.Diagnostics, diagnostic =>
                diagnostic.Severity == Axom.Compiler.Diagnostics.DiagnosticSeverity.Error
                && diagnostic.Message.Contains("Division by zero", StringComparison.Ordinal));

            var persisted = adapter.Scalar<long>("select count(*) from users");
            Assert.Equal(0L, persisted);
        }
        finally
        {
            DbBuiltinGateway.Reset();
        }
    }

    [Fact]
    public void Transaction_statement_releases_transaction_after_runtime_error()
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

transaction {
  let inserted = db.exec("insert into users (id, name) values (88, 'LeakCheck')")
  print match inserted {
    Ok(v) -> v
    Error(_) -> -1
  }
  let crash = 1 / 0
  print crash
}

let beginAgain = db.begin()
print match beginAgain {
  Ok(v) -> v
  Error(_) -> -1
}

let rollbackAgain = db.rollback()
print match rollbackAgain {
  Ok(v) -> v
  Error(_) -> -1
}
""";

            var syntaxTree = SyntaxTree.Parse(new SourceText(source, "test.axom"));
            var interpreter = new Interpreter();
            var result = interpreter.Run(syntaxTree);

            Assert.Contains(result.Diagnostics, diagnostic =>
                diagnostic.Severity == Axom.Compiler.Diagnostics.DiagnosticSeverity.Error
                && diagnostic.Message.Contains("Division by zero", StringComparison.Ordinal));

            Assert.Equal("0\n1", result.Output);

            var persisted = adapter.Scalar<long>("select count(*) from users");
            Assert.Equal(0L, persisted);

            const string followUpSource = """
let beginAgain = db.begin()
print match beginAgain {
  Ok(v) -> v
  Error(_) -> -1
}

let rollbackAgain = db.rollback()
print match rollbackAgain {
  Ok(v) -> v
  Error(_) -> -1
}
""";

            var followUpTree = SyntaxTree.Parse(new SourceText(followUpSource, "follow-up.axom"));
            var followUp = new Interpreter().Run(followUpTree);

            Assert.DoesNotContain(followUp.Diagnostics, diagnostic => diagnostic.Severity == Axom.Compiler.Diagnostics.DiagnosticSeverity.Error);
            Assert.Equal("1\n1", followUp.Output);
        }
        finally
        {
            DbBuiltinGateway.Reset();
        }
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
