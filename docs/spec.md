# Axom Language Specification (v0.4 – Draft)

Axom is a modern programming language designed for .NET. It is intentionally
small, with a clear bias toward explicit flow, predictable semantics, and
systems that are easy to reason about in large codebases.

Axom favors a compact surface area over a large feature set. It elevates
explicit error handling, puts control flow in `match`, and keeps the runtime
model straightforward, while remaining fully interoperable with existing C#
code.

---

## Core Principles

- One obvious way to do things
- Explicit error handling
- Structured concurrency by default
- Immutability by default
- Progressive .NET interoperability
- Small, understandable language surface

---

## Language Decisions (Summary)

This section captures agreed design choices to guide implementation.

Status labels used across docs: `Implemented`, `Partial`, `Planned`.

### Syntactic Sugar Acceptance Criteria
- Sugar is accepted only when it improves readability in common, real code paths.
- Sugar must keep one obvious way at the usage site; avoid parallel equivalent forms.
- Desugaring must be direct and predictable (simple source-to-source rewrite).
- Diagnostics must remain clear and point to user-written syntax.
- Sugar should not increase parser/binder complexity disproportionately.
- If these criteria are not met, keep the explicit core form.
- Current approved sugar exceptions: relational match arms (`<= 1`) and value pipe (`|>`).

### Functions
- Parameters require type annotations: `fn add(x: Int, y: Int) -> Int { x + y }`.
- Return is implicit from the last expression; `return` is allowed for early exit.
- Functions without an explicit return type have type `Unit`.
- No optional/default params or named arguments (for now).
- Lambdas: `fn(x: Int) => x * 2` for single-expression; captures by value only; no `mut` capture.
- Top-level statements are allowed; `fn main()` is optional. CLI args later.

### Operators
- Only structural equality `==` (no `===`).
- Comparisons between different types are compile-time errors.
- Logical operators are `&&`, `||`, `!` only (no `and/or/not`).
- `+` is used for string concatenation.
- Division is integer-only for now; floats later.
- Overflow is checked (no silent wrap).
- Pipe operator `|>` is implemented as value pipe, passes value as first argument.

### Types & Data
- `Int` is 64-bit. `Float` is 64-bit (double precision). No `Char`, `Byte`, or `UInt` for now.
- Numeric conversions are explicit: `float(Int) -> Float` and `int(Float) -> Int`.
- Records use `type` with literal construction `User { name: "Ada" }` and update syntax `user with { age: 37 }`.
- Sum types use `Variant` / `Variant(value)` payload style.
- Generics use `<T>` with inference; no constraints for now.
- Type aliases are equivalent to the base type (no newtype yet).

### Collections & Strings
- Lists are immutable by default; indexing with `[]` returns `Result` on OOB.
- Maps have literal syntax; keys are `String` only for now.
- Tuples are included; access via destructuring only.
- String interpolation: `f"...{expr}..."` with `{}` expressions.
- String helpers (length/split) live in stdlib.
- Function-style collection combinators are implemented: `map`, `filter`, `fold`, `each`, `range`.
- Time/random builtins are available: `sleep(ms)`, `rand_float()`, `rand_int(max)`, `rand_seed(seed)`.
- Dedicated pipeline-combinator expression syntax remains proposed
  (see `docs/proposals/pipeline-combinators.md`).

Builtin notes:
- `sleep(ms: Int) -> Unit` blocks for `ms` milliseconds (`ms <= 0` is a no-op).
- `rand_float() -> Float` returns a value in `[0.0, 1.0)`.
- `rand_int(max: Int) -> Result<Int, String>` returns `Ok(n)` for `0 <= n < max`,
  or `Error("max must be > 0")` when `max <= 0`.
- `rand_seed(seed: Int) -> Unit` resets random state for deterministic runs/tests.
- `range(start: Int, end: Int, step?: Int) -> List<Int>` returns a half-open sequence;
  positive and negative `step` values are supported, and `step = 0` yields `[]`.

