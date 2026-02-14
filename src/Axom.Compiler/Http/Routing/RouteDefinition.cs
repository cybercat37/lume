namespace Axom.Compiler.Http.Routing;

public sealed record RouteDefinition(
    string Method,
    string Template,
    string FilePath,
    IReadOnlyList<RouteSegment> Segments);
