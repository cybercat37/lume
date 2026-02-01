using System.Text;
using Axom.Compiler;

namespace Axom.Fuzz;

public class Program
{
    private const string Alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789 \t\n\r+-*/%(){};=\"_";

    public static int Main(string[] args)
    {
        var iterations = 1000;
        var maxLength = 128;
        var seed = Environment.TickCount;

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--iterations":
                    iterations = ReadIntArg(args, ref i, iterations);
                    break;
                case "--max-length":
                    maxLength = ReadIntArg(args, ref i, maxLength);
                    break;
                case "--seed":
                    seed = ReadIntArg(args, ref i, seed);
                    break;
                case "--help":
                case "-h":
                    Console.WriteLine("Usage: axom-fuzz [--iterations N] [--max-length N] [--seed N]");
                    return 0;
                default:
                    Console.Error.WriteLine("Usage: axom-fuzz [--iterations N] [--max-length N] [--seed N]");
                    return 1;
            }
        }

        var random = new Random(seed);
        var compiler = new CompilerDriver();

        foreach (var seedSource in LoadCorpus())
        {
            if (!TryCompile(compiler, seedSource, seed, -1))
            {
                return 1;
            }
        }

        for (var i = 0; i < iterations; i++)
        {
            var source = Generate(random, maxLength);
            if (!TryCompile(compiler, source, seed, i))
            {
                return 1;
            }
        }

        Console.WriteLine($"Fuzz complete: {iterations} iterations (seed {seed}).");
        return 0;
    }

    private static bool TryCompile(CompilerDriver compiler, string source, int seed, int iteration)
    {
        try
        {
            compiler.Compile(source, "fuzz.axom");
            return true;
        }
        catch (Exception ex)
        {
            var iterationText = iteration < 0 ? "seed" : $"iteration {iteration}";
            Console.Error.WriteLine($"Fuzz crash at {iterationText} (seed {seed}).");
            Console.Error.WriteLine(ex.ToString());
            Console.Error.WriteLine("Source:");
            Console.Error.WriteLine(source);
            return false;
        }
    }

    private static int ReadIntArg(string[] args, ref int index, int fallback)
    {
        if (index + 1 >= args.Length)
        {
            return fallback;
        }

        if (int.TryParse(args[index + 1], out var value))
        {
            index++;
            return value;
        }

        return fallback;
    }

    private static string Generate(Random random, int maxLength)
    {
        var length = random.Next(0, maxLength + 1);
        var builder = new StringBuilder(length);
        for (var i = 0; i < length; i++)
        {
            var c = Alphabet[random.Next(Alphabet.Length)];
            builder.Append(c);
        }

        return builder.ToString();
    }

    private static IEnumerable<string> LoadCorpus()
    {
        var directory = Path.Combine(AppContext.BaseDirectory, "Corpus");
        if (!Directory.Exists(directory))
        {
            return Array.Empty<string>();
        }

        return Directory.EnumerateFiles(directory, "*.axom")
            .OrderBy(path => path, StringComparer.Ordinal)
            .Select(File.ReadAllText);
    }
}
