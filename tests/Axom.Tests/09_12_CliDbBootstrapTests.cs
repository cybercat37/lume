using Axom.Compiler.Interpreting;
using Axom.Compiler.Parsing;
using Axom.Compiler.Text;
using Axom.Runtime.Db;

namespace Axom.Tests;

[Collection("DbGatewayTests")]
public class CliDbBootstrapTests
{
    [Fact]
    public void Configure_from_environment_returns_false_when_unset()
    {
        var previousProvider = Environment.GetEnvironmentVariable("AXOM_DB_PROVIDER");
        var previousConnection = Environment.GetEnvironmentVariable("AXOM_DB_CONNECTION_STRING");
        var previousProjections = Environment.GetEnvironmentVariable("AXOM_DB_RECORD_PROJECTIONS");

        try
        {
            Environment.SetEnvironmentVariable("AXOM_DB_PROVIDER", null);
            Environment.SetEnvironmentVariable("AXOM_DB_CONNECTION_STRING", null);
            Environment.SetEnvironmentVariable("AXOM_DB_RECORD_PROJECTIONS", null);

            var configured = Axom.Cli.DbRuntimeBootstrap.ConfigureFromEnvironment(new StringWriter());

            Assert.False(configured);
        }
        finally
        {
            Environment.SetEnvironmentVariable("AXOM_DB_PROVIDER", previousProvider);
            Environment.SetEnvironmentVariable("AXOM_DB_CONNECTION_STRING", previousConnection);
            Environment.SetEnvironmentVariable("AXOM_DB_RECORD_PROJECTIONS", previousProjections);
            DbBuiltinGateway.Reset();
        }
    }

