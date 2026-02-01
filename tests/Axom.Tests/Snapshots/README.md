# Snapshot Files

This folder stores diagnostics snapshots for parser, binder, and CLI output.

## Naming
- `<TestName>.snapshot.txt` stores the expected diagnostics output.

## Conventions
- One diagnostic per line.
- Stable ordering to avoid drift.

## Format
Each line uses the standard diagnostic string format:

```
<file>(<line>,<column>): <Severity>: <Message>
```

Example:

```
test.axom(1,7): Error: Undefined variable 'x'.
```
