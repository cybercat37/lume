using Lume.Compiler;
using Lume.Tests;

public class FunctionCodegenGoldenTests
{
    [Fact]
    public void Compile_function_and_lambda_matches_expected_output()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile(@"
fn add(x: Int, y: Int) => x + y
let f = fn(x: Int) => x + 1
print add(1, 2)
print f(2)
print (fn(x: Int) => x + 1)(3)
print input()
", "test.lume");

        Assert.True(result.Success);

        var expected = GoldenFiles.Read("FunctionCodegenGoldenTests.golden.cs");

        Assert.Equal(expected, result.GeneratedCode);
    }
}
