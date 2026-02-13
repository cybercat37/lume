using System.Linq;

namespace Axom.Compiler.Interop;

public static class DotNetInteropWhitelist
{
    private static readonly Dictionary<string, Type> AllowedTypes = new(StringComparer.Ordinal)
    {
        ["System.Math"] = typeof(Math),
        ["System.String"] = typeof(string),
        ["System.Convert"] = typeof(Convert)
    };

    private static readonly Dictionary<string, IReadOnlyList<string>> AllowedMethods = new(StringComparer.Ordinal)
    {
        ["System.Math"] = new[] { "Abs", "Max", "Min", "Sqrt", "Pow", "Floor", "Ceiling" },
        ["System.String"] = new[]
        {
            "Contains",
            "StartsWith",
            "EndsWith",
            "ToUpper",
            "ToLower",
            "Trim",
            "Substring",
            "Concat",
            "IsNullOrEmpty",
            "IsNullOrWhiteSpace"
        },
        ["System.Convert"] = new[] { "ToInt32", "ToDouble", "ToString", "ToBoolean" }
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

    public static IReadOnlyList<string> GetAllowedTypes()
    {
        return AllowedTypes.Keys.OrderBy(name => name, StringComparer.Ordinal).ToList();
    }
}
