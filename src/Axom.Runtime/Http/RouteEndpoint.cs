namespace Axom.Runtime.Http;

public sealed record RouteEndpoint(
    string Method,
    string Template,
    string SourceFile);
