using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Axom.Runtime.Http;

public sealed class AxomHttpHost
{
    public async Task RunAsync(string host, int port, CancellationToken cancellationToken)
    {
        var app = BuildApp(host, port);

        await app.StartAsync(cancellationToken);

        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Graceful shutdown path.
        }
        finally
        {
            await app.StopAsync(CancellationToken.None);
            await app.DisposeAsync();
        }
    }

    private static WebApplication BuildApp(string host, int port)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls($"http://{host}:{port}");
        builder.Logging.ClearProviders();

        var app = builder.Build();

        app.MapGet("/health", () =>
        {
            return Results.Text("ok", "text/plain; charset=utf-8", statusCode: StatusCodes.Status200OK);
        });

        app.MapFallback(() => Results.NotFound());

        return app;
    }
}
