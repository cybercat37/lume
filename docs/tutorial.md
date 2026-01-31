# Lume Tutorial

Concise tutorial for experienced developers who want to learn Lume quickly.

> **Status note**: This tutorial covers all language features as per the specification. Implemented features are marked with ✅, planned ones with 🔜.

---

## 1. Quick Start

### Hello World

```lume
print "Hello, Lume!"
```

Run the code:

```bash
# With Makefile
make run FILE=hello.lume

# Or with dotnet
dotnet run --project src/lume -- run hello.lume
```

**Status**: ✅ Implemented

---

## 2. Fundamentals

### 2.1 Variables

Variables are immutable by default:

```lume
let x = 42
let name = "Lume"
let is_active = true
```

For mutable variables, use `let mut`:

```lume
let mut counter = 0
counter = counter + 1
```

**Status**: ✅ Implemented (`let`, `let mut`, assignments)

---

### 2.2 Primitive Types

| Type | Description | Example |
|------|-------------|---------|
| `Int` | 64-bit integer | `42`, `-10` |
| `Bool` | Boolean | `true`, `false` |
| `String` | UTF-8 string | `"hello"` |

**Status**: ✅ Implemented

---

### 2.3 Operators

#### Arithmetic

```lume
let sum = 10 + 5
let diff = 10 - 5
let prod = 10 * 5
let quot = 10 / 3
let rem = 10 % 3
```

#### Comparison

```lume
let eq = 5 == 5
let ne = 5 != 3
let lt = 3 < 5
let gt = 5 > 3
let le = 5 <= 5
let ge = 5 >= 3
```

#### Logical

```lume
let and = true && false
let or = true || false
let not = !false
```

#### Strings

```lume
let msg = "Hello" + " " + "World"
```

**Status**: ✅ Implemented (arithmetic, logical, comparison, string concatenation)

---

### 2.4 Blocks and Scope

Blocks create local scopes:

```lume
let x = 10
{
  let y = 20
  print x
  print y
}
```

**Status**: ✅ Implemented

---

### 2.5 Comments

```lume
// Single-line comment

/* Multi-line
   comment */

// Comments are not nested
```

**Status**: 🔜 Planned (`//` and `/* */` not yet implemented)

---

### 2.6 String Literals and Escape Sequences

```lume
let newline = "line1\nline2"
let tab = "col1\tcol2"
let quote = "He said \"Hello\""
let backslash = "path\\to\\file"
```

**Status**: ✅ Implemented (`\n`, `\t`, `\r`, `\"`, `\\`)

---

## 3. Control Flow

### 3.1 No If/While/For

Lume **does not have** `if`, `else`, `while`, `for`, or `loop`. This is an intentional design choice.

**Status**: 🔜 Design finalized, future implementation

---

### 3.2 Pattern Matching with `match`

All control flow uses `match`:

```lume
let message = match x > 5 {
  true -> "large"
  false -> "small"
}
```

Pattern matching must be exhaustive:

```lume
let result = match value {
  Ok(x) -> f"Got: {x}"
  Error(e) -> f"Error: {e}"
}
```

**Status**: 🔜 Planned

---

### 3.3 Iteration with Recursion

For custom iteration, use tail-recursive functions:

```lume
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

```lume
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

```lume
fn add(a: Int, b: Int) -> Int {
  a + b
}
```

Return is implicit from the last expression. Explicit `return` is allowed for early exit.

**Status**: 🔜 Planned

---

### 4.2 Functions without Return Type

```lume
fn greet(name: String) {
  println f"Hello, {name}!"
}
```

Functions without a return type have type `Unit`.

**Status**: 🔜 Planned

---

### 4.3 Lambdas

```lume
let double = fn(x: Int) => x * 2
let result = double(5)
```

Multi-line lambdas:

```lume
let process = fn(x: Int) {
  let doubled = x * 2
  doubled + 1
}
```

**Status**: 🔜 Planned

---

### 4.4 Top-level Statements

```lume
let x = 10
print x
```

`fn main()` is optional. CLI arguments will be added in the future.

**Status**: 🔜 Top-level statements planned

---

## 5. Composite Types

### 5.1 Records

```lume
type User {
  name: String
  age: Int
}

let user = User { name: "Alice", age: 30 }
let name = user.name
```

**Status**: 🔜 Planned

---

### 5.2 Sum Types (Enums with Payload)

```lume
type Result<T, E> {
  Ok(T)
  Error(E)
}

let success = Ok(42)
let failure = Error("Something went wrong")
```

**Status**: 🔜 Planned

---

### 5.3 Generics

```lume
fn identity<T>(x: T) -> T {
  x
}

let num = identity(42)
let str = identity("hello")
```

**Status**: 🔜 Planned

---

### 5.4 Tuples

