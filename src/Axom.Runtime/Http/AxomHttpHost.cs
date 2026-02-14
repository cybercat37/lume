using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Axom.Runtime.Http;

public sealed class AxomHttpHost
{
    public Task RunAsync(string host, int port, CancellationToken cancellationToken)
    {
        return RunAsync(host, port, Array.Empty<RouteEndpoint>(), cancellationToken);
    }

    public async Task RunAsync(
        string host,
        int port,
        IReadOnlyList<RouteEndpoint> routes,
        CancellationToken cancellationToken)
    {
        var app = BuildApp(host, port, routes);

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

    private static WebApplication BuildApp(string host, int port, IReadOnlyList<RouteEndpoint> routes)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls($"http://{host}:{port}");
        builder.Logging.ClearProviders();

        var app = builder.Build();

        app.MapGet("/health", () =>
        {
            return Results.Text("ok", "text/plain; charset=utf-8", statusCode: StatusCodes.Status200OK);
        });

        foreach (var route in routes)
        {
            var aspPattern = ToAspNetRoutePattern(route.Template);
            app.MapMethods(aspPattern, new[] { route.Method }, (HttpContext context) =>
            {
                var routeValues = context.Request.RouteValues
                    .Where(entry => entry.Value is not null)
                    .Select(entry => $"{entry.Key}={entry.Value}")
                    .ToArray();

                var suffix = routeValues.Length == 0
                    ? string.Empty
                    : $" params{{{string.Join(',', routeValues)}}}";

                var payload = $"stub route matched: {route.Method} {route.Template}{suffix}";
                return Results.Text(payload, "text/plain; charset=utf-8", statusCode: StatusCodes.Status200OK);
            });
        }

        app.MapFallback(() => Results.NotFound());

        return app;
    }

    private static string ToAspNetRoutePattern(string template)
    {
        if (template == "/")
        {
            return "/";
        }

        var segments = template
            .Trim('/')
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Select(ConvertSegment)
            .ToArray();

        return "/" + string.Join('/', segments);
    }

    private static string ConvertSegment(string segment)
    {
        if (!segment.StartsWith(':'))
        {
            return segment;
        }

        var text = segment[1..];
        var constraintStart = text.IndexOf('<');
        if (constraintStart < 0 || !text.EndsWith('>'))
        {
            return $"{{{text}}}";
        }

        var name = text[..constraintStart];
        var constraint = text[(constraintStart + 1)..^1].ToLowerInvariant();
        var aspConstraint = constraint switch
        {
            "int" => "int",
            "uuid" => "guid",
            "alpha" => "regex(^[A-Za-z]+$)",
            "alnum" => "regex(^[A-Za-z0-9]+$)",
            "slug" => "regex(^[a-z0-9-]+$)",
            _ => string.Empty
        };

        if (string.IsNullOrWhiteSpace(aspConstraint))
        {
            return $"{{{name}}}";
        }

        return $"{{{name}:{aspConstraint}}}";
    }
}
