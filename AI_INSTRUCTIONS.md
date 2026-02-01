# AI Instructions

This file provides guidance for AI assistants working on this repository.

## Repo summary
- Axom is a .NET 8 solution for a compiler/runtime/CLI.
- Solution root: `Axom.sln`.
- Compiler: `src/Axom.Compiler`.
- Runtime: `src/Axom.Runtime`.
- CLI app: `src/axom`.
- Tests: `tests/Axom.Tests` (xUnit).

## Build, run, test
- Build: `make build` or `dotnet build` or `dotnet build Axom.sln`.
- Run CLI: `dotnet run --project src/axom -- run path/to/file.axom`.
- Check CLI: `dotnet run --project src/axom -- check path/to/file.axom`.
- Compile to C#: `dotnet run --project src/axom -- build path/to/file.axom`.
- Tests: `make test` or `dotnet test` or `dotnet test Axom.sln`.
- Single test (xUnit filter):
  - `dotnet test tests/Axom.Tests/Axom.Tests.csproj --filter "FullyQualifiedName~ClassName.MethodName"`.

## Code style
- Use file-scoped namespaces.
- One public type per file when practical.
- Unix line endings, 4-space indentation, Allman braces.
- Minimal explicit usings (implicit usings are enabled).
- Keep statements braced (no single-line blocks).

## Language guidance (Axom)
- Statement terminators are optional; newlines end statements by default.
- Semicolons allowed as explicit separators.
- Errors should surface via diagnostics (not exceptions).
- `match` v1 supports literals, `_`, identifiers, and tuples.

## Tests and docs
- Keep tests small and focused on a single behavior.
- Use Arrange/Act/Assert with blank lines.
- Test files use numbered prefixes to match roadmap steps.
- Docs: update `README.md`, `docs/spec.md`, `docs/tutorial.md` when language features change.

## Git and workspace hygiene
- Do not commit generated artifacts (`bin/`, `obj/`, `out/`).
- Keep changes scoped to the task.
- Do not rewrite history without explicit user request.

## Roadmap
- See `docs/roadmap/ROADMAP.md` and step files in `docs/roadmap/`.
