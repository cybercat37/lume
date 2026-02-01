using Axom.Compiler;
using Axom.Tests;

public class CodegenGoldenTests
{
    [Fact]
    public void Compile_program_matches_expected_output()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile("let mut x = 1\n{\nprint \"hi\"\nx = 2\n}\nprint x", "test.axom");

        Assert.True(result.Success);

        var expected = GoldenFiles.Read("CodegenGoldenTests.golden.cs");

        Assert.Equal(expected, result.GeneratedCode);
    }
}
