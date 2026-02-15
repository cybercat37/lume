using Axom.Compiler;

public class CodegenHttpClientBuiltinTests
{
    [Fact]
    public void Compile_http_client_builtins_emits_http_helpers()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile(
            "let request = http(\"http://127.0.0.1:8080\") |> get(\"/health\")\nlet response = send(request)\nprint response",
            "test.axom");

        Assert.True(result.Success);
        Assert.Contains("AxomHttpCreate", result.GeneratedCode);
        Assert.Contains("AxomHttpGet", result.GeneratedCode);
        Assert.Contains("AxomHttpSend", result.GeneratedCode);
        Assert.Contains("AxomHttpResponseValue", result.GeneratedCode);
    }
}
