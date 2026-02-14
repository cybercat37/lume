using Microsoft.AspNetCore.Http;

namespace Axom.Runtime.Http;

public delegate Task<IResult> RouteEndpointHandler(HttpContext context, CancellationToken cancellationToken);

public sealed record RouteEndpoint(
    string Method,
    string Template,
    string SourceFile,
    RouteEndpointHandler? Handler = null);
