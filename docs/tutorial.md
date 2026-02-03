# Axom Tutorial

Concise tutorial for experienced developers who want to learn Axom quickly.

Axom favors explicit flow and a compact language surface. Instead of many branching constructs, it centers on `match` and a few orthogonal concepts, so behavior stays obvious at a glance.

> **Status note**: This tutorial covers all language features as per the specification. Implemented features are marked with ✅, planned ones with 🔜.

---

## 1. Quick Start

### Hello World

```axom
print "Hello, Axom!"
```

Run the code:

```bash
axom run hello.axom

# Or from source
dotnet run --project src/axom -- run hello.axom
```

You can also validate without generating output:

```bash
axom check hello.axom
```

Build to C# without running:

```bash
axom build hello.axom
```

### Install via NuGet (dotnet tool)

If you want the CLI without cloning the repo, install the tool from NuGet:

```bash
dotnet tool install --global Axom.CLI --prerelease
axom --version
axom run hello.axom
```

To update later:

```bash
dotnet tool update --global Axom.CLI --prerelease
```

See `examples/nuget-tool/README.md` for a local tool manifest example.

**Status**: ✅ Implemented (match v1: literals, wildcard, identifier, tuples)

---

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

**Mutable variable with block scope**

```axom
let mut total = 0
{
  let x = 10
  let y = 20
  total = x + y
}
print total
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

**Status**: ✅ Implemented

---

## 2. Fundamentals

### 2.1 Variables

Variables are immutable by default:

```axom
let x = 42
let name = "Axom"
let is_active = true
```

For mutable variables, use `let mut`:

```axom
let mut counter = 0
counter = counter + 1
```

**Status**: ✅ Implemented (`let`, `let mut`, assignments)

---

### 2.2 Primitive Types

| Type | Description | Example |
|------|-------------|---------|
| `Int` | 64-bit integer | `42`, `-10` |
| `Float` | 64-bit float | `1.5`, `0.25` |
| `Bool` | Boolean | `true`, `false` |
| `String` | UTF-8 string | `"hello"` |

**Status**: ✅ Implemented

Numeric conversions are explicit and provided by builtin functions:

```axom
let f = float(2)
let i = int(3.5)
```

---

### 2.3 Operators

#### Arithmetic

```axom
let sum = 10 + 5
let diff = 10 - 5
let prod = 10 * 5
let quot = 10 / 3
let rem = 10 % 3
let fsum = 1.5 + 2.25
```

#### Comparison

```axom
let eq = 5 == 5
let ne = 5 != 3
let lt = 3 < 5
let gt = 5 > 3
let le = 5 <= 5
let ge = 5 >= 3
```

#### Logical

```axom
let and = true && false
let or = true || false
let not = !false
```

#### Strings

```axom
let msg = "Hello" + " " + "World"
```

**Status**: ✅ Implemented (arithmetic, comparison, logical, string concatenation)

---

### 2.4 Blocks and Scope

Blocks create local scopes:

```axom
let x = 10
{
  let y = 20
  print x
  print y
}
```

**Status**: ✅ Implemented

---

### 2.5 Intent Annotations

Intent annotations are built-in attributes used for diagnostics and documentation.

```axom
@intent("Validate and normalize inputs")
{
  let name = input()
  print len(name)
}

let user_id @intent("Lookup user id") = input()
```

Annotations have no runtime effect. Tooling may warn if intent does not match inferred effects.

**Status**: 🔜 Planned

---

### 2.6 String Literals and Escape Sequences

```axom
let newline = "line1\nline2"
let tab = "col1\tcol2"
let quote = "He said \"Hello\""
let backslash = "path\\to\\file"
```

**Status**: ✅ Implemented (`\n`, `\t`, `\r`, `\"`, `\\`)

---

## 3. Control Flow

### 3.1 No If/While/For

Axom **does not have** `if`, `else`, `while`, `for`, or `loop`. This is an intentional design choice.

**Status**: 🔜 Design finalized, future implementation

---

### 3.2 Pattern Matching with `match`

All control flow uses `match`:

```axom
let message = match count {
  0 -> "none"
  _ -> "some"
}
```

Supported patterns (v1): literals, `_`, identifiers, and tuples:

```axom
print match (1, 2) {
  (a, b) -> a + b
  _ -> 0
}
```

Pattern matching must be exhaustive (use `_` when needed).

**Status**: ✅ Implemented (subset)

---

### 3.3 Iteration with Recursion

For custom iteration, use tail-recursive functions:

```axom
fn countdown(n: Int) {
  match n {
    0 -> println "done"
    _ -> {
      println n
      countdown(n - 1)
    }
  }
}
```

The compiler optimizes tail calls to prevent stack overflow.

**Status**: 🔜 Planned

---

### 3.4 Iteration with Standard Library

For collections, use iterator combinators:

```axom
items.each(fn(x) { println x })

let doubled = items.map(fn(x) { x * 2 })

let sum = items.fold(0, fn(acc, x) { acc + x })

let evens = items.filter(fn(x) { x % 2 == 0 })

range(1, 10).each(fn(i) { println i })
```

