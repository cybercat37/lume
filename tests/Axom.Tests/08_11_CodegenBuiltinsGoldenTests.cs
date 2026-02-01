using Axom.Compiler;

public class CodegenBuiltinsGoldenTests
{
    [Fact]
    public void Compile_builtins_matches_expected_output()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile("print len(\"hi\")\nprint abs(-3)\nprint min(1, 2)\nprint max(2, 1)", "test.axom");

        Assert.True(result.Success);

        var expected = "using System;\n" +
                       "\n" +
                       "class Program\n" +
                       "{\n" +
                       "    static void Main()\n" +
                       "    {\n" +
                       "        Console.WriteLine(\"hi\".Length);\n" +
                       "        Console.WriteLine(Math.Abs(-3));\n" +
                       "        Console.WriteLine(Math.Min(1, 2));\n" +
                       "        Console.WriteLine(Math.Max(2, 1));\n" +
                       "    }\n" +
                       "}\n";

        Assert.Equal(expected, result.GeneratedCode);
    }
}
