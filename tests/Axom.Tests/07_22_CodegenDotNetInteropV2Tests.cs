using Axom.Compiler;

public class CodegenDotNetInteropV2Tests
{
    [Fact]
    public void Compile_dotnet_call_string_instance_method_generates_interop_runtime()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile("print dotnet.call<Bool>(\"System.String\", \"Contains\", \"axom\", \"xo\")", "test.axom");

        Assert.True(result.Success);
        Assert.Contains("BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance", result.GeneratedCode);
        Assert.Contains("\"System.String\"", result.GeneratedCode);
    }

    [Fact]
    public void Compile_dotnet_call_convert_method_generates_whitelist_entries()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile("print dotnet.call<Int>(\"System.Convert\", \"ToInt32\", \"42\")", "test.axom");

        Assert.True(result.Success);
        Assert.Contains("\"System.Convert\"", result.GeneratedCode);
        Assert.Contains("ToInt32", result.GeneratedCode);
    }
}
