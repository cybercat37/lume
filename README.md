# Lume

A modern, Gleam-inspired programming language with native .NET interoperability.

## Overview

Lume is a minimal, opinionated language for .NET focused on simplicity, explicit error handling, and structured concurrency, while remaining fully interoperable with existing C# code.

## Core Principles

- **One obvious way to do things** — reduce cognitive load by providing clear, unambiguous patterns
- **Explicit error handling** — errors are values, not exceptions
- **Structured concurrency by default** — safe, predictable concurrent code
- **Immutability by default** — prevent accidental mutations
- **Full .NET interoperability** — seamless integration with existing .NET ecosystems
- **Small, understandable language surface** — easy to learn and maintain

## Quick Start

### Hello World

```lume
print "Hello, Lume!"
```

Run it:

```bash
make run FILE=hello.lume
# or
dotnet run --project src/lume -- run hello.lume
```

### Example Program

```lume
let mut counter = 0
{
  let x = 10
  let y = 20
  counter = x + y
}
print counter
```

## Status

**In Development** — Steps 1-11 of the roadmap are complete. The compiler can parse, type-check, interpret, and generate C# code for basic programs. A full CLI with `check`, `build`, and `run` commands is available. Test infrastructure includes golden files and diagnostic snapshots, plus compilation caching.

### Currently Implemented ✅

- Lexer and Parser with error recovery
- Variables (`let`, `let mut`) and assignments
- Primitive types (`Int`, `Bool`, `String`)
- Arithmetic operators (`+`, `-`, `*`, `/`, `%`)
- Unary operators (`-`, `+`)
- String concatenation (`+`)
- Blocks and scoped variables
- String escape sequences (`\n`, `\t`, `\r`, `\"`, `\\`)
- Binding and scope resolution
- Type checking
- Interpreter runtime
- Code generation (emits C#)
- Builtin functions: `print`, `println`, `input`, `len`, `abs`, `min`, `max`
- CLI commands: `check`, `build`, `run` with options
- Test infrastructure: golden files and diagnostic snapshots
- Compilation cache (`--cache`) and large input guardrail

### Coming Soon 🔜

- Comparison operators (`==`, `!=`, `<`, `>`, `<=`, `>=`)
- Logical operators (`&&`, `||`, `!`)
- Pattern matching (`match`)
- Functions and lambdas
- Records and Sum types
- Generics
- `Result`/`Option` and error propagation (`?`)
- Collections (List, Map, Tuple)
- Iterator combinators
- Modules and imports
- Structured concurrency
- String interpolation
- Comments (`//`, `/* */`)

## Building and Running

### Prerequisites

- .NET 8 SDK
- Make (optional, for convenience targets)

### Build

```bash
make build
# or
dotnet build
```

### Run a Lume Program

```bash
make run FILE=path/to/file.lume
# or
dotnet run --project src/lume -- run path/to/file.lume
```

### Check a Lume Program

```bash
dotnet run --project src/lume -- check path/to/file.lume
```

Validates the source code without generating output files.

### Compile to C#

```bash
make compile FILE=path/to/file.lume
# or
dotnet run --project src/lume -- build path/to/file.lume
```

### CLI Options

- `--out <dir>` — Override output directory (default: `out`)
- `--quiet` — Suppress non-error output
- `--verbose` — Include extra context
- `--cache` — Enable compilation cache for repeated builds
- `--help` or `-h` — Show usage information
- `--version` — Show version

### Performance Notes

You can use `--cache` during `check`, `build`, or `run` to reuse parse/bind/emit work in the same process.

### Run Tests

```bash
make test
# or
dotnet test
```

### Install CLI (dotnet tool)

```bash
dotnet tool install -g Lume.Cli
# or (local tool manifest)
dotnet new tool-manifest
dotnet tool install --local Lume.Cli
```

### Shell Completions

- Bash: `source docs/completions/lume.bash`
- Zsh: `autoload -U compinit && compinit` then `fpath+=($PWD/docs/completions)`
- PowerShell: `. $PWD/docs/completions/lume.ps1`

### Golden/Snapshot Updates

Golden and snapshot files live under `tests/Lume.Tests/Golden` and `tests/Lume.Tests/Snapshots`.
To update them, run the relevant tests, then replace the `.golden.cs` or `.snapshot.txt`
files with the new expected output.

## Documentation

- **[Language Tutorial](docs/tutorial.md)** — Learn Lume with examples
- **[Language Specification](docs/spec.md)** — Complete language reference
- **[Roadmap](ROADMAP.md)** — Implementation progress and plans

## Roadmap

The implementation follows a 12-step roadmap:

1. ✅ **Pipeline Base** — Lexer, parser, AST, diagnostics, minimal emitter
2. ✅ **Core Syntax** — Variables, assignments, blocks, expressions
3. ✅ **Parser Robustness** — Error recovery and clear diagnostics
4. ✅ **Binding & Scope** — Symbol resolution and scope rules
5. ✅ **Type System** — Type checking and inference
6. ✅ **Interpreter Runtime** — Direct AST execution
7. ✅ **Code Generation v1** — C# code emission
8. ✅ **Standard Library** — Basic builtins (`print`, `println`, `input`, `len`, `abs`, `min`, `max`)
9. ✅ **CLI UX** — Commands `check`, `build`, `run` with options (`--out`, `--quiet`, `--verbose`, `--help`, `--version`)
10. ✅ **Test Hardening** — Golden files for codegen, snapshot tests for diagnostics
11. ✅ **Performance** — Incremental compilation, caching, guardrails
12. 🔜 **Tooling** — Packaging, distribution, CI

See [ROADMAP.md](ROADMAP.md) for detailed progress.

## Language Features

### No Traditional Control Flow

Lume intentionally omits `if`, `while`, `for`, and `loop`. Instead:

- **Pattern matching** with `match` for all branching
- **Tail recursion** for custom iteration
- **Iterator combinators** (`each`, `map`, `fold`, `filter`) for collections

### Explicit Error Handling

```lume
// Planned syntax
pub fn load(id: Int) -> Result<User, String> {
  let raw = db.get(id)?
  Ok(parse(raw)?)
}
```

Errors are values (`Result`/`Option`), not exceptions.

### Immutability by Default

```lume
let x = 10        // Immutable
let mut y = 20    // Mutable (scope-local only)
y = y + 1
```

## Contributing

See [`CONTRIBUTING.md`](CONTRIBUTING.md) for guidelines on how to contribute to Lume.

## License

Licensed under the Apache License 2.0. See [`LICENSE`](LICENSE) for details.
