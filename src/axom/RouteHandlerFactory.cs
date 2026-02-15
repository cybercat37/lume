using Axom.Compiler.Http.Routing;
using Axom.Compiler.Interpreting;
using Axom.Compiler.Parsing;
using Axom.Compiler.Text;
using Axom.Runtime.Http;

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
                return Task.FromResult(AxomHttpResponse.Text(500, parseMessage));
            }

            var interpreter = new Interpreter();
            interpreter.SetRequestContext(context.Method, context.Path);
            interpreter.SetQueryParameters(context.QueryParameters);
            if (dynamicSegmentNames.Length > 0)
            {
                var routeValues = dynamicSegmentNames
                    .Select(name => context.RouteParameters.TryGetValue(name, out var value)
                        ? (name, value)
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
                return Task.FromResult(AxomHttpResponse.Text(500, message));
            }

            if (result.Response is not null)
            {
                return Task.FromResult(AxomHttpResponse.Text(result.Response.StatusCode, result.Response.Body));
            }

            return Task.FromResult(AxomHttpResponse.Text(200, result.Output));
        });
    }
}
