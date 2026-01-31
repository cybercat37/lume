using Lume.Compiler;

namespace Lume.Tests;

public class SourceLengthGuardTests
{
    [Fact]
    public void Compile_rejects_large_source_with_diagnostic()
    {
        var compiler = new CompilerDriver();
        var source = new string('a', CompilerDriver.MaxSourceLength + 1);
        var result = compiler.Compile(source, "test.lume");

        Assert.False(result.Success);
        Assert.Contains(result.Diagnostics, d => d.Message.Contains("Source file exceeds max length"));
    }
}
