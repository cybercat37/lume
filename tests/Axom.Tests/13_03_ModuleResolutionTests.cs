using Axom.Compiler;

public class ModuleResolutionTests
{
    [Fact]
    public void From_import_resolves_pub_function()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var mathDir = Path.Combine(tempDir, "math");
            var appDir = Path.Combine(tempDir, "app");
            Directory.CreateDirectory(mathDir);
            Directory.CreateDirectory(appDir);

            File.WriteAllText(Path.Combine(mathDir, "utils.axom"), "pub fn add(a: Int, b: Int) -> Int => a + b\n");
            var mainPath = Path.Combine(appDir, "main.axom");
            File.WriteAllText(mainPath, "from math.utils import add\nprint add(1, 2)\n");

            var compiler = new CompilerDriver();
            var result = compiler.Compile(File.ReadAllText(mainPath), mainPath);

            Assert.True(result.Success);
        }
        finally
        {
            DeleteTempDirectory(tempDir);
        }
    }

    [Fact]
    public void From_import_limits_symbols_to_selected_exports()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var mathDir = Path.Combine(tempDir, "math");
            var appDir = Path.Combine(tempDir, "app");
            Directory.CreateDirectory(mathDir);
            Directory.CreateDirectory(appDir);

            File.WriteAllText(Path.Combine(mathDir, "utils.axom"),
                "pub fn add(a: Int, b: Int) -> Int => a + b\n" +
                "pub fn sub(a: Int, b: Int) -> Int => a - b\n");

            var mainPath = Path.Combine(appDir, "main.axom");
            File.WriteAllText(mainPath, "from math.utils import add\nprint sub(3, 1)\n");

            var compiler = new CompilerDriver();
            var result = compiler.Compile(File.ReadAllText(mainPath), mainPath);

            Assert.False(result.Success);
            Assert.Contains(result.Diagnostics, diagnostic =>
                diagnostic.Message.Contains("Undefined variable", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            DeleteTempDirectory(tempDir);
        }
    }

    [Fact]
    public void Import_loads_module_exports_into_compilation()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var libDir = Path.Combine(tempDir, "lib");
            var appDir = Path.Combine(tempDir, "app");
            Directory.CreateDirectory(libDir);
            Directory.CreateDirectory(appDir);

            File.WriteAllText(Path.Combine(libDir, "tools.axom"), "pub fn forty_two() -> Int => 42\n");
            var mainPath = Path.Combine(appDir, "main.axom");
            File.WriteAllText(mainPath, "import lib.tools\nprint forty_two()\n");

            var compiler = new CompilerDriver();
            var result = compiler.Compile(File.ReadAllText(mainPath), mainPath);

            Assert.True(result.Success);
        }
        finally
        {
            DeleteTempDirectory(tempDir);
        }
    }

    [Fact]
    public void From_import_non_pub_symbol_produces_diagnostic()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var mathDir = Path.Combine(tempDir, "math");
            var appDir = Path.Combine(tempDir, "app");
            Directory.CreateDirectory(mathDir);
            Directory.CreateDirectory(appDir);

            File.WriteAllText(Path.Combine(mathDir, "utils.axom"), "fn hidden() -> Int => 1\n");
            var mainPath = Path.Combine(appDir, "main.axom");
            File.WriteAllText(mainPath, "from math.utils import hidden\nprint hidden()\n");

            var compiler = new CompilerDriver();
            var result = compiler.Compile(File.ReadAllText(mainPath), mainPath);

            Assert.False(result.Success);
            Assert.Contains(result.Diagnostics, diagnostic =>
                diagnostic.Message.Contains("does not export", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            DeleteTempDirectory(tempDir);
        }
    }

    [Fact]
    public void Missing_module_produces_diagnostic()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var mainPath = Path.Combine(tempDir, "main.axom");
            File.WriteAllText(mainPath, "\nimport does.not.exist\nprint 1\n");

            var compiler = new CompilerDriver();
            var result = compiler.Compile(File.ReadAllText(mainPath), mainPath);

            Assert.False(result.Success);
            var diagnostic = Assert.Single(result.Diagnostics.Where(d =>
                d.Message.Contains("Module not found", StringComparison.OrdinalIgnoreCase)));
            Assert.Equal(2, diagnostic.Line);
            Assert.True(diagnostic.Column > 1);
        }
        finally
        {
            DeleteTempDirectory(tempDir);
        }
    }

    [Fact]
    public void Import_cycle_produces_diagnostic()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            File.WriteAllText(Path.Combine(tempDir, "a.axom"), "import b\npub fn a() -> Int => 1\n");
            var bPath = Path.Combine(tempDir, "b.axom");
            File.WriteAllText(bPath, "import a\npub fn b() -> Int => 2\n");

            var compiler = new CompilerDriver();
            var result = compiler.Compile(File.ReadAllText(Path.Combine(tempDir, "a.axom")), Path.Combine(tempDir, "a.axom"));

            Assert.False(result.Success);
            Assert.Contains(result.Diagnostics, diagnostic =>
                diagnostic.Message.Contains("Import cycle", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            DeleteTempDirectory(tempDir);
        }
    }

    [Fact]
    public void Wildcard_import_produces_diagnostic()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var mathDir = Path.Combine(tempDir, "math");
            Directory.CreateDirectory(mathDir);
            File.WriteAllText(Path.Combine(mathDir, "utils.axom"), "pub fn add(a: Int, b: Int) -> Int => a + b\n");

            var mainPath = Path.Combine(tempDir, "main.axom");
            File.WriteAllText(mainPath, "from math.utils import *\nprint 1\n");

            var compiler = new CompilerDriver();
            var result = compiler.Compile(File.ReadAllText(mainPath), mainPath);

            Assert.False(result.Success);
            Assert.Contains(result.Diagnostics, diagnostic =>
                diagnostic.Message.Contains("Wildcard imports", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            DeleteTempDirectory(tempDir);
        }
    }

    [Fact]
    public void Duplicate_from_import_name_produces_conflict_diagnostic()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var aDir = Path.Combine(tempDir, "a");
            var bDir = Path.Combine(tempDir, "b");
            Directory.CreateDirectory(aDir);
            Directory.CreateDirectory(bDir);
            File.WriteAllText(Path.Combine(aDir, "mod.axom"), "pub fn foo() -> Int => 1\n");
            File.WriteAllText(Path.Combine(bDir, "mod.axom"), "pub fn foo() -> Int => 2\n");

            var mainPath = Path.Combine(tempDir, "main.axom");
            File.WriteAllText(mainPath, "from a.mod import foo\nfrom b.mod import foo\nprint foo()\n");

            var compiler = new CompilerDriver();
            var result = compiler.Compile(File.ReadAllText(mainPath), mainPath);

            Assert.False(result.Success);
            Assert.Contains(result.Diagnostics, diagnostic =>
                diagnostic.Message.Contains("conflicts with an existing name", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            DeleteTempDirectory(tempDir);
        }
    }

    [Fact]
    public void Import_alias_conflict_with_local_name_produces_diagnostic()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var libDir = Path.Combine(tempDir, "lib");
            Directory.CreateDirectory(libDir);
            File.WriteAllText(Path.Combine(libDir, "tools.axom"), "pub fn forty_two() -> Int => 42\n");

            var mainPath = Path.Combine(tempDir, "main.axom");
            File.WriteAllText(mainPath, "fn tools() -> Int => 0\nimport lib.tools as tools\nprint 1\n");

            var compiler = new CompilerDriver();
            var result = compiler.Compile(File.ReadAllText(mainPath), mainPath);

            Assert.False(result.Success);
            var diagnostic = Assert.Single(result.Diagnostics.Where(d =>
                d.Message.Contains("Imported alias 'tools' conflicts", StringComparison.OrdinalIgnoreCase)));
            Assert.Equal(2, diagnostic.Line);
            Assert.True(diagnostic.Column > 1);
        }
        finally
        {
            DeleteTempDirectory(tempDir);
        }
    }

    [Fact]
    public void Import_alias_is_reported_as_not_supported()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var libDir = Path.Combine(tempDir, "lib");
            Directory.CreateDirectory(libDir);
            File.WriteAllText(Path.Combine(libDir, "tools.axom"), "pub fn forty_two() -> Int => 42\n");

            var mainPath = Path.Combine(tempDir, "main.axom");
            File.WriteAllText(mainPath, "import lib.tools as t\nprint forty_two()\n");

            var compiler = new CompilerDriver();
            var result = compiler.Compile(File.ReadAllText(mainPath), mainPath);

            Assert.False(result.Success);
            Assert.Contains(result.Diagnostics, diagnostic =>
                diagnostic.Message.Contains("import alias is not supported", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            DeleteTempDirectory(tempDir);
        }
    }

    [Fact]
    public void From_import_alias_is_reported_as_not_supported()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var libDir = Path.Combine(tempDir, "lib");
            Directory.CreateDirectory(libDir);
            File.WriteAllText(Path.Combine(libDir, "tools.axom"), "pub fn forty_two() -> Int => 42\n");

            var mainPath = Path.Combine(tempDir, "main.axom");
            File.WriteAllText(mainPath, "from lib.tools import forty_two as ft\nprint 1\n");

            var compiler = new CompilerDriver();
            var result = compiler.Compile(File.ReadAllText(mainPath), mainPath);

            Assert.False(result.Success);
            Assert.Contains(result.Diagnostics, diagnostic =>
                diagnostic.Message.Contains("from-import alias is not supported", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            DeleteTempDirectory(tempDir);
        }
    }

    private static string CreateTempDirectory()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"axom_modules_{Guid.NewGuid():N}");
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
