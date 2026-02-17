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
    public async Task Http_require_range_accepts_2xx_status_window()
    {
        var host = new AxomHttpHost();
        using var cancellation = new CancellationTokenSource();
        var port = GetFreePort();
        var routes = new[]
        {
            new RouteEndpoint(
                "GET",
                "/created",
                "inline",
                (_, _) => Task.FromResult(AxomHttpResponse.Text(201, "created"))),
            new RouteEndpoint(
                "GET",
                "/health",
                "inline",
                (_, _) => Task.FromResult(AxomHttpResponse.Text(200, "ok")))
        };

        var runTask = host.RunAsync("127.0.0.1", port, routes, cancellation.Token);
        await WaitForHealthAsync(port);

        var sourceText = new SourceText(
            $"let sent = http(\"http://127.0.0.1:{port}\") |> get(\"/created\") |> send()\nprint match sent {{\n  Ok(resp) -> match require_range(resp, 200..299) {{\n    Ok(okResp) -> match response_text(okResp) {{\n      Ok(body) -> body\n      Error(err) -> str(err)\n    }}\n    Error(err) -> str(err)\n  }}\n  Error(err) -> str(err)\n}}",
            "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        var errors = result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        Assert.True(errors.Count == 0, string.Join(Environment.NewLine, errors.Select(error => error.ToString())));
        Assert.Equal("created", result.Output.Trim());

        cancellation.Cancel();
        await runTask.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Http_require_range_accepts_status_class_literal_sugar()
    {
        var host = new AxomHttpHost();
        using var cancellation = new CancellationTokenSource();
        var port = GetFreePort();
        var routes = new[]
        {
            new RouteEndpoint(
                "GET",
                "/accepted",
                "inline",
                (_, _) => Task.FromResult(AxomHttpResponse.Text(202, "accepted"))),
            new RouteEndpoint(
                "GET",
                "/health",
                "inline",
                (_, _) => Task.FromResult(AxomHttpResponse.Text(200, "ok")))
        };

        var runTask = host.RunAsync("127.0.0.1", port, routes, cancellation.Token);
        await WaitForHealthAsync(port);

        var sourceText = new SourceText(
            $"let sent = http(\"http://127.0.0.1:{port}\") |> get(\"/accepted\") |> send()\nprint match sent {{\n  Ok(resp) -> match require_range(resp, 2xx) {{\n    Ok(okResp) -> match response_text(okResp) {{\n      Ok(body) -> body\n      Error(err) -> str(err)\n    }}\n    Error(err) -> str(err)\n  }}\n  Error(err) -> str(err)\n}}",
            "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        var errors = result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        Assert.True(errors.Count == 0, string.Join(Environment.NewLine, errors.Select(error => error.ToString())));
        Assert.Equal("accepted", result.Output.Trim());

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
            "let client = http(\"http://example.test\") |> header(\"x-client\", \"yes\")\nlet req = client |> put(\"/users/1\", \"body\") |> header(\"x-test\", \"ok\") |> request_text(\"replaced\")\nlet req2 = client |> patch(\"/users/2\", \"patch\")\nlet req3 = client |> delete(\"/users/3\")\nprint req\nprint req2\nprint req3",
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

    [Fact]
    public async Task Http_json_and_accept_json_decorators_apply_headers_and_body()
    {
        AxomHttpRequest? observed = null;
        var host = new AxomHttpHost();
        using var cancellation = new CancellationTokenSource();
        var port = GetFreePort();
        var routes = new[]
        {
            new RouteEndpoint(
                "GET",
                "/health",
                "inline",
                (_, _) => Task.FromResult(AxomHttpResponse.Text(200, "ok from host"))),
            new RouteEndpoint(
                "POST",
                "/echo",
                "inline",
                (request, _) =>
                {
                    observed = request;
                    return Task.FromResult(AxomHttpResponse.Text(200, "ok"));
                })
        };

        var runTask = host.RunAsync("127.0.0.1", port, routes, cancellation.Token);
        await WaitForHealthAsync(port);

        var sourceText = new SourceText(
            $"let sent = http(\"http://127.0.0.1:{port}\") |> post(\"/echo\", \"seed\") |> json(\"{{\\\"x\\\":1}}\") |> accept_json() |> send()\nprint match sent {{\n  Ok(resp) -> \"ok\"\n  Error(err) -> str(err)\n}}",
            "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        var errors = result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        Assert.True(errors.Count == 0, string.Join(Environment.NewLine, errors.Select(error => error.ToString())));
        Assert.Equal("ok", result.Output.Trim());

        Assert.NotNull(observed);
        Assert.Equal("{\"x\":1}", observed!.Body);
        Assert.True(observed.Headers.TryGetValue("Content-Type", out var contentType));
        Assert.StartsWith("application/json", contentType, StringComparison.OrdinalIgnoreCase);
        Assert.True(observed.Headers.TryGetValue("Accept", out var accept));
        Assert.Equal("application/json", accept);

        cancellation.Cancel();
        await runTask.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Http_config_sugar_builds_client_used_by_request_pipeline()
    {
        var sourceText = new SourceText(
            "let client = http { baseUrl: \"http://example.test\", headers: [\"x-client\": \"yes\"], timeout: 1800, retry: 2 }\nlet request = client |> get(\"/users/1\")\nprint request",
            "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        var errors = result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        Assert.True(errors.Count == 0, string.Join(Environment.NewLine, errors.Select(error => error.ToString())));
        Assert.Equal("Request { method: GET, url: http://example.test/users/1 }", result.Output.Trim());
    }

    [Fact]
    public void Http_config_sugar_applies_retry_to_client_value()
    {
        var sourceText = new SourceText(
            "let client = http { baseUrl: \"http://example.test\", retry: 3 }\nprint client",
            "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        var errors = result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        Assert.True(errors.Count == 0, string.Join(Environment.NewLine, errors.Select(error => error.ToString())));
        Assert.Equal("Http { baseUrl: http://example.test, headers: 0, timeoutMs: 30000, retryMax: 3 }", result.Output.Trim());
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
