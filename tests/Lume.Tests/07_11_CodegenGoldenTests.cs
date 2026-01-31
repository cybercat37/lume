using Lume.Compiler;

public class CodegenGoldenTests
{
    [Fact]
    public void Compile_program_matches_expected_output()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile("let mut x = 1\n{\nprint \"hi\"\nx = 2\n}\nprint x", "test.lume");

        Assert.True(result.Success);

        var expected = "using System;\n" +
                       "\n" +
                       "class Program\n" +
                       "{\n" +
                       "    static void Main()\n" +
                       "    {\n" +
                       "        var x = 1;\n" +
                       "        {\n" +
                       "            Console.WriteLine(\"hi\");\n" +
                       "            x = 2;\n" +
                       "        }\n" +
                       "        Console.WriteLine(x);\n" +
                       "    }\n" +
                       "}\n";

        Assert.Equal(expected, result.GeneratedCode);
    }
}
