# Lume Language Specification (v0.1 – Draft)

Lume is a modern, Gleam-inspired programming language designed for .NET.
It takes inspiration from Gleam's elegant approach to error handling and
concurrency, while providing native interoperability with the .NET ecosystem.

Lume is a minimal, opinionated language focused on simplicity, explicit
error handling, and structured concurrency, while remaining fully
interoperable with existing C# code.

---

## Core Principles

- One obvious way to do things
- Explicit error handling
- Structured concurrency by default
- Immutability by default
- Full .NET interoperability
- Small, understandable language surface

---

## Syntax Notes

- Statement terminators are optional; newlines end statements by default.
- Semicolons are permitted as explicit separators (useful inside blocks).
- Identifiers may include underscores (`_`) and may start with `_`.

---

## Language Decisions (Summary)

This section captures agreed design choices to guide implementation.

### Functions
- Return is implicit from the last expression; `return` is allowed for early exit.
- Functions without an explicit return type have type `Unit`.
- No optional/default params or named arguments (for now).
- Lambdas: `fn(x) => x * 2` for single-expression; captures by value only; no `mut` capture.
- Top-level statements are allowed; `fn main()` is optional. CLI args later.

### Operators
- Only structural equality `==` (no `===`).
- Comparisons between different types are compile-time errors.
- Logical operators are `&&`, `||`, `!` only (no `and/or/not`).
- `+` is used for string concatenation.
- Division is integer-only for now; floats later.
- Overflow is checked (no silent wrap).
- Pipe operator `|>` planned for later, passes value as first argument.

### Types & Data
- `Int` is 64-bit. `Double` planned later. No `Char`, `Byte`, or `UInt` for now.
- Records use `type`, have auto-constructors, update syntax, and destructuring.
- Sum types use `Variant` / `Variant(value)` payload style.
- Generics use `<T>` with inference; no constraints for now.
- Type aliases are equivalent to the base type (no newtype yet).

### Collections & Strings
- Lists are immutable by default; indexing with `[]` returns `Result` on OOB.
- Maps have literal syntax; keys are `String` only for now.
- Tuples are included; access via destructuring only.
- String interpolation: `f"...{expr}..."` with `{}` expressions.
- String helpers (length/split) live in stdlib.

### Modules & Visibility
- One file = one module; no nested modules for now.
- `import` with aliasing and selective imports is supported.
- Visibility: `pub` or private only.

### Pattern Matching
- Supports rest patterns (`..`), guards, and list patterns; no range patterns yet.
- Non-exhaustive match is an error; `_` is optional but recommended.

### Option/Result
- Option/Result live in stdlib; `?` works for both.
- `.unwrap()` exists (panic on None/Error).
- Result error variant is `Error`; default error type is `String` for now.

### Comments & Docs
- Comments are not nested.
- Doc comments use `///` with Markdown; doc tooling later.

### Misc
- Shadowing in the same scope is not allowed.
- Mutual recursion is allowed; no forward-declare keyword.
- Attributes use `@attr`, planned for later.

---

## 1. Error Handling

### 1.1 Result and Option

Lume uses explicit types for failure:

- Result<T, E> = Ok(T) | Error(E)
- Option<T> = Some(T) | None

Functions that may fail MUST return Result or Option.
Exceptions are not used for control flow in the core language.

---

### 1.2 Error Propagation Operator `?`

The postfix operator `?` is defined only for Result<T, E>.

Semantics:
- Ok(x)?  → evaluates to x
- Error(e)? → returns Error(e) from the current function

Example:

```lume
pub fn load(id: Int) -> Result<User, Err> {
  let raw = db.get(id)?
  Ok(parse(raw)?)
}
```

---

## String Literals

String literals support the following escape sequences:

- `\n` newline
- `\t` tab
- `\r` carriage return
- `\\` backslash
- `\"` double quote

---

## Comments

Lume supports single-line and multi-line comments:

- `// single line`
- `/* multi line */`

Comments are not nested.

---

### 1.3 Pattern Matching

- `match` expressions must be exhaustive
- Non-exhaustive matches are compile-time errors
- Works on Result, Option, and sum types

---

### 1.4 .NET Exception Interop

Lume does not expose try/catch in the core language.

Interop with .NET exceptions is explicit via runtime helpers:

```lume
let x = DotNet.try(() => SomeApi.Call())?
```

---

## 2. Control Flow

### 2.1 No Traditional Control Structures

Lume intentionally omits `if`, `else`, `while`, `for`, and `loop`.

This is a deliberate design choice to enforce:
- Exhaustive handling via pattern matching
- Functional iteration patterns
- Predictable control flow

---

### 2.2 Branching via Pattern Matching

All conditional logic uses `match`:

```lume
let message = match x > 5 {
  true -> "large"
  false -> "small"
}
```

Pattern matching must be exhaustive. The compiler rejects non-exhaustive matches.

---

### 2.3 Iteration via Recursion

Custom iteration uses tail-recursive functions:

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

---

## Operators

Arithmetic operators include:

- `+` add/concat
- `-` subtract
- `*` multiply
- `/` integer division
- `%` remainder

---

### 2.4 Iteration via Standard Library

For collections, use iterator combinators:

```lume
// Iterate with side effects
items.each(fn(x) { println x })

// Transform
let doubled = items.map(fn(x) { x * 2 })

// Reduce
let sum = items.fold(0, fn(acc, x) { acc + x })

// Filter
let evens = items.filter(fn(x) { x % 2 == 0 })

// Numeric ranges
range(1, 10).each(fn(i) { println i })
```

---

### 2.5 Why No Imperative Loops?

- **One obvious way**: recursion OR iterators, never both for the same problem
- **Composability**: iterator chains are easier to reason about
- **Optimization**: no arbitrary control flow simplifies analysis
- **`let mut` purpose**: local accumulators, not manual loop counters

---

## 3. Concurrency & Parallelism

### 3.1 Effects and Suspension

A function is suspensive if it:
- calls another suspensive function
- performs I/O via the runtime

Backend mapping:
- Non-suspensive → sync .NET methods
- Suspensive → ValueTask<T>

---

### 3.2 Implicit Await

Sequential calls to suspensive functions implicitly await.

---

### 3.3 Structured Concurrency

Primitives:
- scope { }
- spawn expr
- task.join()

Fire-and-forget is intentionally impossible.

---

### 3.4 Cancellation

- Cancellation is implicit and scoped
- Blocking operations are forbidden

---

### 3.5 CPU Parallelism

```lume
let result = par compute(data)?
```

`par` is the only supported way to express CPU parallelism.

---

## 4. Mutability

### 4.1 Immutability by Default

All bindings are immutable by default.

---

### 4.2 Local Mutability

```lume
let mut x = 0
x = x + 1
```

- `mut` is scope-local
- Cannot be captured by spawned tasks

---

### 4.3 Mutable Containers

Provided by runtime:
- Cell<T>
- MutList<T>
- Atomic<T>
- Mutex<T>

Builders must be frozen to produce immutable values.

---

### 4.4 Concurrency and Mutation

- Shared state must be explicit
- Default concurrency is shared-state-free

---

## 5. Types & Data

- Primitive types: Int, Bool, String
- Records
- Sum types
- Generics (minimal)

---

## 6. Interoperability

- Direct .NET calls
- NuGet supported
- Standard .NET assemblies output

---

## 7. Philosophy Recap

- Errors are values
- Concurrency is structured
- Parallelism is explicit
- Mutation is controlled
- One obvious way

---

Status: Draft v0.1
