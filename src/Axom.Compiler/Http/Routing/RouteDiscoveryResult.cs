using Axom.Compiler.Diagnostics;

namespace Axom.Compiler.Http.Routing;

public sealed record RouteDiscoveryResult(
    bool Success,
    IReadOnlyList<RouteDefinition> Routes,
    IReadOnlyList<Diagnostic> Diagnostics)
{
    public static RouteDiscoveryResult CreateSuccess(IReadOnlyList<RouteDefinition> routes) =>
        new(true, routes, Array.Empty<Diagnostic>());

    public static RouteDiscoveryResult CreateFailure(IReadOnlyList<RouteDefinition> routes, IReadOnlyList<Diagnostic> diagnostics) =>
        new(false, routes, diagnostics);
}
