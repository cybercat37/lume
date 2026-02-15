using System.Text.Json;

namespace Axom.Runtime.Http;

public sealed record AxomHttpResponse(int StatusCode, string Body, string ContentType)
{
    public static AxomHttpResponse Text(int statusCode, string body)
    {
        return new AxomHttpResponse(statusCode, body, "text/plain; charset=utf-8");
    }

    public static AxomHttpResponse Json(int statusCode, object value)
    {
        var payload = JsonSerializer.Serialize(value);
        return new AxomHttpResponse(statusCode, payload, "application/json; charset=utf-8");
    }
}
