using Axom.Compiler;

public class CodegenStringifyPrintTests
{
    [Fact]
    public void Compile_print_list_uses_axom_stringify()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile("print take([1, 2, 3], 2)", "test.axom");

        Assert.True(result.Success);
        Assert.Contains("Console.WriteLine(AxomStringify", result.GeneratedCode);
        Assert.Contains("static string AxomStringify", result.GeneratedCode);
    }

    [Fact]
    public void Compile_print_tuple_uses_axom_stringify_tuple_helper()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile("print enumerate([10, 20])", "test.axom");

        Assert.True(result.Success);
        Assert.Contains("AxomStringifyTuple", result.GeneratedCode);
    }

    [Fact]
    public void Compile_print_map_uses_axom_stringify_map_helper()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile("print [\"a\": 1, \"b\": 2]", "test.axom");

        Assert.True(result.Success);
        Assert.Contains("AxomStringifyMap", result.GeneratedCode);
    }

    [Fact]
    public void Compile_print_int_keeps_plain_console_write_line()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile("print 42", "test.axom");

        Assert.True(result.Success);
        Assert.Contains("Console.WriteLine(42);", result.GeneratedCode);
    }
}
