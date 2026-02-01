using Axom.Compiler;

namespace Axom.Tests;

public class LargeFileSmokeTests
{
    [Fact]
    public void Compile_large_file_within_limit_succeeds()
    {
        var compiler = new CompilerDriver();
        var line = "print 1\n";
        var repeat = CompilerDriver.MaxSourceLength / line.Length / 4;
        var source = string.Concat(Enumerable.Repeat(line, repeat));

        var result = compiler.Compile(source, "test.axom");

        Assert.True(result.Success);
    }
}