Range semantics examples:
- `range(1, 5)` -> `[1, 2, 3, 4]`
- `range(1, 10, 0)` -> `[]`
- `range(5, 5)` -> `[]`
- `range(1, 5, -1)` -> `[]` (step sign does not move toward `end`)
- `range(5, 1, -2)` -> `[5, 3]`

Value pipe examples:
- `value |> f` desugars to `f(value)`
- `value |> f(a, b)` desugars to `f(value, a, b)`
- `map(items, x -> x * 2)` is shorthand for `map(items, fn(x: T) => x * 2)` when `T` is inferred from context.

### Modules & Visibility
- One file = one module; no nested modules for now.
- Implemented v1 forms: `import mod`, `import mod as alias`, `from mod import name[, ...]`.
- Alias forms are supported for `from mod import name as alias` (values and types).
- Visibility (`pub`/private) is enforced across module boundaries.

### Aspects (Partial)
- Builtin aspect tags use identifier syntax on declarations (for example `@logging`).
- `@logging` on `fn` is implemented in interpreter and codegen.
- Logging emits timestamped invocation/return lines and includes arguments/return values.
- Additional aspects (`@retry`, `@timeout`, webhook/mqtt policies) are planned.

### Pattern Matching
- Current implementation supports literal, relational, `_`, identifier, tuple, and record patterns.
- Non-exhaustive match is an error; `_` is optional but recommended.
- Guards for record/variant matches are implemented; list/rest patterns are planned.

### Option/Result
- Option/Result live in stdlib; `?` applies to both `Result` and `Option`.
- `.unwrap()` exists (panic on None/Error).
- Result error variant is `Error`; default error type is `String` for now.

### Comments & Docs
- Traditional comments are not supported; intent annotations are the planned alternative.

### Intent Annotations (Partial)
- Intent annotations are structured metadata, not general-purpose comments.
- Syntax: `@intent("...")` where the argument is a string literal.
- May appear on blocks and `let` bindings; function-level intent is a future extension.
- No runtime effect; current implementation carries metadata through parser/binder/lowering.
- Effect-mismatch warnings are currently disabled while intent UX is refined.
- `@intent` is a built-in attribute; user-defined attributes use `@attr` later.

Syntax sketch (planned):

```
IntentAnnotation := "@intent" "(" StringLiteral ")"
Block := IntentAnnotation? "{" Statements "}"
LetStmt := "let" Identifier IntentAnnotation? "=" Expression
```

### Misc
- Shadowing in the same scope is not allowed.
- Mutual recursion is allowed; no forward-declare keyword.
- Attributes use `@attr`, planned for later (besides built-in `@intent`).

---

## 1. Syntax Basics

### 1.1 Identifiers

- Identifiers may include underscores (`_`) and may start with `_`.
- Statement terminators are optional; newlines end statements by default.
- Semicolons are permitted as explicit separators (useful inside blocks).

---

### 1.2 String Literals

String literals support the following escape sequences:

| Escape | Meaning |
|--------|---------|
| `\n` | newline |
| `\t` | tab |
| `\r` | carriage return |
| `\\` | backslash |
| `\"` | double quote |

---

### 1.3 Operators

#### Arithmetic

| Operator | Meaning |
|----------|---------|
| `+` | add (also string concatenation) |
| `-` | subtract |
| `*` | multiply |
| `/` | integer division |
| `%` | remainder |

#### Comparison

| Operator | Meaning |
|----------|---------|
| `==` | structural equality |
| `!=` | inequality |
| `<` | less than |
| `>` | greater than |
| `<=` | less or equal |
| `>=` | greater or equal |

#### Logical

| Operator | Meaning |
|----------|---------|
| `&&` | AND (short-circuit) |
| `\|\|` | OR (short-circuit) |
| `!` | NOT |

---

## 2. Error Handling

### 2.1 Result and Option

