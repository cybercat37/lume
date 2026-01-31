using System.Globalization;

namespace Lume.Tests;

[Collection("CliTests")]
public class CliExitCodeTests
{
    [Fact]
    public void Unknown_command_returns_error_and_usage()
    {
        var originalOut = Console.Out;
        var originalError = Console.Error;
        var output = new StringWriter(CultureInfo.InvariantCulture);
        var error = new StringWriter(CultureInfo.InvariantCulture);

        try
        {
            Console.SetOut(output);
            Console.SetError(error);

            var exitCode = Lume.Cli.Program.Main(new[] { "nope", "file.lume" });

            Assert.Equal(1, exitCode);
            Assert.Equal(string.Empty, output.ToString());
            Assert.Contains("Usage: lume <build|run|check>", error.ToString());
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }

    [Fact]
    public void Missing_file_returns_error()
    {
        var originalOut = Console.Out;
        var originalError = Console.Error;
        var output = new StringWriter(CultureInfo.InvariantCulture);
        var error = new StringWriter(CultureInfo.InvariantCulture);

        try
        {
            Console.SetOut(output);
            Console.SetError(error);

            var exitCode = Lume.Cli.Program.Main(new[] { "check", "missing.lume" });

            Assert.Equal(1, exitCode);
            Assert.Equal(string.Empty, output.ToString());
            Assert.Contains("File not found:", error.ToString());
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }
}
