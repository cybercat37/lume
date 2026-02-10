using Axom.Compiler.Diagnostics;
using Axom.Compiler.Parsing;
using Axom.Compiler.Syntax;
using Axom.Compiler.Text;
using System.Text;

namespace Axom.Compiler.Modules;

public sealed class ModuleResolver
{
    public ModuleResolutionResult Resolve(string entryPath, string entrySource)
    {
        var diagnostics = new List<Diagnostic>();
        var modules = new Dictionary<string, ModuleInfo>(StringComparer.Ordinal);
        var ordered = new List<ModuleInfo>();
        var stack = new List<string>();

        var fullEntryPath = Path.GetFullPath(entryPath);
        VisitModule(fullEntryPath, entrySource, diagnostics, modules, ordered, stack);

        ValidateFromImports(diagnostics, modules);
        if (diagnostics.Count > 0)
        {
            var entryTree = modules.TryGetValue(fullEntryPath, out var info)
                ? info.SyntaxTree
                : SyntaxTree.Parse(new SourceText(entrySource, entryPath));
            return ModuleResolutionResult.CreateFailure(diagnostics, entryTree);
        }

        var combinedSource = BuildCombinedSource(ordered, fullEntryPath);
        return ModuleResolutionResult.CreateSuccess(combinedSource);
    }

    private static void VisitModule(
        string modulePath,
        string? sourceOverride,
        List<Diagnostic> diagnostics,
        Dictionary<string, ModuleInfo> modules,
        List<ModuleInfo> ordered,
        List<string> stack)
    {
        if (modules.TryGetValue(modulePath, out var existing))
        {
            if (existing.State == ModuleVisitState.Visiting)
            {
                var cycleStart = stack.IndexOf(modulePath);
                var cycle = cycleStart >= 0
                    ? stack.Skip(cycleStart).Append(modulePath).Select(Path.GetFileNameWithoutExtension)
                    : new[] { Path.GetFileNameWithoutExtension(modulePath) };
                diagnostics.Add(Diagnostic.Error(modulePath, 1, 1, $"Import cycle detected: {string.Join(" -> ", cycle)}"));
            }

            return;
        }

        string source;
        if (sourceOverride is not null)
        {
            source = sourceOverride;
        }
        else
        {
            if (!File.Exists(modulePath))
            {
                diagnostics.Add(Diagnostic.Error(modulePath, 1, 1, $"Module file not found: {modulePath}"));
                return;
            }

            source = File.ReadAllText(modulePath);
        }

        var sourceText = new SourceText(source, modulePath);
        var syntaxTree = SyntaxTree.Parse(sourceText);
        if (syntaxTree.Diagnostics.Count > 0)
        {
            diagnostics.AddRange(syntaxTree.Diagnostics);
        }

        var module = new ModuleInfo(modulePath, source, syntaxTree)
        {
            State = ModuleVisitState.Visiting
        };
        modules[modulePath] = module;
        stack.Add(modulePath);

        foreach (var statement in syntaxTree.Root.Statements)
        {
            if (statement is ImportStatementSyntax importStatement)
            {
                var moduleName = string.Join('.', importStatement.ModuleParts.Select(part => part.Text));
                var dependencyPath = VisitDependency(modulePath, moduleName, diagnostics, modules, ordered, stack);
                module.Imports.Add(new ModuleImport(moduleName, dependencyPath, isFromImport: false, Array.Empty<ImportSpecifierSyntax>()));
            }
            else if (statement is FromImportStatementSyntax fromImportStatement)
            {
                var moduleName = string.Join('.', fromImportStatement.ModuleParts.Select(part => part.Text));
                var dependencyPath = VisitDependency(modulePath, moduleName, diagnostics, modules, ordered, stack);
                module.Imports.Add(new ModuleImport(moduleName, dependencyPath, isFromImport: true, fromImportStatement.Specifiers));
            }
        }

        stack.RemoveAt(stack.Count - 1);
        module.State = ModuleVisitState.Visited;
        ordered.Add(module);
    }

