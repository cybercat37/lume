# DB Verify Output Contract (MVP)

This document defines the canonical CLI output contract for `axom db verify`.

## Scope

- Applies to current MVP flags: `--report`, `--compare`, `--snapshot`, `--plan`.
- Covers key/value lines emitted on standard output.
- Error diagnostics remain on standard error.

## Canonical `--report` lines

When `--report` is enabled and `--quiet` is not set, output includes exactly:

- `total_queries_validated=<n>`
- `average_duration_ms=0`

Ordering is fixed as listed above.

Notes:

- `<n>` is the number of SQL literals validated in the current run.
- `average_duration_ms` is a placeholder metric in MVP.

## Canonical `--compare` lines

When `--compare` is enabled and `--quiet` is not set, output uses these keys:

- `compare_status=ok`
- `compare_warning=snapshot_missing`
- `compare_warning=query_added count=<n>`
- `compare_warning=query_removed count=<n>`
- `compare_warning=plan_hash_changed count=<n>`

Verbose-only detail lines:

- `compare_added_query_id=<query_id>`
- `compare_removed_query_id=<query_id>`
- `compare_plan_hash_changed_query_id=<query_id>`

## Canonical `--plan` lines

When `--plan` is enabled and `--quiet` is not set:

- `plan query_id=<query_id>`
- `plan detail=<text>`

Verbose additional line:

- `plan hash=<sha256_lower_hex>`

## Canonical `--snapshot` line

When `--snapshot` is enabled and `--quiet` is not set:

- `snapshot_written=.axom/query-metrics.json`

## Quiet mode behavior

- `--quiet` suppresses all non-error output lines.
- Validation and comparison logic still execute in quiet mode.
- Errors still fail the command and are written to standard error.

## Composition and ordering

When multiple flags are combined, ordering follows execution flow:

1. `--report` lines
2. `--snapshot` line
3. `--compare` lines
4. verbose metadata (for example `db_verify_file=...`)

Plan lines are emitted during per-query validation when `--plan` is active.