**Status**: 🔜 Planned (Step 8: Standard Library)

---

## 4. Functions

### 4.1 Basic Definition

```axom
fn add(a: Int, b: Int) -> Int {
  a + b
}
```

Return is implicit from the last expression. Explicit `return` is allowed for early exit.

**Status**: ✅ Implemented

---

### 4.2 Functions without Return Type

```axom
fn greet(name: String) {
  println f"Hello, {name}!"
}
```

Functions without a return type have type `Unit`.

**Status**: ✅ Implemented

---

### 4.3 Lambdas

```axom
let double = fn(x: Int) => x * 2
let result = double(5)
```

Multi-line lambdas:

```axom
let process = fn(x: Int) {
  let doubled = x * 2
  doubled + 1
}
```

**Status**: ✅ Implemented

---

### 4.4 Top-level Statements

```axom
let x = 10
print x
```

`fn main()` is optional. CLI arguments will be added in the future.

**Status**: ✅ Implemented

---

## 5. Composite Types

### 5.1 Records

```axom
type User {
  name: String
  age: Int
}

let user = User { name: "Alice", age: 30 }
let name = user.name
```

**Status**: ✅ Implemented

---

### 5.2 Sum Types (Enums with Payload)

```axom
type Result<T, E> {
  Ok(T)
  Error(E)
}

let success = Ok(42)
let failure = Error("Something went wrong")
```

**Status**: ✅ Implemented

---

### 5.3 Generics

```axom
fn identity<T>(x: T) -> T {
  x
}

let num = identity(42)
let str = identity("hello")
```

**Status**: 🔜 Planned

---

### 5.4 Tuples

```axom
let pair = (1, "hello")
let (x, y) = pair
```

**Status**: 🔜 Planned

---

## 6. Error Handling

### 6.1 Result and Option

Axom uses explicit types for error handling:

```axom
type Result<T, E> {
  Ok(T)
  Error(E)
}

type Option<T> {
  Some(T)
  None
}
```

Functions that may fail **must** return `Result` or `Option`. Exceptions are not used for control flow.

**Status**: 🔜 Planned (Step 8: Standard Library)

---

### 6.2 Propagation Operator `?`

The postfix operator `?` works on both `Result` and `Option`:

```axom
pub fn load(id: Int) -> Result<User, String> {
  let raw = db.get(id)?
  Ok(parse(raw)?)
}

pub fn get_name(id: Int) -> Option<String> {
  let user = find_user(id)?
  Some(user.name)
}
```

Semantics:
- `Ok(x)?` → evaluates to `x`
- `Error(e)?` → returns `Error(e)` from the current function
- `Some(x)?` → evaluates to `x`
- `None?` → returns `None` from the current function

**Status**: 🔜 Planned

---

### 6.3 Pattern Matching on Errors

```axom
let message = match result {
  Ok(value) -> f"Got: {value}"
  Error(e) -> f"Error: {e}"
}

let name = match maybe_name {
  Some(n) -> n
  None -> "Unknown"
}
```

**Status**: 🔜 Planned

---

### 6.4 .unwrap()

```axom
let value = result.unwrap()
let name = option.unwrap()
```

Use only when you're certain there won't be an error.

**Status**: 🔜 Planned

---

### 6.5 .NET Exception Interop

Axom does not expose try/catch in the core language. Interop with .NET exceptions is explicit:

```axom
let x = DotNet.try(() => SomeApi.Call())?
```

**Status**: 🔜 Planned

---

## 7. Collections

### 7.1 List

```axom
let numbers = [1, 2, 3, 4, 5]
let first = numbers[0]?
```

Lists are immutable by default.

**Status**: 🔜 Planned

---

### 7.2 Map

```axom
let map = {
  "name" -> "Alice",
  "age" -> "30"
}
let name = map["name"]?
```

Keys are `String` only for now.

**Status**: 🔜 Planned

---

### 7.3 Iterator Combinators

```axom
[1, 2, 3].each(fn(x) { println x })

let doubled = [1, 2, 3].map(fn(x) { x * 2 })

let sum = [1, 2, 3].fold(0, fn(acc, x) { acc + x })

let evens = [1, 2, 3, 4].filter(fn(x) { x % 2 == 0 })
```

**Status**: 🔜 Planned (Step 8: Standard Library)

---

### 7.4 Range

```axom
range(1, 10).each(fn(i) { println i })
```

**Status**: 🔜 Planned

---

## 8. Modules and Visibility

### 8.1 One File = One Module

Each `.axom` file is a module. No nested modules for now.

**Status**: 🔜 Planned

---

### 8.2 Import

```axom
import std.io
import std.collections as coll
import std.math.{max, min}
```

**Status**: 🔜 Planned

---

### 8.3 Visibility

```axom
pub fn public_function() { }
fn private_function() { }
```

**Status**: 🔜 Planned

---

## 9. Concurrency and Parallelism

### 9.1 Effects and Suspension

A function is "suspensive" if it:
- calls another suspensive function
- performs I/O via the runtime

