namespace Axom.Runtime.Http;

public sealed record AxomHttpRequest(
    string Method,
    string Path,
    IReadOnlyDictionary<string, string> QueryParameters,
    IReadOnlyDictionary<string, string> RouteParameters,
    IReadOnlyDictionary<string, string> Headers,
    string? Body);
