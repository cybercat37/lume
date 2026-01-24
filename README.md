# Lume

A modern, Gleam-inspired programming language with native .NET interoperability.

## Overview

Lume is a minimal, opinionated language for .NET focused on simplicity, explicit error handling, and structured concurrency, while remaining fully interoperable with existing C# code.

## Core Principles

- **One obvious way to do things** — reduce cognitive load by providing clear, unambiguous patterns
- **Explicit error handling** — errors are values, not exceptions
- **Structured concurrency by default** — safe, predictable concurrent code
- **Immutability by default** — prevent accidental mutations
- **Full .NET interoperability** — seamless integration with existing .NET ecosystems
- **Small, understandable language surface** — easy to learn and maintain

## Status

**Early Draft** — This project is in its initial design phase. The language specification is being developed and refined.

## Documentation

The complete language specification is available in [`docs/spec.md`](docs/spec.md).

## Roadmap

The implementation roadmap includes:

1. **Lexer & Parser** — Tokenize and parse Lume source code
2. **AST & Type System** — Build abstract syntax tree with type checking
3. **Code Generation** — Emit .NET IL or C# code
4. **Runtime Library** — Core types (`Result`, `Option`, concurrency primitives)
5. **Tooling** — Language server, formatter, and build tools

## Contributing

See [`CONTRIBUTING.md`](CONTRIBUTING.md) for guidelines on how to contribute to Lume.

## License

Licensed under the Apache License 2.0. See [`LICENSE`](LICENSE) for details.
