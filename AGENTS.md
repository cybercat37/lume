# AGENTS.md

This repository is a .NET 8 solution for the Axom compiler/runtime/CLI.
Use this guide for consistent commands and code style when working here.
This file is the canonical AI workflow guidance for agentic coding tools.
## Repository layout
- `Axom.sln` is the solution root.
- `src/Axom.Compiler` holds compiler logic.
- `src/Axom.Runtime` holds runtime pieces.
- `src/axom` is the CLI application.
- `tests/Axom.Tests` contains xUnit tests.
## Build, run, test
Preferred entry points are the Makefile targets or `dotnet` commands.
### Build
- `make build` (wraps `dotnet build`)
- `dotnet build`
- `dotnet build Axom.sln`
### Run the CLI
- If the NuGet tool is installed:
  - `axom build path/to/file.axom`
  - `axom run path/to/file.axom`
  - `axom check path/to/file.axom`
- From source:
  - `dotnet run --project src/axom -- build path/to/file.axom`
  - `dotnet run --project src/axom -- run path/to/file.axom`
  - `dotnet run --project src/axom -- check path/to/file.axom`
  - `dotnet run --project src/axom -- check path/to/file.axom --cache`
- `make compile FILE=path/to/file.axom`
- `make run FILE=path/to/file.axom`
- `make demo-example` (runs `examples/demo-run.axom`)
### Test
- `make test` (wraps `dotnet test`)
- `make test-hardening` (golden + snapshot tests)
- `make test-pipeline` (runs `CompilerPipelineTests.Compile_print_string_generates_console_write`)
- `dotnet test`
- `dotnet test Axom.sln`
- `dotnet test tests/Axom.Tests/Axom.Tests.csproj`
### Run a single test
Use xUnit filters via `dotnet test --filter`.
- Filter by fully qualified name (best signal):
  - `dotnet test tests/Axom.Tests/Axom.Tests.csproj --filter "FullyQualifiedName~CompilerSmokeTests.Empty_source_produces_error"`
- Filter by class name:
  - `dotnet test tests/Axom.Tests/Axom.Tests.csproj --filter "FullyQualifiedName~CompilerSmokeTests"`
- Filter by method name:
  - `dotnet test tests/Axom.Tests/Axom.Tests.csproj --filter "Name~Empty_source_produces_error"`
### Fuzz
- `make fuzz` (short fuzz run)
- `dotnet run --project tests/Axom.Fuzz -- --iterations 1000 --max-length 128 --seed 123`
### Tooling/Release
- `make pack` (wraps `dotnet pack src/axom -c Release`)
- `make publish PACKAGE=path/to/*.nupkg` (requires `api.key`)
- `dotnet pack src/axom -c Release`
- `dotnet publish src/axom -c Release -o out/publish`
- `dotnet nuget push src/axom/bin/Release/*.nupkg -k <API_KEY> -s https://api.nuget.org/v3/index.json`
### CI
- GitHub Actions workflow: `.github/workflows/ci.yml` runs build, tests, hardening, and fuzz.
### Lint/format
No dedicated lint/format command is configured in this repo.
If you add one, document it here and in the Makefile.
## Versioning
- Use SemVer with prerelease tags (e.g., `0.4.0-alpha.2`, `0.3.0-rc1`).
- **Source of truth** for the tool package version is `src/axom/axom.csproj` (`<Version>`).
- Git tags must follow `v<Version>` and should match the package version when cutting a release.
- Record the current values here to avoid ambiguity:
  - CLI package version: `0.4.0-alpha.7` (`src/axom/axom.csproj`)
  - Latest tag: `v0.4.0-alpha.7`
