using Axom.Compiler.Diagnostics;
using Axom.Compiler.Interpreting;
using Axom.Compiler.Parsing;
using Axom.Compiler.Text;
using Axom.Runtime.Http;

public class InterpreterHttpClientBuiltinTests
{
    [Fact]
    public async Task Http_send_require_and_response_text_work_end_to_end()
    {
        var host = new AxomHttpHost();
        using var cancellation = new CancellationTokenSource();
        var port = GetFreePort();
        var routes = new[]
        {
            new RouteEndpoint(
                "GET",
                "/health",
                "inline",
                (_, _) => Task.FromResult(AxomHttpResponse.Text(200, "ok from host")))
        };

        var runTask = host.RunAsync("127.0.0.1", port, routes, cancellation.Token);
        await WaitForHealthAsync(port);

        var sourceText = new SourceText(
            $"let sent = http(\"http://127.0.0.1:{port}\") |> get(\"/health\") |> send()\nprint match sent {{\n  Ok(resp) -> match require(resp, 200) {{\n    Ok(okResp) -> match response_text(okResp) {{\n      Ok(body) -> body\n      Error(err) -> str(err)\n    }}\n    Error(err) -> str(err)\n  }}\n  Error(err) -> str(err)\n}}",
            "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        var errors = result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        Assert.True(errors.Count == 0, string.Join(Environment.NewLine, errors.Select(error => error.ToString())));

        Assert.Equal("ok from host", result.Output.Trim());

        cancellation.Cancel();
        await runTask.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Http_send_returns_typed_invalid_url_error_variant()
    {
        var sourceText = new SourceText(
            "let sent = http(\"not-a-url\") |> get(\"/health\") |> send()\nprint match sent {\n  Ok(resp) -> \"ok\"\n  Error(err) -> match err {\n    InvalidUrl(msg) -> msg\n    Timeout(msg) -> msg\n    NetworkError(msg) -> msg\n    StatusError(msg) -> msg\n  }\n}",
            "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        var errors = result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        Assert.True(errors.Count == 0, string.Join(Environment.NewLine, errors.Select(error => error.ToString())));
        Assert.Equal("invalid url: not-a-url/health", result.Output.Trim());
    }

    [Fact]
    public void Http_extended_verb_and_request_decorator_builtins_produce_requests()
    {
        var sourceText = new SourceText(
            "let client = http(\"http://example.test\")\nlet req = client |> put(\"/users/1\", \"body\") |> request_header(\"x-test\", \"ok\") |> request_text(\"replaced\")\nlet req2 = client |> patch(\"/users/2\", \"patch\")\nlet req3 = client |> delete(\"/users/3\")\nprint req\nprint req2\nprint req3",
            "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        var errors = result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        Assert.True(errors.Count == 0, string.Join(Environment.NewLine, errors.Select(error => error.ToString())));

        var lines = result.Output
            .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        Assert.Equal("Request { method: PUT, url: http://example.test/users/1 }", lines[0]);
        Assert.Equal("Request { method: PATCH, url: http://example.test/users/2 }", lines[1]);
        Assert.Equal("Request { method: DELETE, url: http://example.test/users/3 }", lines[2]);
    }

    private static async Task WaitForHealthAsync(int port)
    {
        using var client = new HttpClient();
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(5);
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                var response = await client.GetAsync($"http://127.0.0.1:{port}/health");
                if (response.IsSuccessStatusCode)
                {
                    return;
                }
            }
            catch
            {
            }

            await Task.Delay(50);
        }

        throw new TimeoutException("Timed out waiting for /health endpoint.");
    }

    private static int GetFreePort()
    {
        using var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}
