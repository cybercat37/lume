using Lume.Compiler;

namespace Lume.Cli;

class Program
{
    static int Main(string[] args)
    {
        if (args.Length < 2 || args[0] != "build")
        {
            Console.Error.WriteLine("Usage: lume build <file.lume>");
            return 1;
        }

        var inputPath = args[1];

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

        Console.WriteLine("Build succeeded.");
        return 0;
    }
}
