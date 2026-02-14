using Axom.Compiler.Http.Routing;
using Axom.Cli;
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
    public async Task Http_host_executes_axom_route_handler()
    {
        var host = new AxomHttpHost();
        using var cancellation = new CancellationTokenSource();
        var port = GetFreePort();
        var tempDir = CreateTempDirectory();

        try
        {
            var routeFile = Path.Combine(tempDir, "users__id_int_get.axom");
            File.WriteAllText(routeFile, "print match route_param_int(\"id\") {\n  Ok(id) -> id\n  Error(_) -> -1\n}\nprint \"user by id route\"");
            var routeDefinition = new RouteDefinition(
                "GET",
                "/users/:id<int>",
                routeFile,
                new[]
                {
                    RouteSegment.Static("users"),
                    RouteSegment.Dynamic("id", "int")
                });
            var routes = new[]
            {
                RouteHandlerFactory.CreateEndpoint(routeDefinition)
            };

            var runTask = host.RunAsync("127.0.0.1", port, routes, cancellation.Token);
            await Task.Delay(100);
            if (runTask.IsFaulted)
            {
                throw runTask.Exception;
            }

            using var client = new HttpClient();
            var timeoutAt = DateTime.UtcNow.AddSeconds(5);
            HttpResponseMessage? response = null;
            while (DateTime.UtcNow < timeoutAt)
            {
                try
                {
                    response = await client.GetAsync($"http://127.0.0.1:{port}/users/42");
                    break;
                }
                catch (HttpRequestException)
                {
                    if (runTask.IsFaulted)
                    {
                        throw runTask.Exception;
                    }

                    await Task.Delay(50);
                }
            }

            Assert.NotNull(response);
            var body = await response!.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("42\nuser by id route", body);

            cancellation.Cancel();
            await runTask.WaitAsync(TimeSpan.FromSeconds(5));
        }
        finally
        {
            DeleteTempDirectory(tempDir);
        }
    }

    [Fact]
    public async Task Http_host_uses_respond_status_and_body_from_handler()
    {
        var host = new AxomHttpHost();
        using var cancellation = new CancellationTokenSource();
        var port = GetFreePort();
        var tempDir = CreateTempDirectory();

        try
        {
            var routeFile = Path.Combine(tempDir, "users__id_int_get.axom");
            File.WriteAllText(routeFile, "respond(404, \"user not found\")");
            var routeDefinition = new RouteDefinition(
                "GET",
                "/users/:id<int>",
                routeFile,
                new[]
                {
                    RouteSegment.Static("users"),
                    RouteSegment.Dynamic("id", "int")
                });
            var routes = new[]
            {
                RouteHandlerFactory.CreateEndpoint(routeDefinition)
            };

            var runTask = host.RunAsync("127.0.0.1", port, routes, cancellation.Token);

            using var client = new HttpClient();
            var timeoutAt = DateTime.UtcNow.AddSeconds(5);
            HttpResponseMessage? response = null;
            while (DateTime.UtcNow < timeoutAt)
            {
                try
                {
                    response = await client.GetAsync($"http://127.0.0.1:{port}/users/42");
                    break;
                }
                catch (HttpRequestException)
                {
                    if (runTask.IsFaulted)
                    {
                        throw runTask.Exception;
                    }

                    await Task.Delay(50);
                }
            }

            Assert.NotNull(response);
            var body = await response!.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal("user not found", body);

            cancellation.Cancel();
            await runTask.WaitAsync(TimeSpan.FromSeconds(5));
        }
        finally
        {
            DeleteTempDirectory(tempDir);
        }
    }

    [Fact]
    public async Task Http_host_exposes_request_context_to_handler()
    {
        var host = new AxomHttpHost();
        using var cancellation = new CancellationTokenSource();
        var port = GetFreePort();
        var tempDir = CreateTempDirectory();

        try
        {
            var routeFile = Path.Combine(tempDir, "users__id_int_get.axom");
            File.WriteAllText(routeFile, "print request_method()\nprint request_path()");
            var routeDefinition = new RouteDefinition(
                "GET",
                "/users/:id<int>",
                routeFile,
                new[]
                {
                    RouteSegment.Static("users"),
                    RouteSegment.Dynamic("id", "int")
                });
            var routes = new[]
            {
                RouteHandlerFactory.CreateEndpoint(routeDefinition)
            };

            var runTask = host.RunAsync("127.0.0.1", port, routes, cancellation.Token);

            using var client = new HttpClient();
            var timeoutAt = DateTime.UtcNow.AddSeconds(5);
            HttpResponseMessage? response = null;
            while (DateTime.UtcNow < timeoutAt)
            {
                try
                {
                    response = await client.GetAsync($"http://127.0.0.1:{port}/users/42");
                    break;
                }
                catch (HttpRequestException)
                {
                    if (runTask.IsFaulted)
                    {
                        throw runTask.Exception;
                    }

                    await Task.Delay(50);
                }
            }

            Assert.NotNull(response);
            var body = await response!.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("GET\n/users/42", body);

            cancellation.Cancel();
            await runTask.WaitAsync(TimeSpan.FromSeconds(5));
        }
        finally
        {
            DeleteTempDirectory(tempDir);
        }
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

    [Fact]
    public void Serve_with_route_conflict_fails_before_starting_host()
    {
        var tempDir = CreateTempDirectory();
        var appDir = Path.Combine(tempDir, "app");
        var routesDir = Path.Combine(appDir, "routes");
        Directory.CreateDirectory(routesDir);

        var filePath = Path.Combine(appDir, "main.axom");
        File.WriteAllText(filePath, "print 1");
        File.WriteAllText(Path.Combine(routesDir, "users_me_get.axom"), "print 1");
        File.WriteAllText(Path.Combine(routesDir, "users__id_get.axom"), "print 1");

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
            Assert.Contains("Route conflict", error.ToString(), StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
            DeleteTempDirectory(tempDir);
        }
    }

    private static async Task<HttpResponseMessage> WaitForHealthAsync(HttpClient client, int port)
    {
        var url = $"http://127.0.0.1:{port}/health";
        return await WaitForAsync(client, url);
    }

    private static async Task<HttpResponseMessage> WaitForAsync(HttpClient client, string url)
    {
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
