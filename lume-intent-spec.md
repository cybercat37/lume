# Intent Annotations and Semantic Documentation

## Overview

Lume does not support traditional comments.

Instead, Lume introduces **intent annotations**: structured, machine-readable metadata that express developer intention and are treated as part of the program semantics.

Intent annotations are:
- parsed into the AST
- available during type checking and linting
- used to generate live documentation
- validated against inferred behavior

Their purpose is to **reduce the gap between intention, implementation, and documentation**.

---

## Design Principles

1. Intent is **explicit and structured**, not free-form comments.
2. Intent annotations **never affect runtime semantics**.
3. Intent annotations **may produce warnings** if inconsistent with inferred behavior.
4. Documentation is **derived from code**, not manually written.
5. Tooling should allow developers to **see what the compiler believes the code does**.

---

## Intent Annotations

### Syntax

Intent annotations use the `@intent("...")` form.

They may appear:
- on blocks
- on local bindings (`let`)
- on function declarations (future extension)

The argument **must be a string literal**.

---

## Block Intent Annotation

### Syntax

```lume
@intent("Description of the step")
{
  statements
}
```

### Semantics

- The annotation applies to the immediately following block.
- The block is treated as a **semantic step**.
- The compiler associates the intent with:
  - the statements inside the block
  - the inferred effects of those statements

### Intended Use

Block intents are used to describe **high-level steps** of a function:
- validation
- persistence
- integration
- orchestration

---

## Local Binding Intent Annotation (`let`)

### Syntax

```lume
let name @intent("Description") = expression
```

### Semantics

- The intent applies only to the evaluation of `expression`.
- The compiler associates the intent with:
  - the expression
  - its inferred effects
  - its resulting type

---

## Effect Inference

The compiler infers a set of **effects** for expressions and blocks.

Effects are inferred transitively from known operations, such as:
- database access
- network calls
- filesystem I/O
- time or randomness

The effect system is **pragmatic**, not formal.

---

## Intent Validation and Warnings

The compiler or linter may emit warnings when intent annotations are inconsistent with inferred behavior.

Example warning:

```
warning(LU1203):
Intent "Persist order in DB" does not match inferred effects.
Expected: db
Found: none
```

---

## Documentation Generation

Intent annotations are a primary input to documentation generation.

Generated documentation may include:
- function summaries
- ordered execution steps
- inferred effects
- exposed error types

Documentation is **derived**, not authored.

---

## Live Documentation

Tooling should support **live intent visualization**, allowing developers to see generated documentation while writing code.

If generated documentation does not match the developer’s intention, this is considered a **design signal**.

---

## Non-Goals

- Intent annotations do not replace tests.
- Intent annotations are not executable.
- Intent annotations are not general-purpose comments.

---

## Summary

Intent annotations make developer intention explicit, inspectable, and partially verifiable.

They allow Lume to treat documentation as a **reflection of code meaning**, not an external artifact.
