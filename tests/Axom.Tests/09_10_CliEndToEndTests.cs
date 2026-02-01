using System.Globalization;

namespace Axom.Tests;

[Collection("CliTests")]
public class CliEndToEndTests
{
    [Fact]
    public void Build_then_run_produces_expected_output()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"lume_cli_e2e_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var filePath = Path.Combine(tempDir, "test.axom");
        File.WriteAllText(filePath, "println 1");

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

            var buildExit = Axom.Cli.Program.Main(new[] { "build", filePath, "--quiet" });
            var runExit = Axom.Cli.Program.Main(new[] { "run", filePath, "--quiet" });

            Assert.Equal(0, buildExit);
            Assert.Equal(0, runExit);
            Assert.Equal(string.Empty, error.ToString());
            Assert.True(File.Exists(Path.Combine(tempDir, "out", "Program.cs")));
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
