using Lume.Compiler;
using System.Diagnostics;

namespace Lume.Cli;

class Program
{
    static int Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine("Usage: lume <build|run> <file.lume>");
            return 1;
        }

        var command = args[0];
        var inputPath = args[1];

        if (command != "build" && command != "run")
        {
            Console.Error.WriteLine("Usage: lume <build|run> <file.lume>");
            return 1;
        }

        if (!File.Exists(inputPath))
        {
            Console.Error.WriteLine($"File not found: {inputPath}");
            return 1;
        }

        var source = File.ReadAllText(inputPath);

        var compiler = new CompilerDriver();
        var result = compiler.Compile(source, inputPath);

        if (!result.Success)
        {
            foreach (var d in result.Diagnostics)
                Console.Error.WriteLine(d);

            return 1;
        }

        Directory.CreateDirectory("out");
        File.WriteAllText("out/Program.cs", result.GeneratedCode);

        if (command == "build")
        {
            Console.WriteLine("Build succeeded.");
            return 0;
        }

        // run command: compile and execute
        return RunGeneratedCode();
    }

    static int RunGeneratedCode()
    {
        var tempDir = Path.Combine("out", ".run");
        
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
            File.Copy("out/Program.cs", Path.Combine(tempDir, "TempRun", "Program.cs"), true);

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
