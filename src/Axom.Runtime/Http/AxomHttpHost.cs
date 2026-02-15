using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Globalization;

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

        var hasCustomHealthRoute = routes.Any(route =>
            string.Equals(route.Method, "GET", StringComparison.OrdinalIgnoreCase)
            && string.Equals(route.Template, "/health", StringComparison.Ordinal));

        if (!hasCustomHealthRoute)
        {
            app.MapGet("/health", () =>
            {
                return ToAspNetResult(AxomHttpResponse.Text(StatusCodes.Status200OK, "ok"));
            });
        }

        foreach (var route in routes)
        {
            var aspPattern = ToAspNetRoutePattern(route.Template);
            app.MapMethods(aspPattern, new[] { route.Method }, async (HttpContext context) =>
            {
                if (route.Handler is not null)
                {
                    var request = await BuildRequestAsync(context, context.RequestAborted);
                    var response = await route.Handler(request, context.RequestAborted);
                    return ToAspNetResult(response);
                }

                var routeValues = context.Request.RouteValues
                    .Where(entry => entry.Value is not null)
                    .Select(entry => $"{entry.Key}={entry.Value}")
                    .ToArray();

                var suffix = routeValues.Length == 0
                    ? string.Empty
                    : $" params{{{string.Join(',', routeValues)}}}";

                var payload = $"stub route matched: {route.Method} {route.Template}{suffix}";
                return ToAspNetResult(AxomHttpResponse.Text(StatusCodes.Status200OK, payload));
            });
        }

        app.MapFallback(() => Results.NotFound());

        return app;
    }

    private static async Task<AxomHttpRequest> BuildRequestAsync(HttpContext context, CancellationToken cancellationToken)
    {
        var queryParameters = context.Request.Query
            .ToDictionary(pair => pair.Key, pair => pair.Value.ToString(), StringComparer.Ordinal);

        var routeParameters = context.Request.RouteValues
            .Where(pair => pair.Value is not null)
            .ToDictionary(
                pair => pair.Key,
                pair => Convert.ToString(pair.Value, CultureInfo.InvariantCulture) ?? string.Empty,
                StringComparer.Ordinal);

        var headers = context.Request.Headers
            .ToDictionary(pair => pair.Key, pair => pair.Value.ToString(), StringComparer.OrdinalIgnoreCase);

        string? body = null;
        if (context.Request.ContentLength is > 0)
        {
            using var reader = new StreamReader(
                context.Request.Body,
                System.Text.Encoding.UTF8,
                detectEncodingFromByteOrderMarks: true,
                bufferSize: 1024,
                leaveOpen: true);
            body = await reader.ReadToEndAsync(cancellationToken);
            if (context.Request.Body.CanSeek)
            {
                context.Request.Body.Position = 0;
            }
        }

        return new AxomHttpRequest(
            context.Request.Method,
            context.Request.Path.ToString(),
            queryParameters,
            routeParameters,
            headers,
            body);
    }

    private static IResult ToAspNetResult(AxomHttpResponse response)
    {
        return Results.Content(response.Body, response.ContentType, statusCode: response.StatusCode);
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
