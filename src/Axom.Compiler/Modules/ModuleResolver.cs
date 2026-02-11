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
        ValidateImportConflicts(diagnostics, modules);
        if (diagnostics.Count > 0)
        {
            var entryTree = modules.TryGetValue(fullEntryPath, out var info)
                ? info.SyntaxTree
                : SyntaxTree.Parse(new SourceText(entrySource, entryPath));
            return ModuleResolutionResult.CreateFailure(diagnostics, entryTree);
        }

        var combinedSource = BuildCombinedSource(ordered, fullEntryPath, modules);
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
                var dependencyPath = VisitDependency(module, moduleName, importStatement.ModuleParts.Last().Span, diagnostics, modules, ordered, stack);
                module.Imports.Add(new ModuleImport(
                    moduleName,
                    dependencyPath,
                    isFromImport: false,
                    Array.Empty<ImportSpecifierSyntax>(),
                    importStatement.Alias?.Text,
                    importStatement.Span,
                    importStatement.Alias?.Span,
                    importStatement.ModuleParts.Last().Span,
                    null,
                    new Dictionary<string, TextSpan>(StringComparer.Ordinal)));
            }
            else if (statement is FromImportStatementSyntax fromImportStatement)
            {
                var moduleName = string.Join('.', fromImportStatement.ModuleParts.Select(part => part.Text));
                var dependencyPath = VisitDependency(module, moduleName, fromImportStatement.ModuleParts.Last().Span, diagnostics, modules, ordered, stack);
                var introducedNameSpans = new Dictionary<string, TextSpan>(StringComparer.Ordinal);
                var requestedNameSpans = new Dictionary<string, TextSpan>(StringComparer.Ordinal);
                TextSpan? wildcardSpan = null;
                foreach (var specifier in fromImportStatement.Specifiers)
                {
                    if (specifier.IsWildcard)
                    {
                        wildcardSpan = specifier.Span;
                        continue;
                    }

                    requestedNameSpans[specifier.NameToken.Text] = specifier.NameToken.Span;
                    var introducedName = specifier.AliasToken?.Text ?? specifier.NameToken.Text;
                    introducedNameSpans[introducedName] = (specifier.AliasToken ?? specifier.NameToken).Span;
                }

                module.Imports.Add(new ModuleImport(
                    moduleName,
                    dependencyPath,
                    isFromImport: true,
                    fromImportStatement.Specifiers,
                    aliasName: null,
                    fromImportStatement.Span,
                    aliasSpan: null,
                    moduleNameSpan: fromImportStatement.ModuleParts.Last().Span,
                    wildcardSpan,
                    introducedNameSpans,
                    requestedNameSpans));
            }
        }

        stack.RemoveAt(stack.Count - 1);
        module.State = ModuleVisitState.Visited;
        ordered.Add(module);
    }

    private static string? VisitDependency(
        ModuleInfo fromModule,
        string moduleName,
        TextSpan moduleNameSpan,
        List<Diagnostic> diagnostics,
        Dictionary<string, ModuleInfo> modules,
        List<ModuleInfo> ordered,
        List<string> stack)
    {
        var dependencyPath = ResolveModulePath(fromModule.Path, moduleName);
        if (dependencyPath is null)
        {
            diagnostics.Add(CreateModuleDiagnostic(fromModule, moduleNameSpan, $"Module not found: {moduleName}"));
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
                if (import.ResolvedPath is null || !modules.TryGetValue(import.ResolvedPath, out var dependency))
                {
                    continue;
                }

                var exports = CollectExportDeclarations(dependency.SyntaxTree.Root.Statements);
                foreach (var specifier in import.Specifiers)
                {
                    if (specifier.IsWildcard)
                    {
                        diagnostics.Add(CreateModuleDiagnostic(module, import.WildcardSpan ?? specifier.Span, "Wildcard imports are not supported."));
                        continue;
                    }

                    var importedName = specifier.NameToken.Text;
                    if (!exports.TryGetValue(importedName, out var exportedDeclaration))
                    {
                        var span = import.GetRequestedNameSpan(importedName) ?? specifier.NameToken.Span;
                        diagnostics.Add(CreateModuleDiagnostic(module, span, $"Module '{import.ModuleName}' does not export '{importedName}'."));
                        continue;
                    }

                    if (specifier.AliasToken is not null && !CanAliasAsValue(exportedDeclaration))
                    {
                        diagnostics.Add(CreateModuleDiagnostic(module, specifier.AliasToken.Span, $"Cannot alias type export '{importedName}' in from-import."));
                    }
                }
            }
        }
    }

    private static void ValidateImportConflicts(List<Diagnostic> diagnostics, Dictionary<string, ModuleInfo> modules)
    {
        foreach (var module in modules.Values)
        {
            var namesInScope = CollectLocalDeclarationNames(module.SyntaxTree.Root.Statements);
            foreach (var import in module.Imports)
            {
                if (import.ResolvedPath is null || !modules.TryGetValue(import.ResolvedPath, out var dependency))
                {
                    continue;
                }

                if (!import.IsFromImport)
                {
                    if (!string.IsNullOrEmpty(import.AliasName))
                    {
                        if (!namesInScope.Add(import.AliasName))
                        {
                            diagnostics.Add(CreateModuleDiagnostic(module, import.AliasSpan ?? import.StatementSpan, $"Imported alias '{import.AliasName}' conflicts with an existing name."));
                        }
                    }
                    else
                    {
                        var exports = CollectExportDeclarations(dependency.SyntaxTree.Root.Statements);
                        foreach (var export in exports.Keys)
                        {
                            if (!namesInScope.Add(export))
                            {
                                var span = import.GetIntroducedNameSpan(export) ?? import.StatementSpan;
                                diagnostics.Add(CreateModuleDiagnostic(module, span, $"Imported name '{export}' conflicts with an existing name."));
                            }
                        }
                    }

                    continue;
                }

                foreach (var specifier in import.Specifiers)
                {
                    if (specifier.IsWildcard)
                    {
                        continue;
                    }

                    var introducedName = specifier.AliasToken?.Text ?? specifier.NameToken.Text;
                    if (!namesInScope.Add(introducedName))
                    {
                        var span = import.GetIntroducedNameSpan(introducedName) ?? specifier.Span;
                        diagnostics.Add(CreateModuleDiagnostic(module, span, $"Imported name '{introducedName}' conflicts with an existing name."));
                    }
                }
            }
        }
    }

    private static Diagnostic CreateModuleDiagnostic(ModuleInfo module, TextSpan span, string message)
    {
        return Diagnostic.Error(module.SyntaxTree.SourceText, span, message);
    }

    private static Dictionary<string, StatementSyntax> CollectExportDeclarations(IReadOnlyList<StatementSyntax> statements)
    {
        var exports = new Dictionary<string, StatementSyntax>(StringComparer.Ordinal);
        foreach (var statement in statements)
        {
            if (statement is not PubStatementSyntax pub)
            {
                continue;
            }

            var name = GetDeclarationName(pub.Declaration);
            if (!string.IsNullOrEmpty(name))
            {
                exports[name] = pub.Declaration;
            }
        }

        return exports;
    }

    private static HashSet<string> CollectLocalDeclarationNames(IReadOnlyList<StatementSyntax> statements)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var statement in statements)
        {
            if (statement is ImportStatementSyntax or FromImportStatementSyntax)
            {
                continue;
            }

            var declaration = statement is PubStatementSyntax pub ? pub.Declaration : statement;
            var name = GetDeclarationName(declaration);
            if (!string.IsNullOrEmpty(name))
            {
                names.Add(name);
            }
        }

        return names;
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

    private static string BuildCombinedSource(
        IReadOnlyList<ModuleInfo> orderedModules,
        string entryPath,
        IReadOnlyDictionary<string, ModuleInfo> modules)
    {
        var importDemand = BuildImportDemand(orderedModules);
        var builder = new StringBuilder();
        foreach (var module in orderedModules)
        {
            var isEntry = string.Equals(module.Path, entryPath, StringComparison.Ordinal);
            AppendFromImportAliasBridges(builder, module, modules);

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

    private static void AppendFromImportAliasBridges(
        StringBuilder builder,
        ModuleInfo module,
        IReadOnlyDictionary<string, ModuleInfo> modules)
    {
        foreach (var import in module.Imports)
        {
            if (!import.IsFromImport || import.ResolvedPath is null || !modules.TryGetValue(import.ResolvedPath, out var dependency))
            {
                continue;
            }

            var exports = CollectExportDeclarations(dependency.SyntaxTree.Root.Statements);
            foreach (var specifier in import.Specifiers)
            {
                if (specifier.IsWildcard || specifier.AliasToken is null)
                {
                    continue;
                }

                var requestedName = specifier.NameToken.Text;
                if (!exports.TryGetValue(requestedName, out var declaration) || !CanAliasAsValue(declaration))
                {
                    continue;
                }

                builder.AppendLine($"let {specifier.AliasToken.Text} = {requestedName}");
                builder.AppendLine();
            }
        }
    }

    private static bool CanAliasAsValue(StatementSyntax declaration)
    {
        return declaration is FunctionDeclarationSyntax or VariableDeclarationSyntax;
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
        public string? AliasName { get; }
        public TextSpan StatementSpan { get; }
        public TextSpan? AliasSpan { get; }
        public TextSpan ModuleNameSpan { get; }
        public TextSpan? WildcardSpan { get; }
        private readonly IReadOnlyDictionary<string, TextSpan> introducedNameSpans;
        private readonly IReadOnlyDictionary<string, TextSpan> requestedNameSpans;

        public ModuleImport(
            string moduleName,
            string? resolvedPath,
            bool isFromImport,
            IReadOnlyList<ImportSpecifierSyntax> specifiers,
            string? aliasName,
            TextSpan statementSpan,
            TextSpan? aliasSpan,
            TextSpan moduleNameSpan,
            TextSpan? wildcardSpan,
            IReadOnlyDictionary<string, TextSpan> introducedNameSpans,
            IReadOnlyDictionary<string, TextSpan>? requestedNameSpans = null)
        {
            ModuleName = moduleName;
            ResolvedPath = resolvedPath;
            IsFromImport = isFromImport;
            Specifiers = specifiers;
            AliasName = aliasName;
            StatementSpan = statementSpan;
            AliasSpan = aliasSpan;
            ModuleNameSpan = moduleNameSpan;
            WildcardSpan = wildcardSpan;
            this.introducedNameSpans = introducedNameSpans;
            this.requestedNameSpans = requestedNameSpans ?? introducedNameSpans;
        }

        public TextSpan? GetIntroducedNameSpan(string name)
        {
            return introducedNameSpans.TryGetValue(name, out var span) ? span : null;
        }

        public TextSpan? GetRequestedNameSpan(string name)
        {
            return requestedNameSpans.TryGetValue(name, out var span) ? span : null;
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
