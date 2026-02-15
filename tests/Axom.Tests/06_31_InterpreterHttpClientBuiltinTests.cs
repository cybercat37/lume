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
