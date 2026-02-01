using Axom.Compiler;

public class CodegenInputGoldenTests
{
    [Fact]
    public void Compile_input_and_println_matches_expected_output()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile("println input", "test.axom");

        Assert.True(result.Success);

        var expected = "using System;\n" +
                       "\n" +
                       "class Program\n" +
                       "{\n" +
                       "    static void Main()\n" +
                       "    {\n" +
                       "        Console.WriteLine(Console.ReadLine());\n" +
                       "    }\n" +
                       "}\n";

        Assert.Equal(expected, result.GeneratedCode);
    }
}
