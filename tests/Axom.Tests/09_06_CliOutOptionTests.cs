using System.Globalization;

namespace Axom.Tests;

[Collection("CliTests")]
public class CliOutOptionTests
{
    [Fact]
    public void Build_writes_output_to_custom_directory()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"axom_cli_out_{Guid.NewGuid():N}");
        var outputDir = Path.Combine(tempDir, "custom");
        Directory.CreateDirectory(tempDir);
        var filePath = Path.Combine(tempDir, "test.axom");
        File.WriteAllText(filePath, "print 1");

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

            var exitCode = Axom.Cli.Program.Main(new[] { "build", filePath, "--out", outputDir });

            Assert.Equal(0, exitCode);
            Assert.Contains("Build succeeded.", output.ToString());
            Assert.Equal(string.Empty, error.ToString());
            Assert.True(File.Exists(Path.Combine(outputDir, "Program.cs")));
            Assert.False(File.Exists(Path.Combine(tempDir, "out", "Program.cs")));
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

    [Fact]
    public void Run_uses_custom_output_directory()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"axom_cli_out_{Guid.NewGuid():N}");
        var outputDir = Path.Combine(tempDir, "custom");
        Directory.CreateDirectory(tempDir);
        var filePath = Path.Combine(tempDir, "test.axom");
        File.WriteAllText(filePath, "print 1");

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

            var exitCode = Axom.Cli.Program.Main(new[] { "run", filePath, "--out", outputDir });

            Assert.Equal(0, exitCode);
            Assert.Equal(string.Empty, error.ToString());
            Assert.True(File.Exists(Path.Combine(outputDir, "Program.cs")));
            Assert.False(File.Exists(Path.Combine(tempDir, "out", "Program.cs")));
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
