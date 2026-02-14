using Axom.Compiler.Http.Routing;

namespace Axom.Tests;

public class RouteDiscoveryTests
{
    [Fact]
    public void Discover_maps_index_and_method_suffixes()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var appDir = Path.Combine(tempDir, "app");
            var routesDir = Path.Combine(appDir, "routes");
            Directory.CreateDirectory(routesDir);
            Directory.CreateDirectory(Path.Combine(routesDir, "users"));

            var entry = Path.Combine(appDir, "main.axom");
            File.WriteAllText(entry, "print 1");
            File.WriteAllText(Path.Combine(routesDir, "users", "index_get.axom"), "print 1");
            File.WriteAllText(Path.Combine(routesDir, "users", "posts_post.axom"), "print 1");

            var discovery = new RouteDiscovery();
            var result = discovery.Discover(entry);

            Assert.True(result.Success);
            Assert.Contains(result.Routes, route => route.Method == "GET" && route.Template == "/users");
            Assert.Contains(result.Routes, route => route.Method == "POST" && route.Template == "/users/posts");
        }
        finally
        {
            DeleteTempDirectory(tempDir);
        }
    }

    [Fact]
    public void Discover_maps_dynamic_param_with_constraint()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var appDir = Path.Combine(tempDir, "app");
            var routesDir = Path.Combine(appDir, "routes");
            Directory.CreateDirectory(routesDir);

            var entry = Path.Combine(appDir, "main.axom");
            File.WriteAllText(entry, "print 1");
            File.WriteAllText(Path.Combine(routesDir, "users__id_int_get.axom"), "print 1");

            var discovery = new RouteDiscovery();
            var result = discovery.Discover(entry);

            Assert.True(result.Success);
            Assert.Contains(result.Routes, route => route.Method == "GET" && route.Template == "/users/:id<int>");
        }
        finally
        {
            DeleteTempDirectory(tempDir);
        }
    }

    [Fact]
    public void Discover_reports_conflict_for_overlapping_templates()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var appDir = Path.Combine(tempDir, "app");
            var routesDir = Path.Combine(appDir, "routes");
            Directory.CreateDirectory(routesDir);

            var entry = Path.Combine(appDir, "main.axom");
            File.WriteAllText(entry, "print 1");
            File.WriteAllText(Path.Combine(routesDir, "users_me_get.axom"), "print 1");
            File.WriteAllText(Path.Combine(routesDir, "users__id_get.axom"), "print 1");

            var discovery = new RouteDiscovery();
            var result = discovery.Discover(entry);

            Assert.False(result.Success);
            Assert.Contains(result.Diagnostics, diagnostic =>
                diagnostic.Message.Contains("Route conflict", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(result.Diagnostics, diagnostic =>
                diagnostic.Message.Contains("reason:", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            DeleteTempDirectory(tempDir);
        }
    }

    [Fact]
    public void Discover_conflict_diagnostic_includes_overlap_reason_detail()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var appDir = Path.Combine(tempDir, "app");
            var routesDir = Path.Combine(appDir, "routes");
            Directory.CreateDirectory(routesDir);

            var entry = Path.Combine(appDir, "main.axom");
            File.WriteAllText(entry, "print 1");
            File.WriteAllText(Path.Combine(routesDir, "users_me_get.axom"), "print 1");
            File.WriteAllText(Path.Combine(routesDir, "users__id_get.axom"), "print 1");

            var discovery = new RouteDiscovery();
            var result = discovery.Discover(entry);

            Assert.False(result.Success);
            Assert.Contains(result.Diagnostics, diagnostic =>
                diagnostic.Message.Contains("accepted by dynamic", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            DeleteTempDirectory(tempDir);
        }
    }

    [Fact]
    public void Discover_allows_static_vs_int_when_no_overlap()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var appDir = Path.Combine(tempDir, "app");
            var routesDir = Path.Combine(appDir, "routes");
            Directory.CreateDirectory(routesDir);

            var entry = Path.Combine(appDir, "main.axom");
            File.WriteAllText(entry, "print 1");
            File.WriteAllText(Path.Combine(routesDir, "users_me_get.axom"), "print 1");
            File.WriteAllText(Path.Combine(routesDir, "users__id_int_get.axom"), "print 1");

            var discovery = new RouteDiscovery();
            var result = discovery.Discover(entry);

            Assert.True(result.Success);
            Assert.Empty(result.Diagnostics);
        }
        finally
        {
            DeleteTempDirectory(tempDir);
        }
    }

    [Fact]
    public void Discover_supports_flat_underscore_routes()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var appDir = Path.Combine(tempDir, "app");
            var routesDir = Path.Combine(appDir, "routes");
            Directory.CreateDirectory(routesDir);

            var entry = Path.Combine(appDir, "main.axom");
            File.WriteAllText(entry, "print 1");
            File.WriteAllText(Path.Combine(routesDir, "admin_users_list_get.axom"), "print 1");

            var discovery = new RouteDiscovery();
            var result = discovery.Discover(entry);

            Assert.True(result.Success);
            Assert.Contains(result.Routes, route => route.Method == "GET" && route.Template == "/admin/users/list");
        }
        finally
        {
            DeleteTempDirectory(tempDir);
        }
    }

    private static string CreateTempDirectory()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"axom_routes_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        return tempDir;
    }

    private static void DeleteTempDirectory(string tempDir)
    {
        if (Directory.Exists(tempDir))
        {
            Directory.Delete(tempDir, true);
        }
    }
}
