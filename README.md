# Axom

A modern, Gleam-inspired programming language with native .NET interoperability.

## Overview

Axom is a minimal, opinionated language for .NET focused on simplicity, explicit error handling, and structured concurrency, while remaining fully interoperable with existing C# code.

## Core Principles

- **One obvious way to do things** — reduce cognitive load by providing clear, unambiguous patterns
- **Explicit error handling** — errors are values, not exceptions
- **Structured concurrency by default** — safe, predictable concurrent code
- **Immutability by default** — prevent accidental mutations
- **Full .NET interoperability** — seamless integration with existing .NET ecosystems
- **Small, understandable language surface** — easy to learn and maintain

## Quick Start

### Hello World

```axom
print "Hello, Axom!"
```

Run it:

```bash
make run FILE=hello.axom
# or
dotnet run --project src/axom -- run hello.axom
```

### Example Programs

**Functions and lambdas**

```axom
fn add(x: Int, y: Int) => x + y

let inc = fn(x: Int) => x + 1
print add(1, 2)
print inc(2)
```

**Input and string helpers**

```axom
let name = input()
print len(name)
```

**Arithmetic utilities**

```axom
print abs(-10)
print min(3, 7)
print max(3, 7)
```

### Editor Support (VS Code)

There is a minimal local VS Code extension for Axom syntax highlighting:

1. Open the Command Palette.
2. Run `Developer: Install Extension from Location...`.
3. Select `tools/vscode-axom`.

## Status

**In Development** — Steps 1-12 of the roadmap are complete. The compiler can parse, type-check, interpret, and generate C# code for basic programs. A full CLI with `check`, `build`, and `run` commands is available. Test infrastructure includes golden files and diagnostic snapshots, plus compilation caching and tooling support.

### Currently Implemented ✅

- Lexer and Parser with error recovery
- Variables (`let`, `let mut`) and assignments
- Primitive types (`Int`, `Bool`, `String`)
- Arithmetic operators (`+`, `-`, `*`, `/`, `%`)
- Unary operators (`-`, `+`)
- String concatenation (`+`)
- Blocks and scoped variables
- Functions and lambdas (`fn`, `return`)
- Pattern matching v1 (`match` with literals, `_`, identifiers, tuples)
- String escape sequences (`\n`, `\t`, `\r`, `\"`, `\\`)
- Binding and scope resolution
- Type checking
- Interpreter runtime
- Code generation (emits C#)
- Builtin functions: `print`, `println`, `input`, `len`, `abs`, `min`, `max`
- CLI commands: `check`, `build`, `run` with options
- Test infrastructure: golden files and diagnostic snapshots
- Compilation cache (`--cache`) and large input guardrail
- Tooling: dotnet tool packaging, CI workflow, shell completions

### Coming Soon 🔜

- Comparison operators (`==`, `!=`, `<`, `>`, `<=`, `>=`)
- Logical operators (`&&`, `||`, `!`)
- Pattern matching (guards, lists, sum types)
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

### Run a Axom Program

```bash
make run FILE=path/to/file.axom
# or
dotnet run --project src/axom -- run path/to/file.axom
```

### Check a Axom Program

```bash
dotnet run --project src/axom -- check path/to/file.axom
```

Validates the source code without generating output files.

### Compile to C#

```bash
make compile FILE=path/to/file.axom
# or
dotnet run --project src/axom -- build path/to/file.axom
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
dotnet tool install -g Axom.CLi
# or (local tool manifest)
dotnet new tool-manifest
dotnet tool install --local Axom.CLi
```

### Shell Completions

- Bash: `source docs/completions/axom.bash`
- Zsh: `autoload -U compinit && compinit` then `fpath+=($PWD/docs/completions)`
- PowerShell: `. $PWD/docs/completions/axom.ps1`

### Golden/Snapshot Updates

Golden and snapshot files live under `tests/Axom.Tests/Golden` and `tests/Axom.Tests/Snapshots`.
To update them, run the relevant tests, then replace the `.golden.cs` or `.snapshot.txt`
files with the new expected output.

## Documentation

- **[Language Tutorial](docs/tutorial.md)** — Learn Axom with examples
- **[Language Specification](docs/spec.md)** — Complete language reference
- **[Roadmap](ROADMAP.md)** — Implementation progress and plans

## Roadmap

The current plan is the v0.5 roadmap (next minor release).

- ✅ Steps 1–13 complete (pipeline through functions/lambdas)
- ⏭ Next step: **Pattern match v1** (see `STEP15_PATTERN_MATCH.md`)
- 📍 Full plan: [ROADMAP.md](ROADMAP.md)

## Language Features

### No Traditional Control Flow

Axom intentionally omits `if`, `while`, `for`, and `loop`. Instead:

- **Pattern matching** with `match` for all branching
- **Tail recursion** for custom iteration
- **Iterator combinators** (`each`, `map`, `fold`, `filter`) for collections

### Explicit Error Handling

```axom
// Planned syntax
pub fn load(id: Int) -> Result<User, String> {
  let raw = db.get(id)?
  Ok(parse(raw)?)
}
```

Errors are values (`Result`/`Option`), not exceptions.

### Immutability by Default

```axom
let x = 10        // Immutable
let mut y = 20    // Mutable (scope-local only)
y = y + 1
```

## Contributing

See [`CONTRIBUTING.md`](CONTRIBUTING.md) for guidelines on how to contribute to Axom.

## License

Licensed under the Apache License 2.0. See [`LICENSE`](LICENSE) for details.
