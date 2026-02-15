namespace Axom.Runtime.Http;

public delegate Task<AxomHttpResponse> RouteEndpointHandler(AxomHttpRequest request, CancellationToken cancellationToken);

public sealed record RouteEndpoint(
    string Method,
    string Template,
    string SourceFile,
    RouteEndpointHandler? Handler = null);