```lume
let pair = (1, "hello")
let (x, y) = pair
```

**Status**: 🔜 Planned

---

## 6. Error Handling

### 6.1 Result and Option

Lume uses explicit types for error handling:

```lume
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

```lume
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

```lume
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

```lume
let value = result.unwrap()
let name = option.unwrap()
```

Use only when you're certain there won't be an error.

**Status**: 🔜 Planned

---

### 6.5 .NET Exception Interop

Lume does not expose try/catch in the core language. Interop with .NET exceptions is explicit:

```lume
let x = DotNet.try(() => SomeApi.Call())?
```

**Status**: 🔜 Planned

---

## 7. Collections

### 7.1 List

```lume
let numbers = [1, 2, 3, 4, 5]
let first = numbers[0]?
```

Lists are immutable by default.

**Status**: 🔜 Planned

---

### 7.2 Map

```lume
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

```lume
[1, 2, 3].each(fn(x) { println x })

let doubled = [1, 2, 3].map(fn(x) { x * 2 })

let sum = [1, 2, 3].fold(0, fn(acc, x) { acc + x })

let evens = [1, 2, 3, 4].filter(fn(x) { x % 2 == 0 })
```

**Status**: 🔜 Planned (Step 8: Standard Library)

---

### 7.4 Range

```lume
range(1, 10).each(fn(i) { println i })
```

**Status**: 🔜 Planned

---

## 8. Modules and Visibility

### 8.1 One File = One Module

Each `.lume` file is a module. No nested modules for now.

**Status**: 🔜 Planned

---

### 8.2 Import

```lume
import std.io
import std.collections as coll
import std.math.{max, min}
```

**Status**: 🔜 Planned

---

### 8.3 Visibility

```lume
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

```lume
let data = fetch_data()
let processed = process(data)
```

**Status**: 🔜 Planned

---

### 9.3 Structured Concurrency

```lume
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

```lume
let result = par compute(data)?
```

`par` is the only supported way to express CPU parallelism.

**Status**: 🔜 Planned

---

## 10. Mutability

### 10.1 Immutability by Default

All bindings are immutable by default:

```lume
let x = 10
```

**Status**: ✅ Implemented

---

### 10.2 Local Mutability

```lume
let mut counter = 0
counter = counter + 1
```

- `mut` is scope-local
- Cannot be captured by spawned tasks

**Status**: ✅ Implemented

---

### 10.3 Mutable Containers

Provided by runtime:

```lume
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

```lume
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

```lume
let name = "Alice"
let age = 30
let msg = f"Hello, {name}! You are {age} years old."
```

**Status**: 🔜 Planned

---

## 13. Best Practices

### 13.1 Error Handling

✅ **Prefer** `Result`/`Option` and pattern matching:
```lume
match result {
  Ok(value) -> process(value)
  Error(e) -> handle_error(e)
}
```

❌ **Avoid** `.unwrap()` unless necessary:
```lume
let value = result.unwrap()
```

---

### 13.2 Control Flow

✅ **Use** `match` for all conditions:
```lume
let message = match condition {
  true -> "yes"
  false -> "no"
}
```

❌ **Don't expect** `if`/`while`/`for` - they don't exist in the language.

---

### 13.3 Iteration

✅ **Prefer** iterator combinators:
```lume
let sum = numbers.fold(0, fn(acc, x) { acc + x })
```

✅ **Use** tail recursion for custom iteration:
```lume
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
```lume
let x = compute()
```

✅ **Use** `let mut` only for local accumulators:
```lume
let mut sum = 0
numbers.each(fn(x) { sum = sum + x })
```

❌ **Don't use** `mut` for manual loop counters - use iterators or recursion.

---

## 14. References

- **Full specification**: [docs/spec.md](spec.md)
- **Roadmap**: [ROADMAP.md](../../ROADMAP.md)
- **Agent guide**: [AGENTS.md](../../AGENTS.md)

---

## 15. Implementation Status (Summary)

### ✅ Implemented (Steps 1-7)

- Base Lexer and Parser
- Variables (`let`, `let mut`)
- Primitive types (`Int`, `Bool`, `String`)
- Operators (arithmetic, logical, comparison)
- Blocks and scope
- Comments (planned, not yet implemented)
- String escape sequences
- Binding and scope resolution
- Base type checking
- Interpreter runtime
- Code generation (C# emission)
- Some builtin functions (`print`, `println`, `input`)

### 🔜 Planned

- Pattern matching (`match`)
- Functions and lambdas
- Records and Sum types
- Generics
- `Result`/`Option` and `?` operator
- Collections (List, Map, Tuple)
- Iterator combinators
- Modules and `import`
- Structured concurrency
- String interpolation
- Complete .NET interop

---

**Last updated**: January 2026
