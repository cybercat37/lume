# AGENTS.md

This repository is a .NET 8 solution for the Lume compiler/runtime/CLI.
Use this guide for consistent commands and code style when working here.

## Repository layout
- `Lume.sln` is the solution root.
- `src/Lume.Compiler` holds compiler logic.
- `src/Lume.Runtime` holds runtime pieces.
- `src/lume` is the CLI application.
- `tests/Lume.Tests` contains xUnit tests.

## Build, run, test
Preferred entry points are the Makefile targets or `dotnet` commands.

### Build
- `make build` (wraps `dotnet build`)
- `dotnet build`
- `dotnet build Lume.sln`

### Run the CLI
- `dotnet run --project src/lume -- build path/to/file.lume`
- `dotnet run --project src/lume -- run path/to/file.lume`
- `make compile FILE=path/to/file.lume`
- `make run FILE=path/to/file.lume`

### Test
- `make test` (wraps `dotnet test`)
- `make test-pipeline` (runs `CompilerPipelineTests.Compile_print_string_generates_console_write`)
- `dotnet test`
- `dotnet test Lume.sln`
- `dotnet test tests/Lume.Tests/Lume.Tests.csproj`

### Run a single test
Use xUnit filters via `dotnet test --filter`.

- Filter by fully qualified name (best signal):
  - `dotnet test tests/Lume.Tests/Lume.Tests.csproj --filter "FullyQualifiedName~CompilerSmokeTests.Empty_source_produces_error"`
- Filter by class name:
  - `dotnet test tests/Lume.Tests/Lume.Tests.csproj --filter "FullyQualifiedName~CompilerSmokeTests"`
- Filter by method name:
  - `dotnet test tests/Lume.Tests/Lume.Tests.csproj --filter "Name~Empty_source_produces_error"`

### Lint/format
No dedicated lint/format command is configured in this repo.
If you add one, document it here and in the Makefile.

## Code style guidelines
Follow the existing conventions visible in the C# files.

### Language specification
- Language spec lives at `docs/spec.md`.

### Language spec alignment
- Statement terminators are optional; newlines end statements by default.
- Semicolons are allowed as explicit separators (useful inside blocks).

### C# language and project settings
- Target framework: .NET 8 (`net8.0`).
- Nullable reference types are enabled (`<Nullable>enable</Nullable>`).
- Implicit usings are enabled (`<ImplicitUsings>enable</ImplicitUsings>`).

### Files, namespaces, and layout
- Use file-scoped namespaces: `namespace Lume.Compiler;`.
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

## Special notes for agents
- There are no Cursor or Copilot instruction files in this repo.
- Do not commit generated build artifacts (`bin/`, `obj/`, `out/`).
- Keep changes scoped; avoid unrelated refactors unless required by the task.

## Roadmap (12 main steps)
See `ROADMAP.md` for the full 12-step program, `STEP1_PIPELINE.md` for Step 1, `STEP2_CORE_SYNTAX.md` for Step 2, `STEP3_PARSER_ROBUSTNESS.md` for Step 3, `STEP4_BINDING_SCOPE.md` for Step 4, `STEP5_TYPE_SYSTEM.md` for Step 5, `STEP6_INTERPRETER.md` for Step 6, and `STEP7_CODEGEN.md` for Step 7.

### Current progress
- Step 1 (Pipeline base): complete
- Step 2 (Core syntax): complete
- Step 3 (Parser robustness): complete
- Step 4 (Binding and scope): complete
- Step 5 (Type system): complete
- Step 6 (Interpreter runtime): complete
- Step 7 (Code generation v1): complete

## If you add new tooling
When adding tooling (formatters, analyzers, CI tasks), update this file
with the exact commands and any per-project variants.