    private static string? VisitDependency(
        string fromModulePath,
        string moduleName,
        List<Diagnostic> diagnostics,
        Dictionary<string, ModuleInfo> modules,
        List<ModuleInfo> ordered,
        List<string> stack)
    {
        var dependencyPath = ResolveModulePath(fromModulePath, moduleName);
        if (dependencyPath is null)
        {
            diagnostics.Add(Diagnostic.Error(fromModulePath, 1, 1, $"Module not found: {moduleName}"));
            return null;
        }

        VisitModule(dependencyPath, sourceOverride: null, diagnostics, modules, ordered, stack);
        return dependencyPath;
    }

    private static string? ResolveModulePath(string fromModulePath, string moduleName)
    {
        var parts = moduleName.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return null;
        }

        var relativePath = Path.Combine(parts) + ".axom";
        var currentDirectory = Path.GetDirectoryName(fromModulePath);
        while (!string.IsNullOrEmpty(currentDirectory))
        {
            var candidate = Path.Combine(currentDirectory, relativePath);
            if (File.Exists(candidate))
            {
                return Path.GetFullPath(candidate);
            }

            currentDirectory = Path.GetDirectoryName(currentDirectory);
        }

        return null;
    }

    private static void ValidateFromImports(List<Diagnostic> diagnostics, Dictionary<string, ModuleInfo> modules)
    {
        foreach (var module in modules.Values)
        {
            foreach (var import in module.Imports.Where(item => item.IsFromImport))
            {
                var dependencyPath = ResolveModulePath(module.Path, import.ModuleName);
                if (dependencyPath is null || !modules.TryGetValue(dependencyPath, out var dependency))
                {
                    continue;
                }

                var exports = CollectExports(dependency.SyntaxTree.Root.Statements);
                foreach (var specifier in import.Specifiers)
                {
                    if (specifier.IsWildcard)
                    {
                        diagnostics.Add(Diagnostic.Error(module.Path, 1, 1, "Wildcard imports are not supported."));
                        continue;
                    }

                    var importedName = specifier.NameToken.Text;
                    if (!exports.Contains(importedName))
                    {
                        diagnostics.Add(Diagnostic.Error(module.Path, 1, 1, $"Module '{import.ModuleName}' does not export '{importedName}'."));
                    }
                }
            }
        }
    }

    private static HashSet<string> CollectExports(IReadOnlyList<StatementSyntax> statements)
    {
        var exports = new HashSet<string>(StringComparer.Ordinal);
        foreach (var statement in statements)
        {
            if (statement is not PubStatementSyntax pub)
            {
                continue;
            }

            var name = GetDeclarationName(pub.Declaration);
            if (!string.IsNullOrEmpty(name))
            {
                exports.Add(name);
            }
        }

        return exports;
    }

    private static string? GetDeclarationName(StatementSyntax declaration)
    {
        return declaration switch
        {
            FunctionDeclarationSyntax function => function.IdentifierToken.Text,
            RecordTypeDeclarationSyntax record => record.IdentifierToken.Text,
            SumTypeDeclarationSyntax sum => sum.IdentifierToken.Text,
            VariableDeclarationSyntax variable when variable.Pattern is IdentifierPatternSyntax identifierPattern => identifierPattern.IdentifierToken.Text,
            _ => null
        };
    }

    private static string BuildCombinedSource(IReadOnlyList<ModuleInfo> orderedModules, string entryPath)
    {
        var importDemand = BuildImportDemand(orderedModules);
        var builder = new StringBuilder();
        foreach (var module in orderedModules)
        {
            var isEntry = string.Equals(module.Path, entryPath, StringComparison.Ordinal);
            foreach (var statement in module.SyntaxTree.Root.Statements)
            {
                if (statement is ImportStatementSyntax or FromImportStatementSyntax)
                {
                    continue;
                }

                if (statement is PubStatementSyntax pub)
                {
                    if (!isEntry && !ShouldIncludeExport(module.Path, pub.Declaration, importDemand))
                    {
                        continue;
                    }

                    AppendStatement(builder, module.Source, pub.Declaration.Span);
                    continue;
                }

                if (isEntry)
                {
                    AppendStatement(builder, module.Source, statement.Span);
                }
            }
        }

        return builder.ToString();
    }

    private static Dictionary<string, ImportDemand> BuildImportDemand(IReadOnlyList<ModuleInfo> orderedModules)
    {
        var demand = new Dictionary<string, ImportDemand>(StringComparer.Ordinal);
        foreach (var module in orderedModules)
        {
            foreach (var import in module.Imports)
            {
                if (import.ResolvedPath is null)
                {
                    continue;
                }

                if (!demand.TryGetValue(import.ResolvedPath, out var moduleDemand))
                {
                    moduleDemand = new ImportDemand();
                    demand[import.ResolvedPath] = moduleDemand;
                }

                if (!import.IsFromImport)
                {
                    moduleDemand.IncludeAll = true;
                    continue;
                }

                foreach (var specifier in import.Specifiers)
                {
                    if (!specifier.IsWildcard)
                    {
                        moduleDemand.Names.Add(specifier.NameToken.Text);
                    }
                }
            }
        }

        return demand;
    }

    private static bool ShouldIncludeExport(string modulePath, StatementSyntax declaration, Dictionary<string, ImportDemand> importDemand)
    {
        if (!importDemand.TryGetValue(modulePath, out var demand))
        {
            return false;
        }

        if (demand.IncludeAll)
        {
            return true;
        }

        var name = GetDeclarationName(declaration);
        return !string.IsNullOrEmpty(name) && demand.Names.Contains(name);
    }

    private static void AppendStatement(StringBuilder builder, string source, TextSpan span)
    {
        if (span.Start < 0 || span.End > source.Length || span.Length <= 0)
        {
            return;
        }

        builder.Append(source.AsSpan(span.Start, span.Length));
        builder.AppendLine();
        builder.AppendLine();
    }

    private sealed class ModuleInfo
    {
        public string Path { get; }
        public string Source { get; }
        public SyntaxTree SyntaxTree { get; }
        public List<ModuleImport> Imports { get; } = new();
        public ModuleVisitState State { get; set; }

        public ModuleInfo(string path, string source, SyntaxTree syntaxTree)
        {
            Path = path;
            Source = source;
            SyntaxTree = syntaxTree;
            State = ModuleVisitState.Unvisited;
        }
    }

    private sealed class ModuleImport
    {
        public string ModuleName { get; }
        public string? ResolvedPath { get; }
        public bool IsFromImport { get; }
        public IReadOnlyList<ImportSpecifierSyntax> Specifiers { get; }

        public ModuleImport(string moduleName, string? resolvedPath, bool isFromImport, IReadOnlyList<ImportSpecifierSyntax> specifiers)
        {
            ModuleName = moduleName;
            ResolvedPath = resolvedPath;
            IsFromImport = isFromImport;
            Specifiers = specifiers;
        }
    }

    private sealed class ImportDemand
    {
        public bool IncludeAll { get; set; }
        public HashSet<string> Names { get; } = new(StringComparer.Ordinal);
    }

    private enum ModuleVisitState
    {
        Unvisited,
        Visiting,
        Visited
    }
}

public sealed class ModuleResolutionResult
{
    public bool Success { get; }
    public string CombinedSource { get; }
    public IReadOnlyList<Diagnostic> Diagnostics { get; }
    public SyntaxTree? EntrySyntaxTree { get; }

    private ModuleResolutionResult(bool success, string combinedSource, IReadOnlyList<Diagnostic> diagnostics, SyntaxTree? entrySyntaxTree)
    {
        Success = success;
        CombinedSource = combinedSource;
        Diagnostics = diagnostics;
        EntrySyntaxTree = entrySyntaxTree;
    }

    public static ModuleResolutionResult CreateSuccess(string combinedSource) =>
        new(true, combinedSource, Array.Empty<Diagnostic>(), null);

    public static ModuleResolutionResult CreateFailure(IReadOnlyList<Diagnostic> diagnostics, SyntaxTree entrySyntaxTree) =>
        new(false, string.Empty, diagnostics, entrySyntaxTree);
}
