namespace Axom.Compiler.Http.Routing;

public sealed record RouteSegment(
    bool IsDynamic,
    string Value,
    string Constraint)
{
    public static RouteSegment Static(string value) => new(false, value, string.Empty);

    public static RouteSegment Dynamic(string name, string constraint) =>
        new(true, name, string.IsNullOrWhiteSpace(constraint) ? "string" : constraint);
}
