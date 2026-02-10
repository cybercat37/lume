namespace Axom.Compiler.Interop;

public static class DotNetInteropWhitelist
{
    private static readonly Dictionary<string, Type> AllowedTypes = new(StringComparer.Ordinal)
    {
        ["System.Math"] = typeof(Math)
    };

    private static readonly Dictionary<string, IReadOnlyList<string>> AllowedMethods = new(StringComparer.Ordinal)
    {
        ["System.Math"] = new[] { "Abs", "Max", "Min", "Sqrt", "Pow", "Floor", "Ceiling" }
    };

    public static bool IsTypeAllowed(string typeName)
    {
        return AllowedTypes.ContainsKey(typeName);
    }

    public static bool IsMethodAllowed(string typeName, string methodName)
    {
        return AllowedMethods.TryGetValue(typeName, out var methods)
            && methods.Contains(methodName, StringComparer.Ordinal);
    }

    public static bool TryResolveType(string typeName, out Type type)
    {
        return AllowedTypes.TryGetValue(typeName, out type!);
    }

    public static IReadOnlyDictionary<string, IReadOnlyList<string>> GetAllowedMethods()
    {
        return AllowedMethods;
    }
}
