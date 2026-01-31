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

## 2. Concurrency & Parallelism

### 2.1 Effects and Suspension

A function is suspensive if it:
- calls another suspensive function
- performs I/O via the runtime

Backend mapping:
- Non-suspensive → sync .NET methods
- Suspensive → ValueTask<T>

---

### 2.2 Implicit Await

Sequential calls to suspensive functions implicitly await.

---

### 2.3 Structured Concurrency

Primitives:
- scope { }
- spawn expr
- task.join()

Fire-and-forget is intentionally impossible.

---

### 2.4 Cancellation

- Cancellation is implicit and scoped
- Blocking operations are forbidden

---

### 2.5 CPU Parallelism

```lume
let result = par compute(data)?
```

`par` is the only supported way to express CPU parallelism.

---

## 3. Mutability

### 3.1 Immutability by Default

All bindings are immutable by default.

---

### 3.2 Local Mutability

```lume
let mut x = 0
x = x + 1
```

- `mut` is scope-local
- Cannot be captured by spawned tasks

---

### 3.3 Mutable Containers

Provided by runtime:
- Cell<T>
- MutList<T>
- Atomic<T>
- Mutex<T>

Builders must be frozen to produce immutable values.

---

### 3.4 Concurrency and Mutation

- Shared state must be explicit
- Default concurrency is shared-state-free

---

## 4. Types & Data

- Primitive types: Int, Bool, String
- Records
- Sum types
- Generics (minimal)

---

## 5. Interoperability

- Direct .NET calls
- NuGet supported
- Standard .NET assemblies output

---

## 6. Philosophy Recap

- Errors are values
- Concurrency is structured
- Parallelism is explicit
- Mutation is controlled
- One obvious way

---

Status: Draft v0.1
