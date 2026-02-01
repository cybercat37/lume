# AI Instructions

This file provides guidance for AI assistants working on this repository.

## Repo summary
- Lume is a .NET 8 solution for a compiler/runtime/CLI.
- Solution root: `Lume.sln`.
- Compiler: `src/Lume.Compiler`.
- Runtime: `src/Lume.Runtime`.
- CLI app: `src/lume`.
- Tests: `tests/Lume.Tests` (xUnit).

## Build, run, test
- Build: `make build` or `dotnet build` or `dotnet build Lume.sln`.
- Run CLI: `dotnet run --project src/lume -- run path/to/file.lume`.
- Check CLI: `dotnet run --project src/lume -- check path/to/file.lume`.
- Compile to C#: `dotnet run --project src/lume -- build path/to/file.lume`.
- Tests: `make test` or `dotnet test` or `dotnet test Lume.sln`.
- Single test (xUnit filter):
  - `dotnet test tests/Lume.Tests/Lume.Tests.csproj --filter "FullyQualifiedName~ClassName.MethodName"`.

## Code style
- Use file-scoped namespaces.
- One public type per file when practical.
- Unix line endings, 4-space indentation, Allman braces.
- Minimal explicit usings (implicit usings are enabled).
- Keep statements braced (no single-line blocks).

## Language guidance (Lume)
- Statement terminators are optional; newlines end statements by default.
- Semicolons allowed as explicit separators.
- Errors should surface via diagnostics (not exceptions).

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
- See `ROADMAP.md` and step files (`STEP1_PIPELINE.md` ... `STEP13_FUNCTIONS.md`).
