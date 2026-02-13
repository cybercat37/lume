using Axom.Compiler;

public class CodegenTimeStdlibTests
{
    [Fact]
    public void Compile_time_now_add_diff_emit_datetimeoffset_calls()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile(
            "let now = time_now_utc()\nlet later = time_add_ms(now, 100)\nprint time_diff_ms(later, now)",
            "test.axom");

        Assert.True(result.Success);
        Assert.Contains("DateTimeOffset.UtcNow", result.GeneratedCode);
        Assert.Contains("AddMilliseconds", result.GeneratedCode);
        Assert.Contains("TotalMilliseconds", result.GeneratedCode);
    }

    [Fact]
    public void Compile_time_iso_helpers_emit_roundtrip_format_and_parse()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile("print time_to_iso(time_now_utc())\nprint time_to_local_iso(time_now_utc())\nprint time_from_iso(\"bad\")", "test.axom");

        Assert.True(result.Success);
        Assert.Contains("ToString(\"O\"", result.GeneratedCode);
        Assert.Contains("ToLocalTime()", result.GeneratedCode);
        Assert.Contains("DateTimeOffset.TryParse", result.GeneratedCode);
        Assert.Contains("AxomResult<DateTimeOffset>", result.GeneratedCode);
    }
}
