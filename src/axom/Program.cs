using Axom.Compiler;
using System.Diagnostics;

namespace Axom.Cli;

public class Program
{
    public static int Main(string[] args)
    {
        const string usage =
            "Usage: axom <build|run|check> <file.axom> [options]\n" +
            "\n" +
            "Options:\n" +
            "  --out <dir>   Override output directory (default: out)\n" +
            "  --quiet       Suppress non-error output\n" +
            "  --verbose     Include extra context\n" +
            "  --cache       Enable compilation cache\n" +
            "  --help, -h    Show usage\n" +
            "  --version     Show version\n" +
            "\n" +
            "Examples:\n" +
            "  axom check hello.axom\n" +
            "  axom build hello.axom --out out\n" +
            "  axom run hello.axom --cache\n";

        if (args.Length == 1)
        {
            if (args[0] == "--help" || args[0] == "-h")
            {
                Console.WriteLine(usage);
                return 0;
            }

            if (args[0] == "--version")
            {
                var version = typeof(Program).Assembly.GetName().Version?.ToString() ?? "0.0.0";
                Console.WriteLine($"axom {version}");
                return 0;
            }
        }

        if (args.Length < 2)
        {
            Console.Error.WriteLine(usage);
            return 1;
        }

        var command = args[0];
        var inputPath = args[1];

        if (command != "build" && command != "run" && command != "check")
        {
            Console.Error.WriteLine(usage);
            return 1;
        }

        var outputDir = "out";
        var quiet = false;
        var verbose = false;
        var useCache = false;

        for (var i = 2; i < args.Length; i++)
        {
            var argument = args[i];
            if (argument == "--out")
            {
                if (i + 1 >= args.Length)
                {
                    Console.Error.WriteLine(usage);
                    return 1;
                }

                outputDir = args[i + 1];
                if (string.IsNullOrWhiteSpace(outputDir))
                {
                    Console.Error.WriteLine(usage);
                    return 1;
                }

                i++;
                continue;
            }

            if (argument == "--quiet")
            {
                quiet = true;
                continue;
            }

            if (argument == "--verbose")
            {
                verbose = true;
                continue;
            }

            if (argument == "--cache")
            {
                useCache = true;
                continue;
            }

            Console.Error.WriteLine(usage);
            return 1;
        }

        if (quiet && verbose)
        {
            Console.Error.WriteLine(usage);
            return 1;
        }

        if (!File.Exists(inputPath))
        {
            Console.Error.WriteLine($"File not found: {inputPath}");
            return 1;
        }

        var source = File.ReadAllText(inputPath);

        var compiler = new CompilerDriver();
        var result = useCache
            ? compiler.CompileCached(source, inputPath, new CompilerCache())
            : compiler.Compile(source, inputPath);

        if (!result.Success)
        {
            foreach (var d in result.Diagnostics)
                Console.Error.WriteLine(d);

            return 1;
        }

        if (command == "check")
        {
            return 0;
        }

        Directory.CreateDirectory(outputDir);
        var outputPath = Path.Combine(outputDir, "Program.cs");
        File.WriteAllText(outputPath, result.GeneratedCode);

        if (command == "build")
        {
            if (!quiet)
            {
                if (verbose)
                {
                    Console.WriteLine($"Output: {outputPath}");
                }

                Console.WriteLine("Build succeeded.");
            }
            return 0;
        }

        // run command: compile and execute
        return RunGeneratedCode(outputDir);
    }

    static int RunGeneratedCode(string outputDir)
    {
        var tempDir = Path.Combine(outputDir, ".run");
        
        try
        {
            // Clean up previous run
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
            Directory.CreateDirectory(tempDir);

            // Create a temporary console project
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = "new console -n TempRun --force",
                    WorkingDirectory = tempDir,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                }
            };

            process.Start();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                Console.Error.WriteLine("Failed to create temporary project.");
                return 1;
            }

            // Copy generated Program.cs
            File.Copy(Path.Combine(outputDir, "Program.cs"), Path.Combine(tempDir, "TempRun", "Program.cs"), true);

            // Build and run
            var runProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = "run",
                    WorkingDirectory = Path.Combine(tempDir, "TempRun"),
                    UseShellExecute = false
                }
            };

            runProcess.Start();
            runProcess.WaitForExit();
            return runProcess.ExitCode;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error running code: {ex.Message}");
            return 1;
        }
        finally
        {
            // Cleanup is optional - comment out if you want to inspect generated code
            // if (Directory.Exists(tempDir))
            // {
            //     Directory.Delete(tempDir, true);
            // }
        }
    }
}