Backend mapping:
- Non-suspensive → synchronous .NET methods
- Suspensive → `ValueTask<T>`

**Status**: 🔜 Planned

---

### 9.2 Implicit Await

Sequential calls to suspensive functions implicitly await:

```axom
let data = fetch_data()
let processed = process(data)
```

**Status**: 🔜 Planned

---

### 9.3 Structured Concurrency

```axom
scope {
  let task1 = spawn compute1()
  let task2 = spawn compute2()
  let result1 = task1.join()?
  let result2 = task2.join()?
  result1 + result2
}
```

Fire-and-forget is intentionally impossible.

**Status**: 🔜 Planned

---

### 9.4 Cancellation

- Cancellation is implicit and scoped
- Blocking operations are forbidden

**Status**: 🔜 Planned

---

### 9.5 CPU Parallelism

```axom
let result = par compute(data)?
```

`par` is the only supported way to express CPU parallelism.

**Status**: 🔜 Planned

---

## 10. Mutability

### 10.1 Immutability by Default

All bindings are immutable by default:

```axom
let x = 10
```

**Status**: ✅ Implemented

---

### 10.2 Local Mutability

```axom
let mut counter = 0
counter = counter + 1
```

- `mut` is scope-local
- Cannot be captured by spawned tasks

**Status**: ✅ Implemented

---

### 10.3 Mutable Containers

Provided by runtime:

```axom
let cell = Cell.new(0)
cell.set(10)
let value = cell.get()
```

Available types:
- `Cell<T>`: single mutable cell
- `MutList<T>`: mutable list
- `Atomic<T>`: atomic value
- `Mutex<T>`: mutex-protected value

Builders must be "frozen" to produce immutable values.

**Status**: 🔜 Planned

---

## 11. .NET Interoperability

### 11.1 Direct Calls

```axom
let result = System.Console.ReadLine()
let number = Int32.Parse("42")
```

**Status**: 🔜 Planned

---

### 11.2 NuGet

NuGet packages can be used directly.

**Status**: 🔜 Planned

---

### 11.3 Standard .NET Output

Generated code compiles to standard .NET assemblies.

**Status**: ✅ Implemented (C# emission)

---

## 12. String Interpolation

```axom
let name = "Alice"
let age = 30
let msg = f"Hello, {name}! You are {age} years old."
```

**Status**: 🔜 Planned

---

## 13. Best Practices

### 13.1 Error Handling

✅ **Prefer** `Result`/`Option` and pattern matching:
```axom
match result {
  Ok(value) -> process(value)
  Error(e) -> handle_error(e)
}
```

❌ **Avoid** `.unwrap()` unless necessary:
```axom
let value = result.unwrap()
```

---

### 13.2 Control Flow

✅ **Use** `match` for all conditions:
```axom
let message = match condition {
  true -> "yes"
  false -> "no"
}
```

❌ **Don't expect** `if`/`while`/`for` - they don't exist in the language.

---

### 13.3 Iteration

✅ **Prefer** iterator combinators:
```axom
let sum = numbers.fold(0, fn(acc, x) { acc + x })
```

✅ **Use** tail recursion for custom iteration:
```axom
fn countdown(n: Int) {
  match n {
    0 -> done()
    _ -> {
      process(n)
      countdown(n - 1)
    }
  }
}
```

---

### 13.4 Mutability

✅ **Maintain** immutability by default:
```axom
let x = compute()
```

✅ **Use** `let mut` only for local accumulators:
```axom
let mut sum = 0
numbers.each(fn(x) { sum = sum + x })
```

❌ **Don't use** `mut` for manual loop counters - use iterators or recursion.

---

## 14. References

- **Full specification**: [docs/spec.md](spec.md)
- **Roadmap**: [ROADMAP.md](docs/roadmap/ROADMAP.md)
- **Agent guide**: [AGENTS.md](../../AGENTS.md)

---

## 15. Implementation Status (Summary)

### ✅ Implemented

- Lexer and Parser with error recovery
- Variables (`let`, `let mut`) and assignments
- Primitive types (`Int`, `Float`, `Bool`, `String`)
- Arithmetic operators (`+`, `-`, `*`, `/`, `%`)
- Comparison operators (`==`, `!=`, `<`, `>`, `<=`, `>=`)
- Logical operators (`&&`, `||`, `!`)
- Unary operators (`-`, `+`)
- String concatenation (`+`)
- Blocks and scoped variables
- Functions and lambdas (`fn`, `return`)
- Pattern matching v1 (`match` with literals, `_`, identifiers, tuples)
- Records v1 (`type`, record literals, field access)
- Sum types v1 (variants with optional payloads)
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

### 🔜 Planned

- Explicit numeric conversions (`float(Int)`, `int(Float)`)
- Pattern matching (guards, lists, sum types)
- Generics
- `Result`/`Option` and error propagation (`?`)
- Collections (List, Map, Tuple)
- Iterator combinators
- Modules and imports
- Structured concurrency
- String interpolation

---

**Last updated**: February 2026
