using Axom.Compiler.Diagnostics;
using System.Text.RegularExpressions;

namespace Axom.Compiler.Http.Routing;

public sealed class RouteDiscovery
{
    private static readonly HashSet<string> SupportedMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        "get", "post", "put", "patch", "delete", "head", "options"
    };

    private static readonly HashSet<string> KnownConstraints = new(StringComparer.OrdinalIgnoreCase)
    {
        "string", "int", "uuid", "slug", "alpha", "alnum"
    };

    public RouteDiscoveryResult Discover(string entryFilePath)
    {
        var routesRoot = FindRoutesRoot(entryFilePath);
        if (routesRoot is null)
        {
            return RouteDiscoveryResult.CreateSuccess(Array.Empty<RouteDefinition>());
        }

        var routeFiles = Directory
            .EnumerateFiles(routesRoot, "*.axom", SearchOption.AllDirectories)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToList();

        var routes = new List<RouteDefinition>(routeFiles.Count);
        var diagnostics = new List<Diagnostic>();
        foreach (var routeFile in routeFiles)
        {
            var parseResult = ParseRoute(routeFile, routesRoot);
            if (parseResult.Route is not null)
            {
                routes.Add(parseResult.Route);
            }

            diagnostics.AddRange(parseResult.Diagnostics);
        }

        diagnostics.AddRange(DetectConflicts(routes));

        if (diagnostics.Count > 0)
        {
            return RouteDiscoveryResult.CreateFailure(routes, diagnostics);
        }

        return RouteDiscoveryResult.CreateSuccess(routes);
    }

    private static string? FindRoutesRoot(string entryFilePath)
    {
        var absoluteEntryPath = Path.GetFullPath(entryFilePath);
        var startDirectory = Path.GetDirectoryName(absoluteEntryPath);
        if (string.IsNullOrWhiteSpace(startDirectory))
        {
            return null;
        }

        var current = new DirectoryInfo(startDirectory);
        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, "routes");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        return null;
    }

    private static ParseRouteResult ParseRoute(string routeFile, string routesRoot)
    {
        var relative = Path.GetRelativePath(routesRoot, routeFile);
        var withoutExtension = Path.ChangeExtension(relative, null) ?? string.Empty;
        var normalizedPath = withoutExtension.Replace('\\', '/');

        var pathParts = normalizedPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var fileName = pathParts.Length == 0 ? string.Empty : pathParts[^1];
        var method = "GET";

        var diagnostics = new List<Diagnostic>();

        var fileTokens = ParseFileTokens(routeFile, fileName, diagnostics, out var detectedMethod);
        if (!string.IsNullOrWhiteSpace(detectedMethod))
        {
            method = detectedMethod;
        }

        var segments = new List<RouteSegment>();

        for (var i = 0; i < pathParts.Length - 1; i++)
        {
            segments.Add(RouteSegment.Static(pathParts[i]));
        }

        foreach (var token in fileTokens)
        {
            if (string.Equals(token.Value, "index", StringComparison.OrdinalIgnoreCase) && token.IsDynamic == false)
            {
                continue;
            }

            segments.Add(token);
        }

        var template = segments.Count == 0
            ? "/"
            : "/" + string.Join('/', segments.Select(ToTemplateSegment));

        if (diagnostics.Count > 0)
        {
            return new ParseRouteResult(null, diagnostics);
        }

        return new ParseRouteResult(new RouteDefinition(method, template, routeFile, segments), diagnostics);
    }

    private static IReadOnlyList<RouteSegment> ParseFileTokens(
        string routeFile,
        string fileName,
        List<Diagnostic> diagnostics,
        out string method)
    {
        var tokensWithEmpty = fileName.Split('_', StringSplitOptions.None).ToList();
        method = "GET";

        if (tokensWithEmpty.Count > 0 && SupportedMethods.Contains(tokensWithEmpty[^1]))
        {
            method = tokensWithEmpty[^1].ToUpperInvariant();
            tokensWithEmpty.RemoveAt(tokensWithEmpty.Count - 1);
        }

        var tokens = new List<RouteSegment>();
        for (var i = 0; i < tokensWithEmpty.Count; i++)
        {
            var current = tokensWithEmpty[i];
            if (current.Length > 0)
            {
                tokens.Add(RouteSegment.Static(current));
                continue;
            }

            if (i + 1 >= tokensWithEmpty.Count)
            {
                diagnostics.Add(Diagnostic.Error(
                    routeFile,
                    1,
                    1,
                    "Invalid route filename: dynamic segment marker '__' must be followed by a parameter name."));
                continue;
            }

            var parameterName = tokensWithEmpty[i + 1];
            if (string.IsNullOrWhiteSpace(parameterName))
            {
                diagnostics.Add(Diagnostic.Error(
                    routeFile,
                    1,
                    1,
                    "Invalid route filename: dynamic parameter name cannot be empty."));
                continue;
            }

            if (!IsValidParameterName(parameterName))
            {
                diagnostics.Add(Diagnostic.Error(
                    routeFile,
                    1,
                    1,
                    $"Invalid route filename: dynamic parameter '{parameterName}' is not a valid identifier."));
                continue;
            }

            var constraint = "string";
            if (i + 2 < tokensWithEmpty.Count
                && KnownConstraints.Contains(tokensWithEmpty[i + 2]))
            {
                constraint = tokensWithEmpty[i + 2];
                i += 1;
            }

            tokens.Add(RouteSegment.Dynamic(parameterName, constraint));
            i += 1;
        }

        return tokens;
    }

    private static bool IsValidParameterName(string parameterName)
    {
        if (parameterName.Length == 0)
        {
            return false;
        }

        if (!(char.IsLetter(parameterName[0]) || parameterName[0] == '_'))
        {
            return false;
        }

        for (var i = 1; i < parameterName.Length; i++)
        {
            if (!(char.IsLetterOrDigit(parameterName[i]) || parameterName[i] == '_'))
            {
                return false;
            }
        }

        return true;
    }

    private static string ToTemplateSegment(RouteSegment segment)
    {
        if (!segment.IsDynamic)
        {
            return segment.Value;
        }

        if (string.Equals(segment.Constraint, "string", StringComparison.OrdinalIgnoreCase))
        {
            return $":{segment.Value}";
        }

        return $":{segment.Value}<{segment.Constraint}>";
    }

    private static List<Diagnostic> DetectConflicts(IReadOnlyList<RouteDefinition> routes)
    {
        var diagnostics = new List<Diagnostic>();

        for (var i = 0; i < routes.Count; i++)
        {
            for (var j = i + 1; j < routes.Count; j++)
            {
                var left = routes[i];
                var right = routes[j];

                if (!string.Equals(left.Method, right.Method, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (left.Segments.Count != right.Segments.Count)
                {
                    continue;
                }

                if (!TryGetOverlapReason(left.Segments, right.Segments, out var reason))
                {
                    continue;
                }

                diagnostics.Add(Diagnostic.Error(
                    left.FilePath,
                    1,
                    1,
                    $"Route conflict: {left.Method} {left.Template} ({left.FilePath}) overlaps {right.Method} {right.Template} ({right.FilePath}); reason: {reason}."));
            }
        }

        return diagnostics;
    }

    private static bool TryGetOverlapReason(
        IReadOnlyList<RouteSegment> left,
        IReadOnlyList<RouteSegment> right,
        out string reason)
    {
        reason = string.Empty;

        for (var i = 0; i < left.Count; i++)
        {
            if (!TryGetSegmentOverlapReason(left[i], right[i], out var segmentReason))
            {
                reason = string.Empty;
                return false;
            }

            if (i == left.Count - 1)
            {
                reason = segmentReason;
            }
        }

        reason = string.IsNullOrWhiteSpace(reason) ? "all segments overlap" : reason;
        return true;
    }

    private static bool TryGetSegmentOverlapReason(RouteSegment left, RouteSegment right, out string reason)
    {
        if (!left.IsDynamic && !right.IsDynamic)
        {
            var overlap = string.Equals(left.Value, right.Value, StringComparison.OrdinalIgnoreCase);
            reason = overlap
                ? $"static segment '{left.Value}' equals '{right.Value}'"
                : string.Empty;
            return overlap;
        }

        if (!left.IsDynamic && right.IsDynamic)
        {
            var overlap = IsValueAllowedByConstraint(left.Value, right.Constraint);
            reason = overlap
                ? $"static segment '{left.Value}' is accepted by dynamic ':{right.Value}<{right.Constraint}>'"
                : string.Empty;
            return overlap;
        }

        if (left.IsDynamic && !right.IsDynamic)
        {
            var overlap = IsValueAllowedByConstraint(right.Value, left.Constraint);
            reason = overlap
                ? $"static segment '{right.Value}' is accepted by dynamic ':{left.Value}<{left.Constraint}>'"
                : string.Empty;
            return overlap;
        }

        var constraintsOverlap = ConstraintsOverlap(left.Constraint, right.Constraint);
        reason = constraintsOverlap
            ? $"dynamic constraints overlap ('{left.Constraint}' with '{right.Constraint}')"
            : string.Empty;
        return constraintsOverlap;
    }

    private static bool ConstraintsOverlap(string left, string right)
    {
        if (string.Equals(left, right, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.Equals(left, "string", StringComparison.OrdinalIgnoreCase)
            || string.Equals(right, "string", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var sampleValues = new[]
        {
            "1",
            "abc",
            "abc-1",
            "550e8400-e29b-41d4-a716-446655440000"
        };

        return sampleValues.Any(value =>
            IsValueAllowedByConstraint(value, left)
            && IsValueAllowedByConstraint(value, right));
    }

    private static bool IsValueAllowedByConstraint(string value, string constraint)
    {
        var normalized = constraint.ToLowerInvariant();
        return normalized switch
        {
            "string" => !string.IsNullOrWhiteSpace(value),
            "int" => int.TryParse(value, out _),
            "uuid" => Guid.TryParse(value, out _),
            "slug" => Regex.IsMatch(value, "^[a-z0-9-]+$", RegexOptions.CultureInvariant),
            "alpha" => Regex.IsMatch(value, "^[A-Za-z]+$", RegexOptions.CultureInvariant),
            "alnum" => Regex.IsMatch(value, "^[A-Za-z0-9]+$", RegexOptions.CultureInvariant),
            _ => true
        };
    }

    private sealed record ParseRouteResult(RouteDefinition? Route, IReadOnlyList<Diagnostic> Diagnostics);
}
