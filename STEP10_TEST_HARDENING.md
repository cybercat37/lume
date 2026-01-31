# Step 10: Test Strategy Hardening (12 Sub-steps)

Goal: strengthen test coverage with golden files, snapshot diagnostics, and fuzzing foundations.

1) Golden output layout
   Define folder structure and naming for golden codegen outputs.
   DoD:
   - Golden files are stored in a deterministic location.
   - Naming scheme documented.

2) Golden test harness
   Add a helper for golden comparisons.
   DoD:
   - Tests read expected output from disk.
   - Diffs are reported clearly on mismatch.

3) Codegen golden coverage
   Add golden tests for representative programs.
   DoD:
   - Covers literals, variables, blocks, and builtins.

4) Diagnostics snapshot format
   Define snapshot format for diagnostics output.
   DoD:
   - Stable formatting and ordering.

5) Diagnostics snapshot harness
   Add helper to compare diagnostics snapshots.
   DoD:
   - Snapshots checked into repo.

6) Parser recovery snapshots
   Snapshot diagnostics for recovery scenarios.
   DoD:
   - Includes missing tokens, extra tokens, malformed expressions.

7) Binder/type error snapshots
   Snapshot semantic errors.
   DoD:
   - Includes undefined variables, immutability, type mismatches.

8) CLI diagnostics snapshots
   Snapshot CLI error output for user-facing messages.
   DoD:
   - `check` error output verified.

9) Fuzzing entry point
   Add a basic fuzzing harness entry point.
   DoD:
   - Can run parser on random input without crashing.

10) Fuzz corpus seeds
   Create a minimal corpus of inputs.
   DoD:
   - Corpus includes valid and invalid samples.

11) CI wiring (optional)
   Add a task for golden/snapshot validation.
   DoD:
   - Tests fail on snapshot drift.

12) Documentation
   Document how to update golden/snapshot files.
   DoD:
   - README or AGENTS updated.

## Status
- Sub-step 1: complete
- Sub-step 2: complete
- Sub-step 3: complete
- Sub-step 4: complete
- Sub-step 5: complete
- Sub-step 6: complete
- Sub-step 7: complete
- Sub-step 8: complete
- Sub-step 9: complete
- Sub-step 10: complete
- Sub-step 11: complete
- Sub-step 12: complete
