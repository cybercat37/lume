# Step 9: CLI UX Expansion (12 Sub-steps)

Goal: define and implement a consistent CLI surface for check/build/run with clear output and exit codes.

1) CLI command spec
   Define commands, usage strings, exit codes, and output expectations.
   DoD:
   - `check`, `build`, `run` commands defined.
   - Usage strings documented.
   - Exit codes: 0 success, 1 failure.

2) `check` command
   Validate source without generating output files or running code.
   DoD:
   - Diagnostics written to stderr.
   - No `out/Program.cs` generated.
   - Exit 0 on success, 1 on diagnostics.

3) `build` command
   Generate `out/Program.cs` on success.
   DoD:
   - Output written to `out/Program.cs`.
   - Errors reported on stderr.
   - Exit 0 on success, 1 on diagnostics.

4) `run` command
   Build and execute generated output.
   DoD:
   - Build occurs before run.
   - Execution errors reported to stderr.
   - Exit code is propagated from run.

5) Path handling
   Support relative and absolute input paths.
   DoD:
   - Validates existence before compile.

6) Diagnostics formatting
   Ensure consistent error output format.
   DoD:
   - One line per diagnostic.
   - Includes file, line, column, severity, message.

7) Optional `--out`
   Allow overriding output directory for `build` and `run`.
   DoD:
   - Generated file path is `--out/Program.cs`.
   - Default remains `out/Program.cs`.

8) Optional verbosity flags
   Add `--quiet` and `--verbose` for user-facing messages.
   DoD:
   - `--quiet` suppresses non-error output.
   - `--verbose` includes extra context.

9) `--version` and `--help`
   Standard CLI metadata.
   DoD:
   - `--help` prints usage and commands.
   - `--version` prints version string.

10) Exit codes
   Verify CLI exit codes for all commands.
   DoD:
   - 0 for success, 1 for failures.

11) CLI tests
   Add tests for success and failure paths.
   DoD:
   - `check`, `build`, `run` have tests.

12) Documentation
   Update README/AGENTS with CLI usage.
   DoD:
   - Usage examples include `check`.

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
