using Axom.Compiler;

public class CodegenHttpClientBuiltinTests
{
    [Fact]
    public void Compile_http_client_builtins_emits_http_helpers()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile(
            "let client = http(\"http://127.0.0.1:8080\") |> header(\"x-client\", \"yes\")\nlet request = client |> put(\"/health\", \"body\") |> header(\"x-test\", \"ok\") |> request_text(\"override\") |> json(\"{\\\"ok\\\":true}\") |> accept_json()\nlet request2 = client |> patch(\"/health\", \"patch\")\nlet request3 = client |> delete(\"/health\")\nlet request4 = client |> get(\"/health\")\nlet response = send(request)\nprint response\nprint request2\nprint request3\nprint request4",
            "test.axom");

        Assert.True(result.Success);
        Assert.Contains("AxomHttpCreate", result.GeneratedCode);
        Assert.Contains("AxomHttpGet", result.GeneratedCode);
        Assert.Contains("AxomHttpPut", result.GeneratedCode);
        Assert.Contains("AxomHttpPatch", result.GeneratedCode);
        Assert.Contains("AxomHttpDelete", result.GeneratedCode);
        Assert.Contains("AxomHttpRequestHeader", result.GeneratedCode);
        Assert.Contains("AxomHttpRequestText", result.GeneratedCode);
        Assert.Contains("AxomHttpRequestJson", result.GeneratedCode);
        Assert.Contains("AxomHttpAcceptJson", result.GeneratedCode);
        Assert.Contains("AxomHttpSend", result.GeneratedCode);
        Assert.Contains("AxomHttpResponseValue", result.GeneratedCode);
        Assert.Contains("AxomHttpErrorValue", result.GeneratedCode);
    }

    [Fact]
    public void Compile_http_config_sugar_emits_http_create_and_decorators()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile(
            "let client = http { baseUrl: \"http://127.0.0.1:8080\", headers: [\"x-client\": \"yes\"], timeout: 1500, retry: 2 }\nlet request = client |> get(\"/health\")\nprint request",
            "test.axom");

        Assert.True(result.Success);
        Assert.Contains("AxomHttpCreate", result.GeneratedCode);
        Assert.Contains("AxomHttpHeader", result.GeneratedCode);
        Assert.Contains("AxomHttpTimeout", result.GeneratedCode);
        Assert.Contains("AxomHttpRetry", result.GeneratedCode);
    }

    [Fact]
    public void Compile_http_status_range_require_emits_status_range_helpers()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile(
            "let request = http(\"http://127.0.0.1:8080\") |> get(\"/health\")\nlet sent = send(request)\nprint match sent {\n  Ok(resp) -> require_range(resp, 200..299)\n  Error(_) -> sent\n}",
            "test.axom");

        Assert.True(result.Success);
        Assert.Contains("AxomStatusRange", result.GeneratedCode);
        Assert.Contains("AxomHttpRequireRange", result.GeneratedCode);
    }

    [Fact]
    public void Compile_http_status_class_literal_emits_status_range_call()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile(
            "let request = http(\"http://127.0.0.1:8080\") |> get(\"/health\")\nlet sent = send(request)\nprint match sent {\n  Ok(resp) -> require_range(resp, 2xx)\n  Error(_) -> sent\n}",
            "test.axom");

        Assert.True(result.Success);
        Assert.Contains("AxomStatusRange(200, 299)", result.GeneratedCode);
        Assert.Contains("AxomHttpRequireRange", result.GeneratedCode);
    }
}
