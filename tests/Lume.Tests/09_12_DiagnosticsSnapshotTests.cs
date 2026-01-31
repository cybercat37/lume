using Lume.Compiler;
using System.Globalization;

namespace Lume.Tests;

public class DiagnosticsSnapshotTests
{
    [Fact]
    public void Parser_diagnostics_match_snapshot()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile("print", "test.lume");

        Assert.False(result.Success);

        var snapshot = Snapshots.Read("ParserUnexpectedToken.snapshot.txt");
        var actual = Snapshots.Format(result.Diagnostics);
        Assert.Equal(snapshot, actual);
    }

    [Fact]
    public void Parser_missing_close_paren_matches_snapshot()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile("print (1 +", "test.lume");

        Assert.False(result.Success);

        var snapshot = Snapshots.Read("ParserMissingCloseParen.snapshot.txt");
        var actual = Snapshots.Format(result.Diagnostics);
        Assert.Equal(snapshot, actual);
    }

    [Fact]
    public void Parser_unexpected_token_in_block_matches_snapshot()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile("{\n)\n}", "test.lume");

        Assert.False(result.Success);

        var snapshot = Snapshots.Read("ParserUnexpectedTokenInBlock.snapshot.txt");
        var actual = Snapshots.Format(result.Diagnostics);
        Assert.Equal(snapshot, actual);
    }

    [Fact]
    public void Binder_undefined_variable_matches_snapshot()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile("print x", "test.lume");

        Assert.False(result.Success);

        var snapshot = Snapshots.Read("BinderUndefinedVariable.snapshot.txt");
        var actual = Snapshots.Format(result.Diagnostics);
        Assert.Equal(snapshot, actual);
    }

    [Fact]
    public void Binder_immutable_assignment_matches_snapshot()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile("let x = 1\nx = 2", "test.lume");

        Assert.False(result.Success);

        var snapshot = Snapshots.Read("BinderImmutableAssignment.snapshot.txt");
        var actual = Snapshots.Format(result.Diagnostics);
        Assert.Equal(snapshot, actual);
    }

    [Fact]
    public void Type_mismatch_assignment_matches_snapshot()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile("let mut x = 1\nx = true", "test.lume");

        Assert.False(result.Success);

        var snapshot = Snapshots.Read("TypeMismatchAssignment.snapshot.txt");
        var actual = Snapshots.Format(result.Diagnostics);
        Assert.Equal(snapshot, actual);
    }

    [Fact]
    public void Cli_check_diagnostics_match_snapshot()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"lume_cli_snapshot_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var filePath = Path.Combine(tempDir, "test.lume");
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

            var exitCode = Lume.Cli.Program.Main(new[] { "check", filePath, "--quiet" });

            Assert.Equal(1, exitCode);
            Assert.Equal(string.Empty, output.ToString());

            var snapshot = Snapshots.Read("CliCheckUndefinedVariable.snapshot.txt");
            var normalized = error.ToString().Replace(filePath, "test.lume", StringComparison.Ordinal);
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