## Code style guidelines
Follow the existing conventions visible in the C# files.
### Language specification
- Language spec lives at `docs/spec.md`.
- Statement terminators are optional; newlines end statements by default.
- Semicolons are allowed as explicit separators (useful inside blocks).
### C# language and project settings
- Target framework: .NET 8 (`net8.0`).
- Nullable reference types are enabled (`<Nullable>enable</Nullable>`).
- Implicit usings are enabled (`<ImplicitUsings>enable</ImplicitUsings>`).
### Files, namespaces, and layout
- Use file-scoped namespaces: `namespace Axom.Compiler;`.
- Keep one public type per file when practical.
- Match file names to type names (e.g., `CompilerDriver` in `CompilerDriver.cs`).
- Use Unix line endings and 4-space indentation.
- Use Allman style braces (opening brace on a new line).
### Using directives and imports
- Place `using` directives at the top of the file.
- Prefer minimal explicit imports since implicit usings are on.
- Follow the existing pattern: project imports first, then framework imports.
### Naming conventions
- Types and namespaces: `PascalCase`.
- Methods and properties: `PascalCase`.
- Local variables and parameters: `camelCase`.
- Test method names: descriptive snake_case is currently used (keep consistent).
- Boolean names should read as predicates when possible (`isReady`, `hasErrors`).
### Types and nullability
- Respect nullable annotations; avoid suppressions unless justified.
- Prefer `var` when the RHS makes the type obvious.
- Use explicit types when clarity matters (public APIs, ambiguous expressions).
### Formatting and expressions
- Use `var` with `new` when the type is explicit on the right.
- Keep simple `if` blocks with braces (no single-line brace-less blocks).
- Expression-bodied members are ok for short, clear expressions.
- Raw string literals (`"""`) are used for generated code blocks.
### Error handling and diagnostics
- Compiler errors should be surfaced via `Diagnostic` objects and results,
  not thrown exceptions during normal compilation flow.
- CLI errors should write to `Console.Error` and exit with non-zero codes.
- Use exceptions for unexpected, non-recoverable runtime failures.
- Keep error messages concise and user-facing.
### Collections and LINQ
- Prefer simple loops when they improve readability.
- Use LINQ when it clarifies intent and avoids extra state.
### Tests
- Tests use xUnit (`[Fact]`).
- Keep tests small and focused on a single behavior.
- Prefer Arrange/Act/Assert separation with blank lines.
- Use `Assert.*` rather than custom assertion helpers unless repeated.
- Test files are prefixed by step/sub-step (e.g., `02_10_PrecedenceTests.cs`) to map coverage to the roadmap.
### Logging and output
- CLI output: user-facing messages to `Console.WriteLine`.
- Errors and diagnostics: `Console.Error.WriteLine`.
- Avoid noisy logging in libraries; bubble results to the CLI.
### File system usage
- Use `Path.Combine` for building paths.
- Ensure directories exist before writing output (`Directory.CreateDirectory`).
- Clean up temporary resources when possible; keep debugging toggles obvious.
### Public API design
- Keep public APIs minimal and focused.
- Prefer immutable public types when possible (get-only properties).
- Use factory methods for controlled construction (see `Diagnostic.Error`).
## Agent notes
- No Cursor rules found in `.cursor/rules/` and no `.cursorrules` file.
- No `.github/copilot-instructions.md` file present.
- Do not commit generated build artifacts (`bin/`, `obj/`, `out/`).
- Keep changes scoped; avoid unrelated refactors unless required by the task.
- Keep `axom init` scaffold output up to date as language/runtime features evolve
  (generated files, README, Docker assets, and route examples should reflect current behavior).
## Compiler modularity (guidance)
- Keep compiler phases explicit and isolated: Parse → Bind → Lower → Interpret/Emit.
- Introduce a dedicated lowering pass (`Lowerer`) that transforms `BoundProgram`
  into a lowered form consumed by the interpreter and emitter.
- AST stays syntax-only; bound nodes stay semantic; lowered nodes represent
  executable forms. Avoid cross-layer dependencies.
- Preserve public entry points used by tests (e.g., `SyntaxTree.Parse`,
  `Interpreter.Run`) to keep tests unchanged while internal structure evolves.
## Roadmap references
- The consolidated roadmap lives in `roadmap.md`.
## If you add new tooling
When adding tooling (formatters, analyzers, CI tasks), update this file
with the exact commands and any per-project variants.
