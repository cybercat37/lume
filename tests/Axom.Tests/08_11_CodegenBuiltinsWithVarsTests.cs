using Axom.Compiler;

public class CodegenBuiltinsWithVarsTests
{
    [Fact]
    public void Compile_builtins_with_vars_matches_expected_output()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile("let x = \"hey\"\nprint len(x)", "test.axom");

        Assert.True(result.Success);

        var expected = "using System;\n" +
                       "\n" +
                       "class Program\n" +
                       "{\n" +
                       "    static void Main()\n" +
                       "    {\n" +
                       "        var x = \"hey\";\n" +
                       "        Console.WriteLine(x.Length);\n" +
                       "    }\n" +
                       "}\n";

        Assert.Equal(expected, result.GeneratedCode);
    }
}
