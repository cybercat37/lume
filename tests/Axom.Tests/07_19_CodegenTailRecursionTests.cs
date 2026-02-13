using Axom.Compiler;

public class CodegenTailRecursionTests
{
    [Fact]
    public void Compile_tail_recursive_sum_emits_loop_instead_of_recursive_return()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile(
            "fn tail_sum(n: Int, acc: Int) -> Int => match n {\n  0 -> acc\n  _ -> tail_sum(n - 1, acc + n)\n}\nprint tail_sum(10, 0)",
            "test.axom");

        Assert.True(result.Success);
        Assert.Contains("static int tail_sum", result.GeneratedCode);
        Assert.Contains("while (true)", result.GeneratedCode);
        Assert.Contains("continue;", result.GeneratedCode);
    }
}