Axom uses explicit types for failure:

- `Result<T, E> = Ok(T) | Error(E)`
- `Option<T> = Some(T) | None`

Functions that may fail MUST return Result or Option.
Exceptions are not used for control flow in the core language.

---

### 2.2 Error Propagation Operator `?`

The postfix operator `?` applies to `Result<T, E>` and `Option<T>`.

For Result:
- `Ok(x)?` → evaluates to `x`
- `Error(e)?` → returns `Error(e)` from the current function

For Option:
- `Some(x)?` → evaluates to `x`
- `None?` → returns `None` from the current function

Example:

```axom
pub fn load(id: Int) -> Result<User, Err> {
  let raw = db.get(id)?
  Ok(parse(raw)?)
}

```

---

### 2.3 Pattern Matching

- `match` expressions must be exhaustive
- Non-exhaustive matches are compile-time errors
- Current implementation supports literals, relational patterns, `_`, identifiers, tuples, and record patterns
- Guards are available for record/variant patterns

Relational patterns use the match expression as the implicit left operand:
- `<= 1` desugars to `x when x <= 1`
- `> limit` desugars to `x when x > limit`

```axom
let message = match count {
  0 -> "none"
  _ -> "some"
}
```

---

### 2.4 .NET Exception Interop

Axom does not expose try/catch in the core language.

Interop with .NET exceptions is explicit via runtime helpers:

```axom
let x = dotnet.try_call<Int>("System.Math", "Max", 3, 7)?
```

Current interop support is partial: `dotnet.call<T>` and `dotnet.try_call<T>` with a whitelist (currently `System.Math`).

---

## 3. Control Flow

### 3.1 No Traditional Control Structures

Axom intentionally omits `if`, `else`, `while`, `for`, and `loop`.

This is a deliberate design choice to enforce:
- Exhaustive handling via pattern matching
- Functional iteration patterns
- Predictable control flow

---

### 3.2 Branching via Pattern Matching

All conditional logic uses `match`:

```axom
let message = match flag {
  true -> "large"
  false -> "small"
}
```

Pattern matching must be exhaustive. The compiler rejects non-exhaustive matches.

---

### 3.3 Iteration via Recursion

Custom iteration uses tail-recursive functions:

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

---

### 3.4 Iteration via Standard Library

For collections, use iterator combinators:

```axom
each(items, fn(x: Int) { println x })

let doubled = map(items, fn(x: Int) => x * 2)

let sum = fold(items, 0, fn(acc: Int, x: Int) => acc + x)

let evens = filter(items, fn(x: Int) => x % 2 == 0)

let piped = items
  |> map(fn(x: Int) => x * 2)
  |> filter(fn(x: Int) => x > 2)

rand_seed(42)
print rand_float()
print rand_int(10)
sleep(20)
```

---

### 3.5 Why No Imperative Loops?

- **One obvious way**: recursion OR iterators, never both for the same problem
- **Composability**: iterator chains are easier to reason about
- **Optimization**: no arbitrary control flow simplifies analysis
- **`let mut` purpose**: local accumulators, not manual loop counters

---

## 4. Mutability

### 4.1 Immutability by Default

All bindings are immutable by default.

---

### 4.2 Local Mutability

