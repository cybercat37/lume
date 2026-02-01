using Axom.Compiler;

namespace Axom.Tests;

public class CodegenGoldenCoverageTests
{
    [Fact]
    public void Compile_literals_match_expected_output()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile("print 1\nprint true\nprint \"hi\"", "test.axom");

        Assert.True(result.Success);

        var expected = GoldenFiles.Read("CodegenLiteralsGoldenTests.golden.cs");
        Assert.Equal(expected, result.GeneratedCode);
    }

    [Fact]
    public void Compile_builtins_match_expected_output()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile("print len(\"hi\")\nprint abs(-3)\nprint min(1, 2)\nprint max(2, 1)", "test.axom");

        Assert.True(result.Success);

        var expected = GoldenFiles.Read("CodegenBuiltinsGoldenTests.golden.cs");
        Assert.Equal(expected, result.GeneratedCode);
    }
}
