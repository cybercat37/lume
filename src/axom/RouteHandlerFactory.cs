using Axom.Compiler.Http.Routing;
using Axom.Compiler.Interpreting;
using Axom.Compiler.Parsing;
using Axom.Compiler.Text;
using Axom.Runtime.Http;
using Microsoft.AspNetCore.Http;

namespace Axom.Cli;

public static class RouteHandlerFactory
{
    public static RouteEndpoint CreateEndpoint(RouteDefinition route)
    {
        var sourceText = File.Exists(route.FilePath)
            ? File.ReadAllText(route.FilePath)
            : string.Empty;
        var syntaxTree = SyntaxTree.Parse(new SourceText(sourceText, route.FilePath));
        var dynamicSegmentNames = route.Segments
            .Where(segment => segment.IsDynamic)
            .Select(segment => segment.Value)
            .ToArray();

        return new RouteEndpoint(route.Method, route.Template, route.FilePath, (context, _) =>
        {
            if (syntaxTree.Diagnostics.Count > 0)
            {
                var parseMessage = string.Join(
                    Environment.NewLine,
                    syntaxTree.Diagnostics.Select(diagnostic => diagnostic.ToString()));
                return Task.FromResult<IResult>(
                    Results.Text(parseMessage, "text/plain; charset=utf-8", statusCode: StatusCodes.Status500InternalServerError));
            }

            var interpreter = new Interpreter();
            interpreter.SetRequestContext(context.Request.Method, context.Request.Path.ToString());
            var queryValues = context.Request.Query
                .ToDictionary(pair => pair.Key, pair => pair.Value.ToString(), StringComparer.Ordinal);
            interpreter.SetQueryParameters(queryValues);
            if (dynamicSegmentNames.Length > 0)
            {
                var routeValues = dynamicSegmentNames
                    .Select(name => context.Request.RouteValues.TryGetValue(name, out var value)
                        ? (name, value: Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty)
                        : (name, value: string.Empty))
                    .ToDictionary(entry => entry.name, entry => entry.value, StringComparer.Ordinal);
                interpreter.SetRouteParameters(routeValues);
            }

            var result = interpreter.Run(syntaxTree);
            var errorDiagnostics = result.Diagnostics
                .Where(diagnostic => diagnostic.Severity == Axom.Compiler.Diagnostics.DiagnosticSeverity.Error)
                .ToList();
            if (errorDiagnostics.Count > 0)
            {
                var message = string.Join(Environment.NewLine, errorDiagnostics.Select(diagnostic => diagnostic.ToString()));
                return Task.FromResult<IResult>(
                    Results.Text(message, "text/plain; charset=utf-8", statusCode: StatusCodes.Status500InternalServerError));
            }

            if (result.Response is not null)
            {
                return Task.FromResult<IResult>(
                    Results.Text(result.Response.Body, "text/plain; charset=utf-8", statusCode: result.Response.StatusCode));
            }

            return Task.FromResult<IResult>(
                Results.Text(result.Output, "text/plain; charset=utf-8", statusCode: StatusCodes.Status200OK));
        });
    }
}
