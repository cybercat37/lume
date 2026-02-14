using Axom.Runtime.Http;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;

namespace Axom.Tests;

[Collection("CliTests")]
public class CliServeTests
{
    [Fact]
    public async Task Http_host_health_endpoint_returns_ok()
    {
        var host = new AxomHttpHost();
        using var cancellation = new CancellationTokenSource();
        var port = GetFreePort();

        var runTask = host.RunAsync("127.0.0.1", port, cancellation.Token);

        using var client = new HttpClient();
        var response = await WaitForHealthAsync(client, port);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("ok", body);

        cancellation.Cancel();
        await runTask.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Serve_with_invalid_port_fails()
    {
        var tempDir = CreateTempDirectory();
        var filePath = Path.Combine(tempDir, "test.axom");
        File.WriteAllText(filePath, "print 1");

        var originalOut = Console.Out;
        var originalError = Console.Error;
        var output = new StringWriter(CultureInfo.InvariantCulture);
        var error = new StringWriter(CultureInfo.InvariantCulture);

        try
        {
            Console.SetOut(output);
            Console.SetError(error);

            var exitCode = Axom.Cli.Program.Main(new[] { "serve", filePath, "--port", "70000" });

            Assert.Equal(1, exitCode);
            Assert.NotEqual(string.Empty, error.ToString());
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
            DeleteTempDirectory(tempDir);
        }
    }

    [Fact]
    public void Serve_with_compile_error_fails_before_starting_host()
    {
        var tempDir = CreateTempDirectory();
        var filePath = Path.Combine(tempDir, "test.axom");
        File.WriteAllText(filePath, "print \"unterminated");

        var originalOut = Console.Out;
        var originalError = Console.Error;
        var output = new StringWriter(CultureInfo.InvariantCulture);
        var error = new StringWriter(CultureInfo.InvariantCulture);

        try
        {
            Console.SetOut(output);
            Console.SetError(error);

            var exitCode = Axom.Cli.Program.Main(new[] { "serve", filePath });

            Assert.Equal(1, exitCode);
            Assert.Contains("error", error.ToString(), StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
            DeleteTempDirectory(tempDir);
        }
    }

    [Fact]
    public void Serve_with_missing_file_fails()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), $"axom_missing_{Guid.NewGuid():N}.axom");

        var originalOut = Console.Out;
        var originalError = Console.Error;
        var output = new StringWriter(CultureInfo.InvariantCulture);
        var error = new StringWriter(CultureInfo.InvariantCulture);

        try
        {
            Console.SetOut(output);
            Console.SetError(error);

            var exitCode = Axom.Cli.Program.Main(new[] { "serve", missingPath });

            Assert.Equal(1, exitCode);
            Assert.Contains("File not found", error.ToString(), StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }

    private static async Task<HttpResponseMessage> WaitForHealthAsync(HttpClient client, int port)
    {
        var url = $"http://127.0.0.1:{port}/health";
        var timeoutAt = DateTime.UtcNow.AddSeconds(5);

        while (DateTime.UtcNow < timeoutAt)
        {
            try
            {
                var response = await client.GetAsync(url);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return response;
                }
            }
            catch (HttpRequestException)
            {
                // Server not ready yet.
            }

            await Task.Delay(50);
        }

        throw new TimeoutException("Timed out waiting for /health endpoint.");
    }

    private static int GetFreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static string CreateTempDirectory()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"axom_cli_serve_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        return tempDir;
    }

    private static void DeleteTempDirectory(string tempDir)
    {
        if (Directory.Exists(tempDir))
        {
            Directory.Delete(tempDir, true);
        }
    }
}
