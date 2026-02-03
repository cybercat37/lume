# Pipeline Combinator Expressions (Proposal)

This proposal introduces compact, pipeline-friendly combinators as first-class
language expressions. The goal is to enable readable, efficient iteration
without adding traditional loops, while keeping syntax small and explicit.

## Motivation

Axom intentionally omits if/while/for. Iteration is expected to be expressed via
recursion or iterators. To make iteration ergonomic and readable, this proposal
adds a dedicated expression form for combinators that composes naturally with
the pipe operator.

## Core Idea

The pipeline operator (|>) remains a value pipe:

```
value |> f  ==  f(value)
```

Combinators are introduced as first-class expression forms that can appear on
the right-hand side of |>. This keeps the pipeline readable and avoids special
case syntax for collections.

## Syntax (Proposed)

### Cascading pipeline

```axom
let sum = range(1, n)
  |> filter { x -> x % 2 == 0 }
  |> map { x -> x * 2 }
  |> fold 0 { acc, x -> acc + x }
```

### Single-stage examples

```axom
let doubled = items |> map { x -> x * 2 }
let evens = items |> filter { x -> x % 2 == 0 }
let sum = items |> fold 0 { acc, x -> acc + x }
let _ = items |> each { x -> println x }
```

## Grammar Sketch

```
PipeExpr        := Expr ("|>" PipeTarget)*
PipeTarget      := Expr | PipeCombinator
PipeCombinator  := MapExpr | FilterExpr | EachExpr | FoldExpr

MapExpr         := "map" LambdaExpr
FilterExpr      := "filter" LambdaExpr
EachExpr        := "each" LambdaExpr
FoldExpr        := "fold" Expr LambdaExpr

LambdaExpr      := "{" ParamList "->" Expr "}"
ParamList       := Identifier ("," Identifier)?
```

Notes:
- The compact lambda uses braces with an arrow form: `{ x -> expr }`.
- `fold` takes an init expression followed by a lambda with two parameters.

## Typing Rules

Assume `Iterable<T>` as the conceptual collection type (exact runtime type TBD).

- `map`:
  - Input: `Iterable<T>`
  - Lambda: `fn(T) -> U`
  - Output: `Iterable<U>`

- `filter`:
  - Input: `Iterable<T>`
  - Lambda: `fn(T) -> Bool`
  - Output: `Iterable<T>`

- `each`:
  - Input: `Iterable<T>`
  - Lambda: `fn(T) -> Unit`
  - Output: `Unit`

- `fold`:
  - Input: `Iterable<T>`
  - Init: `A`
  - Lambda: `fn(A, T) -> A`
  - Output: `A`

Diagnostics:
- Non-iterable left-hand value.
- Lambda arity mismatch (expected 1 for map/filter/each, 2 for fold).
- Lambda return type mismatch for filter/fold.

## Lowering Strategy

Lowering should preserve semantics and enable tail-call optimization.

### Conceptual rewrite

```
items |> map { x -> f(x) }
```

lowers to a compiler-generated function equivalent to:

```axom
fn __map(items) {
  // build result list with tail recursion or loop
}
```

### Preferred implementation

- Interpreter: execute via loop-based evaluator for combinators.
- Codegen: generate C# loops directly (no recursion in emitted code).
- Tail-call optimization applies when lowering to recursive form.

## Design Constraints

- No new collection syntax is implied by this proposal.
- Pipe remains a generic value pipe; combinators are recognized as intrinsic
  expression forms for better diagnostics and codegen.
- Lambdas use the compact `{ ... -> ... }` syntax only in combinator context
  (to avoid ambiguity with block statements).

## Non-Goals

- No implicit parallelism.
- No iterator protocol exposed yet.
- No extension methods or user-defined combinators in this phase.

## Open Questions

1) Should compact lambdas be allowed outside combinators?
2) Should map/filter return lists or lazy iterables?
3) How should range() integrate with Iterable types in the binder?
