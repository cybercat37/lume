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
- **Progressive .NET interoperability** — controlled interop surface, expanded by milestone
- **Small, understandable language surface** — easy to learn and maintain

## Why Axom over C#

If you like the .NET ecosystem but want a smaller, more deliberate language, Axom offers a few concrete advantages:

- **Smaller surface area** — fewer constructs and one obvious way to do things means less incidental complexity than C#.
- **Explicit errors** — errors are values (`Result`/`Option`), making failure paths visible instead of hidden in exceptions.
- **Immutable by default** — safer state handling with mutation opt-in (`let mut`).
- **Match-first control flow** — `match` is the primary branching tool, keeping logic explicit and centralized.
- **.NET interop without losing Axom’s shape** — v1 supports controlled calls through `dotnet.call<T>` / `dotnet.try_call<T>`.
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

**Time and random builtins**

```axom
rand_seed(42)
let now = time_now_utc()
let later = time_add_ms(now, 250)
print time_diff_ms(later, now)
print time_to_iso(now)
print time_to_local_iso(now)
print time_from_iso(time_to_iso(now))
print rand_float()
print rand_int(10)
sleep(20)
clear()
```

- `sleep(ms)` waits for `ms` milliseconds (`ms <= 0` is a no-op)
- `clear()` clears the current console screen
- `time_from_iso(text)` returns `Ok(instant)` or `Error("invalid ISO-8601 instant")`
- `rand_int(max)` returns `Ok(n)` or `Error("max must be > 0")`

**Value pipe**

```axom
let score = -3 |> abs
print score
```

`value |> f` is shorthand for `f(value)`.

**Aspects (`@logging`)**

```axom
@logging fn add(a: Int, b: Int) -> Int {
  return a + b
}

print add(3, 4)
```

`@logging` emits timestamped call/return logs with arguments and return values.

**Collections + combinators**

```axom
let doubled = map([1, 2, 3], fn(x: Int) => x * 2)
let sum = [1, 2, 3]
  |> map(fn(x: Int) => x * 2)
  |> fold(0, fn(acc: Int, x: Int) => acc + x)
let first_two = take([10, 20, 30], 2)
let tail = skip([10, 20, 30], 1)
let prefix = take_while([1, 2, 3, 1], fn(x: Int) => x < 3)
let rest = skip_while([1, 2, 3, 1], fn(x: Int) => x < 3)
let indexed = enumerate([10, 20])
let pairs = zip([1, 2, 3], ["a", "b"])
let pair_sums = zip_with([1, 2, 3], [10, 20], fn(x: Int, y: Int) => x + y)

print doubled
print sum
print first_two
print tail
print prefix
print rest
print indexed
print pairs
print pair_sums
```

**Range + pipelines**

```axom
print range(1, 6)
print range(1, 10, 3)
print range(5, 0, -2)

let squares = range(1, 6)
  |> map(fn(x: Int) => x * x)
print squares

let sum = range(1, 6)
  |> fold(0, fn(acc: Int, x: Int) => acc + x)
print sum
```

`range(start, end)` is half-open (`start` included, `end` excluded).
`range(start, end, step)` supports positive or negative step.

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
let updated = user with { age: 37 }
let copy = User { ...user }
print user.name
print updated.age
```

Record rule of thumb:
- use `with` for updates (`user with { age: 37 }`)
- use spread only for copy literals (`User { ...user }`)

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

## Status and Roadmap

Project status, implemented features, and future plans live in the single source of truth:
- `roadmap.md`

Status labels used across docs: `Implemented`, `Partial`, `Planned`.

Current concurrency status (Partial):
- Structured concurrency uses `scope` + `spawn { ... }` + `task.join()`.
- Channel messaging v1 is available with `channel<T>()`, `send`, and blocking `recv`.
- Strict channel semantics are available: `recv` returns `Result<T, String>` and must be handled via `?` or `match`.
- Scope-owned channel close is implemented; blocked `recv` unblocks with `Error("channel closed")`.
- Bounded channel capacity is available (`channel<T>(N)`, default `64`).
- Scope cancellation propagation is implemented (`Error("cancelled")` on interrupted channel ops).
- Advanced backpressure policies are follow-up work.

Current aspects status (Partial):
- `@logging` is implemented for function declarations (`@logging fn ...`).
- Interpreter and codegen both emit timestamped invocation/return logs.
- `@timeout(ms)` is implemented for functions returning `Result<T, String>`.
- Additional aspects (`@retry`, webhook/mqtt policies) are planned.

Current intent status (Partial):
- `@intent("...")` metadata on `let` and blocks is implemented.
- Intent effect-mismatch warnings are currently disabled.

Current HTTP + DB track status (Early/Partial):
- `axom serve <file.axom>` is available with a runtime health endpoint (`GET /health`).
- File-based route discovery is available from `routes/**/*.axom` (method suffixes, `index`, dynamic params like `__id_int`).
- Route conflict diagnostics run before server start.
- Discovered routes are currently served as runtime stubs; Axom handler execution is next.
- HTTP client, DB runtime, typed SQL interpolation, and auth/security DSL are planned in the M13-M21 track.

Design references:
- `docs/proposals/http-db-reference.md` (vision/reference)
- `docs/roadmap/http-db-plan.md` (implementation plan)

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
# runnable repo demo
make demo-example
```

### Serve HTTP (Bootstrap)

```bash
axom serve path/to/file.axom --host 127.0.0.1 --port 8080
# or (from source)
dotnet run --project src/axom -- serve path/to/file.axom --host 127.0.0.1 --port 8080
```

Quick check:

```bash
curl -i http://127.0.0.1:8080/health
```

Route files under `routes/**/*.axom` are discovered and mounted as stub endpoints.

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
- `--host <addr>` — Bind host for `serve` (default: `127.0.0.1`)
- `--port <n>` — Bind port for `serve` (default: `8080`)
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
- **[Roadmap](roadmap.md)** — Implementation progress and plans

## Roadmap

See `roadmap.md` for the complete, consolidated roadmap.

## Language Features

### No Traditional Control Flow

Axom intentionally omits `if`, `while`, `for`, and `loop`. Instead:

- **Pattern matching** with `match` for all branching
- **Tail recursion** for custom iteration
- **Iterator combinators** (`each`, `map`, `fold`, `filter`) for collections

### Explicit Error Handling

```axom
pub fn load(id: Int) -> Result<User, String> {
  let raw = db.get(id)?
  Ok(parse(raw)?)
}
```

Errors are values (`Result`/`Option`), not exceptions.

### Immutability by Default

```axom
let x = 10
let mut y = 20
y = y + 1
```

`let` is immutable by default; `let mut` enables local mutable state.

## Contributing

See [`CONTRIBUTING.md`](CONTRIBUTING.md) for guidelines on how to contribute to Axom.

## License

Licensed under the Apache License 2.0. See [`LICENSE`](LICENSE) for details.
