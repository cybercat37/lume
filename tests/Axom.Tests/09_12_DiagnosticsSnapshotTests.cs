using Axom.Compiler;
using System.Globalization;

namespace Axom.Tests;

public class DiagnosticsSnapshotTests
{
    [Fact]
    public void Parser_diagnostics_match_snapshot()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile("print", "test.axom");

        Assert.False(result.Success);

        var snapshot = Snapshots.Read("ParserUnexpectedToken.snapshot.txt");
        var actual = Snapshots.Format(result.Diagnostics);
        Assert.Equal(snapshot, actual);
    }

    [Fact]
    public void Parser_missing_close_paren_matches_snapshot()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile("print (1 +", "test.axom");

        Assert.False(result.Success);

        var snapshot = Snapshots.Read("ParserMissingCloseParen.snapshot.txt");
        var actual = Snapshots.Format(result.Diagnostics);
        Assert.Equal(snapshot, actual);
    }

    [Fact]
    public void Parser_unexpected_token_in_block_matches_snapshot()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile("{\n)\n}", "test.axom");

        Assert.False(result.Success);

        var snapshot = Snapshots.Read("ParserUnexpectedTokenInBlock.snapshot.txt");
        var actual = Snapshots.Format(result.Diagnostics);
        Assert.Equal(snapshot, actual);
    }

    [Fact]
    public void Binder_undefined_variable_matches_snapshot()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile("print x", "test.axom");

        Assert.False(result.Success);

        var snapshot = Snapshots.Read("BinderUndefinedVariable.snapshot.txt");
        var actual = Snapshots.Format(result.Diagnostics);
        Assert.Equal(snapshot, actual);
    }

    [Fact]
    public void Binder_immutable_assignment_matches_snapshot()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile("let x = 1\nx = 2", "test.axom");

        Assert.False(result.Success);

        var snapshot = Snapshots.Read("BinderImmutableAssignment.snapshot.txt");
        var actual = Snapshots.Format(result.Diagnostics);
        Assert.Equal(snapshot, actual);
    }

    [Fact]
    public void Type_mismatch_assignment_matches_snapshot()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile("let mut x = 1\nx = true", "test.axom");

        Assert.False(result.Success);

        var snapshot = Snapshots.Read("TypeMismatchAssignment.snapshot.txt");
        var actual = Snapshots.Format(result.Diagnostics);
        Assert.Equal(snapshot, actual);
    }

    [Fact]
    public void Match_non_exhaustive_diagnostic_matches_snapshot()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile("let result = match true { true -> 1 }", "test.axom");

        Assert.False(result.Success);

        var snapshot = Snapshots.Read("MatchNonExhaustive.snapshot.txt");
        var actual = Snapshots.Format(result.Diagnostics);
        Assert.Equal(snapshot, actual);
    }

    [Fact]
    public void Match_unreachable_arm_diagnostic_matches_snapshot()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile("let result = match 1 { _ -> 1; 2 -> 2 }", "test.axom");

        Assert.False(result.Success);

        var snapshot = Snapshots.Read("MatchUnreachableArm.snapshot.txt");
        var actual = Snapshots.Format(result.Diagnostics);
        Assert.Equal(snapshot, actual);
    }

    [Fact]
    public void Match_tuple_arity_diagnostic_matches_snapshot()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile("let result = match (1, 2) { (x, y, z) -> x; _ -> 0 }", "test.axom");

        Assert.False(result.Success);

        var snapshot = Snapshots.Read("MatchTupleArity.snapshot.txt");
        var actual = Snapshots.Format(result.Diagnostics);
        Assert.Equal(snapshot, actual);
    }

    [Fact]
    public void Match_relational_type_mismatch_diagnostic_matches_snapshot()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile("let result = match \"x\" {\n  <= 1 -> \"small\"\n  _ -> \"other\"\n}", "test.axom");

        Assert.False(result.Success);

        var snapshot = Snapshots.Read("MatchRelationalTypeMismatch.snapshot.txt");
        var actual = Snapshots.Format(result.Diagnostics);
        Assert.Equal(snapshot, actual);
    }

    [Fact]
    public void Match_relational_non_exhaustive_diagnostic_matches_snapshot()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile("let result = match 1 { <= 1 -> 1 }", "test.axom");

        Assert.False(result.Success);

        var snapshot = Snapshots.Read("MatchRelationalNonExhaustive.snapshot.txt");
        var actual = Snapshots.Format(result.Diagnostics);
        Assert.Equal(snapshot, actual);
    }

    [Fact]
    public void Record_missing_field_diagnostic_matches_snapshot()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile("type User { name: String, age: Int }\nlet user = User { name: \"Ada\" }", "test.axom");

        Assert.False(result.Success);

        var snapshot = Snapshots.Read("RecordMissingField.snapshot.txt");
        var actual = Snapshots.Format(result.Diagnostics);
        Assert.Equal(snapshot, actual);
    }

    [Fact]
    public void Record_unknown_field_diagnostic_matches_snapshot()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile("type User { name: String }\nlet user = User { name: \"Ada\", age: 1 }", "test.axom");

        Assert.False(result.Success);

        var snapshot = Snapshots.Read("RecordUnknownField.snapshot.txt");
        var actual = Snapshots.Format(result.Diagnostics);
        Assert.Equal(snapshot, actual);
    }

    [Fact]
    public void Cli_check_diagnostics_match_snapshot()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"axom_cli_snapshot_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var filePath = Path.Combine(tempDir, "test.axom");
        File.WriteAllText(filePath, "print x");

        var originalDirectory = Directory.GetCurrentDirectory();
        var originalOut = Console.Out;
        var originalError = Console.Error;
        var output = new StringWriter(CultureInfo.InvariantCulture);
        var error = new StringWriter(CultureInfo.InvariantCulture);

        try
        {
            Directory.SetCurrentDirectory(tempDir);
            Console.SetOut(output);
            Console.SetError(error);

            var exitCode = Axom.Cli.Program.Main(new[] { "check", filePath, "--quiet" });

            Assert.Equal(1, exitCode);
            Assert.Equal(string.Empty, output.ToString());

            var snapshot = Snapshots.Read("CliCheckUndefinedVariable.snapshot.txt");
            var normalized = error.ToString().Replace(filePath, "test.axom", StringComparison.Ordinal);
            Assert.Equal(snapshot, normalized);
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
            Directory.SetCurrentDirectory(originalDirectory);
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }
}
