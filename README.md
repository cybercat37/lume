# Axom

Axom — deliberate code, explicit flow.

A modern programming language with native .NET interoperability.

## Overview

Axom is a minimal, opinionated language for .NET focused on clarity over cleverness. It favors a small, consistent surface area, explicit error handling, and control flow that reads like a set of decisions instead of a chain of incidental mechanics. The goal is to make programs predictable to read, easy to reason about, and still fully interoperable with existing C# code.

## Core Principles

- **One obvious way to do things** — reduce cognitive load by providing clear, unambiguous patterns
- **Explicit error handling** — errors are values, not exceptions
- **Structured concurrency by default** — safe, predictable concurrent code
- **Immutability by default** — prevent accidental mutations
- **Full .NET interoperability** — seamless integration with existing .NET ecosystems
- **Small, understandable language surface** — easy to learn and maintain

## Why Axom over C#

If you like the .NET ecosystem but want a smaller, more deliberate language, Axom offers a few concrete advantages:

- **Smaller surface area** — fewer constructs and one obvious way to do things means less incidental complexity than C#.
- **Explicit errors** — errors are values (`Result`/`Option` planned), making failure paths visible instead of hidden in exceptions.
- **Immutable by default** — safer state handling with mutation opt-in (`let mut`).
- **Match-first control flow** — `match` is the primary branching tool, keeping logic explicit and centralized.
- **.NET interop without losing Axom’s shape** — you can call into existing C# libraries while writing Axom code.
- **Predictable output** — the compiler emits C#, so integration and debugging can stay familiar when needed.

## Quick Start

### Hello World

```axom
print "Hello, Axom!"
```

Run it:

```bash
axom run hello.axom
# or (from source)
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

**Match as control flow**

```axom
let message = match count {
  0 -> "none"
  _ -> "some"
}
print message
```

**Tuple patterns**

```axom
print match (1, 2) {
  (a, b) -> a + b
  _ -> 0
}
```

**Records**

```axom
type User { name: String, age: Int }

let user = User { name: "Ada", age: 36 }
print user.name
```

**Sum types**

```axom
type Result { Ok(Int) Error(String) }

let value = Ok(42)
print match value {
  Ok(x) -> x
  Error(_) -> 0
}
```

### Editor Support (VS Code)

There is a minimal local VS Code extension for Axom syntax highlighting:

1. Open the Command Palette.
2. Run `Developer: Install Extension from Location...`.
3. Select `tools/vscode-axom`.

## Status

**In Development** — Steps 1-13 of the roadmap are complete, with pattern matching v1, records v1, and sum types v1 implemented. The compiler can parse, type-check, interpret, and generate C# code for basic programs. A full CLI with `check`, `build`, and `run` commands is available. Test infrastructure includes golden files and diagnostic snapshots, plus compilation caching and tooling support.

### Currently Implemented ✅

- Lexer and Parser with error recovery
- Variables (`let`, `let mut`) and assignments
- Primitive types (`Int`, `Float`, `Bool`, `String`)
- Arithmetic operators (`+`, `-`, `*`, `/`, `%`)
- Comparison operators (`==`, `!=`, `<`, `>`, `<=`, `>=`)
- Logical operators (`&&`, `||`, `!`)
- Unary operators (`-`, `+`)
- String concatenation (`+`)
- Numeric conversions (`float(Int)`, `int(Float)`)
- Blocks and scoped variables
- Functions and lambdas (`fn`, `return`)
- Recursion via `match` (no `if/while/for`)
- Pattern matching v1 (`match` with literals, `_`, identifiers, tuples)
- Records v1 (`type`, record literals, field access)
- Sum types v1 (variants with optional payloads)
- String escape sequences (`\n`, `\t`, `\r`, `\"`, `\\`)
- Binding and scope resolution
- Type checking
- Interpreter runtime
- Code generation (emits C#)
- Builtin functions: `print`, `println`, `input`, `len`, `abs`, `min`, `max`, `float`, `int`
- CLI commands: `check`, `build`, `run` with options
- Test infrastructure: golden files and diagnostic snapshots
- Compilation cache (`--cache`) and large input guardrail
- Tooling: dotnet tool packaging, CI workflow, shell completions

### Coming Soon 🔜
- Pattern matching (guards, lists, sum types)
- Generics
- `Result`/`Option` and error propagation (`?`)
- Collections (List, Map, Tuple)
- Iterator combinators
- Modules and imports
- Structured concurrency
- String interpolation

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
axom run path/to/file.axom
# or (from source)
dotnet run --project src/axom -- run path/to/file.axom
```

### Check a Axom Program

```bash
axom check path/to/file.axom
# or (from source)
dotnet run --project src/axom -- check path/to/file.axom
```

Validates the source code without generating output files.

### Compile to C#

```bash
axom build path/to/file.axom
# or (from source)
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
dotnet tool install -g Axom.CLI --prerelease
# or (local tool manifest)
dotnet new tool-manifest
dotnet tool install --local Axom.CLI --prerelease
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
- **[Roadmap](docs/roadmap/ROADMAP.md)** — Implementation progress and plans

## Roadmap

The current plan is the v0.5 roadmap (next minor release).

 - ✅ Steps 1–13 complete (pipeline through functions/lambdas)
 - ✅ Pattern match v1 complete
 - 📍 Full plan and current focus: [ROADMAP.md](docs/roadmap/ROADMAP.md)
 - 📄 Proposal: pipeline combinator expressions ([docs/proposals/pipeline-combinators.md](docs/proposals/pipeline-combinators.md))

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