```axom
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

## 5. Concurrency & Parallelism

### 5.1 Effects and Suspension

A function is suspensive if it:
- calls another suspensive function
- performs I/O via the runtime

Backend mapping:
- Non-suspensive → sync .NET methods
- Suspensive → ValueTask<T>

---

### 5.2 Implicit Await

Sequential calls to suspensive functions implicitly await.

---

### 5.3 Structured Concurrency

Primitives:
- `scope { }`
- `spawn { ... }`
- `task.join()`

Fire-and-forget is intentionally impossible.

---

### 5.4 Cancellation

- Cancellation is implicit and scoped
- Blocking operations are forbidden

---

### 5.5 CPU Parallelism

```axom
scope {
  let a = spawn { compute_a(data) }
  let b = spawn { compute_b(data) }
  let result = a.join() + b.join()
}
```

CPU parallelism uses `scope` + `spawn { ... }` + `task.join()`.

---

### 5.6 Message Passing (Partial)

Axom message passing uses typed channels.

Primitives:
- `channel<T>() -> (Sender<T>, Receiver<T>)`
- `channel<T>(capacity: Int)` for bounded buffers
- `tx.send(value)`
- `rx.recv() -> Result<T, String>`

Rules:
- `recv()` is blocking until a message arrives.
- `recv()` results must be handled explicitly via `?` or `match`.
- No explicit `close()` in user code.
- Channel lifetime is scoped; endpoints cannot escape owner scope.
- Protocol termination is explicit in message type (for example `Stop`).
- Current implementation supports `channel<T>()`, `send`, and blocking `recv` for task communication.
- Channel `recv` results are strict: they must be handled explicitly via `?` or `match`.
- Scope teardown closes owned channels; blocked `recv()` returns `Error("channel closed")`.
- Default channel capacity is `64`; custom bounded capacity is available with `channel<T>(N)`.
- `send` is blocking when the bounded buffer is full.
- Channel message ordering is FIFO.
- Fairness across producer/consumer tasks is best-effort (runtime scheduling dependent).

Known limitations (current implementation):
- Cancellation semantics are baseline-only (`cancelled` propagation); richer cancellation policies are not implemented yet.
- Parser support for `channel<T>()` is currently specialized rather than a general generic-call mechanism.
- Buffer policy is currently bounded FIFO only; advanced backpressure strategies are not implemented yet.

```axom
type Msg {
  Value(Int)
  Stop
}

fn sender_loop(tx: Sender<Msg>, n: Int) {
  match n == 10 {
    true -> tx.send(Stop)
    false -> {
      tx.send(Value(n))
      wait(1000)
      sender_loop(tx, n + 1)
    }
  }
}

fn worker_loop(rx: Receiver<Msg>) -> Result<Unit, String> {
  let msg = rx.recv()?
  match msg {
    Value(x) -> {
      println x
      worker_loop(rx)
    }
    Stop -> Ok(())
  }
}

scope {
  let (tx, rx) = channel<Msg>()
  let sender_task = spawn { sender_loop(tx, 0) }
  let worker_task = spawn { worker_loop(rx) }
  sender_task.join()
  match worker_task.join() {
    Ok(_) -> ()
    Error(e) -> print e
  }
}
```

---

## 6. Types & Data

- Primitive types: Int (64-bit), Float (64-bit), Bool, String
- Records (with `type` keyword)
- Sum types
- Generics (minimal, with `<T>` syntax)

---

### 6.1 Records

Records declare named fields and are constructed with record literals.

```axom
type User { name: String, age: Int }

let user = User { name: "Ada", age: 36 }
let updated = user with { age: 37 }
print user.name
```

Notes:
- Field order in literals is not significant.
- Missing, duplicate, or unknown fields are compile-time errors.
- Record update uses `target with { field: value }`.
- Spread literals are for copy construction (`User { ...user }`) and cannot override fields.
- Constructor-style calls `User(...)` are planned but not implemented.

### 6.2 Sum Types

Sum types declare a fixed set of variants, optionally carrying a payload.

```axom
type Result { Ok(Int) Error(String) }

let value = Ok(42)
print match value {
  Ok(x) -> x
  Error(_) -> 0
}
```

Notes:
- Variants may omit payloads (`Ready`).
- Non-exhaustive matches are compile-time errors.

## 7. Interoperability

- Direct .NET calls
- NuGet supported
- Standard .NET assemblies output

---

## 8. Philosophy Recap

- Errors are values
- Concurrency is structured
- Parallelism is explicit
- Mutation is controlled
- One obvious way

---

Status: Draft v0.4 (see `roadmap.md` for implementation status)
