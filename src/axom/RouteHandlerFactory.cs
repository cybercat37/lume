using System.Globalization;
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
            if (dynamicSegmentNames.Length > 0)
            {
                var routeInputs = dynamicSegmentNames
                    .Select(name => context.Request.RouteValues.TryGetValue(name, out var value)
                        ? Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty
                        : string.Empty)
                    .ToArray();
                interpreter.SetInput(routeInputs);
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

            return Task.FromResult<IResult>(
                Results.Text(result.Output, "text/plain; charset=utf-8", statusCode: StatusCodes.Status200OK));
        });
    }
}