    [Fact]
    public void Configure_from_environment_sets_db_gateway_for_interpreter_builtins()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "axom_cli_db_bootstrap", Guid.NewGuid().ToString("N", System.Globalization.CultureInfo.InvariantCulture));
        Directory.CreateDirectory(tempRoot);
        var dbPath = Path.Combine(tempRoot, "test.db");

        var previousProvider = Environment.GetEnvironmentVariable("AXOM_DB_PROVIDER");
        var previousConnection = Environment.GetEnvironmentVariable("AXOM_DB_CONNECTION_STRING");
        var previousLog = Environment.GetEnvironmentVariable("AXOM_DB_LOG");
        var previousLogSql = Environment.GetEnvironmentVariable("AXOM_DB_LOG_SQL");
        var previousProjections = Environment.GetEnvironmentVariable("AXOM_DB_RECORD_PROJECTIONS");

        try
        {
            Environment.SetEnvironmentVariable("AXOM_DB_PROVIDER", "sqlite");
            Environment.SetEnvironmentVariable("AXOM_DB_CONNECTION_STRING", $"Data Source={dbPath}");
            Environment.SetEnvironmentVariable("AXOM_DB_LOG", "all");
            Environment.SetEnvironmentVariable("AXOM_DB_LOG_SQL", "0");
            Environment.SetEnvironmentVariable("AXOM_DB_RECORD_PROJECTIONS", null);

            var logs = new StringWriter();
            var configured = Axom.Cli.DbRuntimeBootstrap.ConfigureFromEnvironment(logs);
            Assert.True(configured);

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
let scalar = db.scalar("select name from users where id = @id", ["id": "1"])
print match scalar {
  Ok(v) -> v
  Error(e) -> e
}
""";

            var interpreter = new Interpreter();
            var result = interpreter.Run(SyntaxTree.Parse(new SourceText(source, "test.axom")));

            Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == Axom.Compiler.Diagnostics.DiagnosticSeverity.Error);
            Assert.Equal("0\n1\nAda", result.Output);

            var logOutput = logs.ToString();
            Assert.Contains("db query_id=", logOutput, StringComparison.Ordinal);
            Assert.DoesNotContain("db sql=", logOutput, StringComparison.Ordinal);
        }
        finally
        {
            Environment.SetEnvironmentVariable("AXOM_DB_PROVIDER", previousProvider);
            Environment.SetEnvironmentVariable("AXOM_DB_CONNECTION_STRING", previousConnection);
            Environment.SetEnvironmentVariable("AXOM_DB_LOG", previousLog);
            Environment.SetEnvironmentVariable("AXOM_DB_LOG_SQL", previousLogSql);
            Environment.SetEnvironmentVariable("AXOM_DB_RECORD_PROJECTIONS", previousProjections);
            DbBuiltinGateway.Reset();

            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    [Fact]
    public void Configure_from_environment_parses_record_projection_map_for_sql_record_placeholders()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "axom_cli_db_bootstrap", Guid.NewGuid().ToString("N", System.Globalization.CultureInfo.InvariantCulture));
        Directory.CreateDirectory(tempRoot);
        var dbPath = Path.Combine(tempRoot, "test.db");

        var previousProvider = Environment.GetEnvironmentVariable("AXOM_DB_PROVIDER");
        var previousConnection = Environment.GetEnvironmentVariable("AXOM_DB_CONNECTION_STRING");
        var previousProjections = Environment.GetEnvironmentVariable("AXOM_DB_RECORD_PROJECTIONS");

        try
        {
            Environment.SetEnvironmentVariable("AXOM_DB_PROVIDER", "sqlite");
            Environment.SetEnvironmentVariable("AXOM_DB_CONNECTION_STRING", $"Data Source={dbPath}");
            Environment.SetEnvironmentVariable("AXOM_DB_RECORD_PROJECTIONS", "User:id,name");

            var configured = Axom.Cli.DbRuntimeBootstrap.ConfigureFromEnvironment(new StringWriter());
            Assert.True(configured);

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
""".exec(["id": "1", "name": "Ada"])
print match inserted {
  Ok(v) -> v
  Error(_) -> -1
}

type User { id: Int, name: String }

let rows = sql"""
select {User} from users where id = {id}
""".all(["id": "1"])
print match rows {
  Ok(_) -> "ok"
  Error(e) -> e
}
"""";

            var interpreter = new Interpreter();
            var result = interpreter.Run(SyntaxTree.Parse(new SourceText(source, "test.axom")));

            Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == Axom.Compiler.Diagnostics.DiagnosticSeverity.Error);
            Assert.Equal("0\n1\nok", result.Output);
        }
        finally
        {
            Environment.SetEnvironmentVariable("AXOM_DB_PROVIDER", previousProvider);
            Environment.SetEnvironmentVariable("AXOM_DB_CONNECTION_STRING", previousConnection);
            Environment.SetEnvironmentVariable("AXOM_DB_RECORD_PROJECTIONS", previousProjections);
            DbBuiltinGateway.Reset();

            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    [Fact]
    public void Configure_from_environment_fails_for_invalid_record_projection_map()
    {
        var previousProvider = Environment.GetEnvironmentVariable("AXOM_DB_PROVIDER");
        var previousConnection = Environment.GetEnvironmentVariable("AXOM_DB_CONNECTION_STRING");
        var previousProjections = Environment.GetEnvironmentVariable("AXOM_DB_RECORD_PROJECTIONS");

        try
        {
            Environment.SetEnvironmentVariable("AXOM_DB_PROVIDER", "sqlite");
            Environment.SetEnvironmentVariable("AXOM_DB_CONNECTION_STRING", "Data Source=:memory:");
            Environment.SetEnvironmentVariable("AXOM_DB_RECORD_PROJECTIONS", "bad-entry-without-colons");

            var errors = new StringWriter();
            var configured = Axom.Cli.DbRuntimeBootstrap.ConfigureFromEnvironment(errors);

            Assert.False(configured);
            Assert.Contains("AXOM_DB_RECORD_PROJECTIONS", errors.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            Environment.SetEnvironmentVariable("AXOM_DB_PROVIDER", previousProvider);
            Environment.SetEnvironmentVariable("AXOM_DB_CONNECTION_STRING", previousConnection);
            Environment.SetEnvironmentVariable("AXOM_DB_RECORD_PROJECTIONS", previousProjections);
            DbBuiltinGateway.Reset();
        }
    }
}
