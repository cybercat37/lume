using Axom.Compiler;

public class CodegenRandomBuiltinTests
{
    [Fact]
    public void Compile_random_builtins_emits_runtime_helpers()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile("rand_seed(1)\nprint rand_float()\nprint rand_int(10)\nsleep(1)", "test.axom");

        Assert.True(result.Success);
        Assert.Contains("AxomRandFloat", result.GeneratedCode);
        Assert.Contains("AxomRandInt", result.GeneratedCode);
        Assert.Contains("AxomRandSeed", result.GeneratedCode);
        Assert.Contains("Thread.Sleep", result.GeneratedCode);
    }
}
