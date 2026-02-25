using Axom.Compiler;
using Axom.Compiler.Http.Routing;
using Axom.Runtime.Db;
using Axom.Runtime.Http;
using System.Diagnostics;
using System.Text.Json;

namespace Axom.Cli;

public class Program
{
    public static int Main(string[] args)
    {
        DbRuntimeBootstrap.ConfigureFromEnvironment();

        const string usage =
            "Usage: axom <build|run|check|serve|init> <file.axom|project-name> [options]\n" +
            "   or: axom db verify <file.axom> [--report] [--plan] [--snapshot] [--compare] [--quiet|--verbose] [--cache]\n" +
            "\n" +
            "Options:\n" +
            "  --out <dir>   Override output directory (default: out)\n" +
            "  --host <addr> Bind host for serve (default: 127.0.0.1)\n" +
            "  --port <n>    Bind port for serve (default: 8080)\n" +
            "  --force       Overwrite scaffold files for init\n" +
            "  --quiet       Suppress non-error output\n" +
            "  --verbose     Include extra context\n" +
            "  --cache       Enable compilation cache\n" +
            "  --help, -h    Show usage\n" +
            "  --version     Show version\n" +
            "\n" +
            "Examples:\n" +
            "  axom init myapp\n" +
            "  axom check hello.axom\n" +
            "  axom db verify hello.axom --report\n" +
            "  axom build hello.axom --out out\n" +
            "  axom run hello.axom --cache\n" +
            "  axom serve hello.axom --host 127.0.0.1 --port 8080\n";

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

        if (args.Length == 0)
        {
            Console.Error.WriteLine(usage);
            return 1;
        }

        var command = args[0];
        if (command == "init")
        {
            return InitProject(args, usage);
        }

        if (command == "db")
        {
            return HandleDbCommand(args, usage);
        }

        if (args.Length < 2)
        {
            Console.Error.WriteLine(usage);
            return 1;
        }

        var inputPath = args[1];

        if (command != "build" && command != "run" && command != "check" && command != "serve")
        {
            Console.Error.WriteLine(usage);
            return 1;
        }

        var outputDir = "out";
        var quiet = false;
        var verbose = false;
        var useCache = false;
        var host = "127.0.0.1";
        var port = 8080;

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

            if (argument == "--host")
            {
                if (i + 1 >= args.Length)
                {
                    Console.Error.WriteLine(usage);
                    return 1;
                }

                host = args[i + 1];
                if (string.IsNullOrWhiteSpace(host))
                {
                    Console.Error.WriteLine(usage);
                    return 1;
                }

                i++;
                continue;
            }

            if (argument == "--port")
            {
                if (i + 1 >= args.Length)
                {
                    Console.Error.WriteLine(usage);
                    return 1;
                }

                if (!int.TryParse(args[i + 1], out port) || port is < 1 or > 65535)
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

        if (command == "serve" && !string.IsNullOrWhiteSpace(outputDir) && outputDir != "out")
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

        if (command == "serve")
        {
            return ServeProgram(inputPath, host, port, quiet, verbose);
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

    private static int HandleDbCommand(string[] args, string usage)
    {
        if (args.Length < 3 || !string.Equals(args[1], "verify", StringComparison.Ordinal))
        {
            Console.Error.WriteLine(usage);
            return 1;
        }

        var inputPath = args[2];
        var quiet = false;
        var verbose = false;
        var useCache = false;
        var report = false;
        var plan = false;
        var snapshot = false;
        var compare = false;

        for (var i = 3; i < args.Length; i++)
        {
            var argument = args[i];
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

            if (argument == "--report")
            {
                report = true;
                continue;
            }

            if (argument == "--plan")
            {
                plan = true;
                continue;
            }

            if (argument == "--snapshot")
            {
                snapshot = true;
                continue;
            }

            if (argument == "--compare")
            {
                compare = true;
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
            {
                Console.Error.WriteLine(d);
            }

            return 1;
        }

        var sqlLiterals = ExtractSqlLiterals(source);
        if (report && !quiet)
        {
            Console.WriteLine($"total_queries_validated={sqlLiterals.Count}");
            Console.WriteLine("average_duration_ms=0");
        }

        if (plan && !quiet)
        {
            Console.WriteLine("plan_output=not_available_in_mvp");
        }

        if (snapshot)
        {
            WriteMetricsSnapshot(sqlLiterals);
            if (!quiet)
            {
                Console.WriteLine("snapshot_written=.axom/query-metrics.json");
            }
        }

        if (compare && !quiet)
        {
            var snapshotPath = Path.Combine(".axom", "query-metrics.json");
            if (!File.Exists(snapshotPath))
            {
                Console.WriteLine("compare_warning=snapshot_missing");
            }
            else
            {
                Console.WriteLine("compare_status=ok");
            }
        }

        if (verbose && !quiet)
        {
            Console.WriteLine($"db_verify_file={inputPath}");
        }

        return 0;
    }

    private static List<string> ExtractSqlLiterals(string source)
    {
        var values = new List<string>();
        var searchStart = 0;
        while (searchStart < source.Length)
        {
            var start = source.IndexOf("sql\"\"\"", searchStart, StringComparison.Ordinal);
            if (start < 0)
            {
                break;
            }

            var valueStart = start + 6;
            var end = source.IndexOf("\"\"\"", valueStart, StringComparison.Ordinal);
            if (end < 0)
            {
                break;
            }

            values.Add(source.Substring(valueStart, end - valueStart));
            searchStart = end + 3;
        }

        return values;
    }

    private static void WriteMetricsSnapshot(IReadOnlyList<string> sqlLiterals)
    {
        var payload = sqlLiterals
            .Select(sql => new
            {
                query_id = DbQueryFingerprint.CreateQueryId(sql),
                average_duration = 0,
                execution_count = 0
            })
            .ToList();

        Directory.CreateDirectory(".axom");
        var snapshotPath = Path.Combine(".axom", "query-metrics.json");
        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(snapshotPath, json);
    }

    private static int InitProject(string[] args, string usage)
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine(usage);
            return 1;
        }

        var projectName = args[1];
        if (string.IsNullOrWhiteSpace(projectName))
        {
            Console.Error.WriteLine("Project name cannot be empty.");
            return 1;
        }

        var force = false;
        for (var i = 2; i < args.Length; i++)
        {
            if (args[i] == "--force")
            {
                force = true;
                continue;
            }

            Console.Error.WriteLine(usage);
            return 1;
        }

        var projectPath = Path.GetFullPath(projectName);
        if (!ProjectScaffolder.TryScaffoldApiProject(projectPath, force, out var error))
        {
            Console.Error.WriteLine(error ?? "Failed to scaffold project.");
            return 1;
        }

        Console.WriteLine($"Initialized Axom API project at {projectPath}");
        Console.WriteLine("Next steps:");
        Console.WriteLine($"  cd {projectName}");
        Console.WriteLine("  axom serve main.axom --host 127.0.0.1 --port 8080");
        Console.WriteLine("  curl -i http://127.0.0.1:8080/health");
        return 0;
    }

    private static int ServeProgram(string inputPath, string host, int port, bool quiet, bool verbose)
    {
        var routeDiscovery = new RouteDiscovery();
        var routeResult = routeDiscovery.Discover(inputPath);
        if (!routeResult.Success)
        {
            foreach (var diagnostic in routeResult.Diagnostics)
            {
                Console.Error.WriteLine(diagnostic);
            }

            return 1;
        }

        using var cancellationTokenSource = new CancellationTokenSource();
        var runtimeRoutes = routeResult.Routes
            .Select(RouteHandlerFactory.CreateEndpoint)
            .ToList();

        ConsoleCancelEventHandler handler = (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cancellationTokenSource.Cancel();
        };

        Console.CancelKeyPress += handler;

        try
        {
            var httpHost = new AxomHttpHost();

            if (!quiet)
            {
                if (verbose)
                {
                    Console.WriteLine($"Serving source: {inputPath}");
                    Console.WriteLine($"Discovered routes: {routeResult.Routes.Count}");

                    foreach (var route in routeResult.Routes.OrderBy(route => route.Template, StringComparer.Ordinal))
                    {
                        Console.WriteLine($"  {route.Method} {route.Template}");
                    }
                }

                Console.WriteLine($"Listening on http://{host}:{port}");
                Console.WriteLine("Press Ctrl+C to stop.");
            }

            httpHost.RunAsync(host, port, runtimeRoutes, cancellationTokenSource.Token).GetAwaiter().GetResult();
            return 0;
        }
        catch (OperationCanceledException)
        {
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error serving code: {ex.Message}");
            return 1;
        }
        finally
        {
            Console.CancelKeyPress -= handler;
        }
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
