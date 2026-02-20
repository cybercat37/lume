# RFC: Query Observability & Performance Instrumentation

Status: Draft

Normative language: the key words MUST, MUST NOT, SHOULD, and MAY are to be interpreted as described in RFC 2119.

## Goals

- Observability MUST be strictly opt-in.
- Default behavior MUST be silent.
- Observability MUST NOT modify SQL or execution semantics.
- SQL rewriting is forbidden.
- Automatic optimization is forbidden.
- Implicit provider abstraction is forbidden.
- Behavior MUST remain deterministic across environments.

## Design Principles

- SQL remains the source of truth.
- Instrumentation MUST be passive.
- Production safety is the default posture.
- Performance gates MUST NOT be enabled unless explicitly requested.
- Hidden execution overhead MUST be avoided.

## Runtime Observability

### Default Runtime Behavior

By default, the runtime MUST:

- Disable query logging.
- Disable execution time reporting.
- Disable execution-plan collection.
- Disable parameter logging.
- Disable aggregated metrics output.
- Emit only execution errors.

### Environment Configuration

All runtime observability MUST be controlled via environment variables.

#### AXOM_DB_LOG

Controls per-query logging.

Values:

- `off` (default)
- `slow`
- `all`

`off`

- Query logging MUST be disabled.

`slow`

- MUST log only queries exceeding configured threshold.
- Output MUST include:
  - `query_id`
  - `duration_ms`
  - `rows_returned` / `rows_affected`
  - `error_flag`
- SQL text MUST NOT be printed unless explicitly enabled.

`all`

- MUST log every executed query.
- Output MUST include:
  - `query_id`
  - `duration_ms`
  - row metadata
  - `error_flag`
- SQL text and parameters MUST require additional flags.

#### AXOM_DB_LOG_SQL

Values:

- `0` (default)
- `1`

If enabled:

- Full SQL text MUST be included in logs.

If disabled:

- Logs MUST omit SQL text and include only `query_id` identifiers.

#### AXOM_DB_LOG_PARAMS

Values:

- `0` (default)
- `1`

If enabled:

- Parameters MAY be logged.
- Parameters MUST be masked by default:
  - Strings truncated
  - Known sensitive names masked
  - Large payloads summarized

#### AXOM_DB_PROFILE

Values:

- `0` (default)
- `1`

Enables runtime metric aggregation.

Metrics collected SHOULD include:

- total query count
- total execution time
- per-query frequency
- average duration
- percentile distribution (optional)
- error rate

No output MUST be printed unless combined with `AXOM_DB_LOG`
or explicitly requested via summary mode.

#### AXOM_DB_SLOW_MS

Defines slow query threshold.

Example:

`AXOM_DB_SLOW_MS=200`

If unset:

- Slow logging MUST remain disabled even if `AXOM_DB_LOG=slow`.

#### AXOM_DB_PLAN

Values:

- `0` (default)
- `1`

If enabled:

- Runtime SHOULD execute `EXPLAIN` before running the query.
- Plan output MUST be printed only when logging is enabled.
- Plan collection MUST NOT alter original query execution semantics.

#### AXOM_DB_PLAN_ANALYZE

Values:

- `0` (default)
- `1`

If enabled together with `AXOM_DB_PLAN`:

- Runtime SHOULD execute `EXPLAIN ANALYZE`.

`AXOM_DB_PLAN_ANALYZE` MUST NOT be enabled in production.

Axom does not enforce this automatically,
but documentation MUST clearly state the risk.

### Query Fingerprinting

Each executed SQL statement SHOULD be normalized and hashed.

Normalization includes:

- Removing literal values
- Collapsing whitespace
- Standardizing parameter placeholders

Example:

- `select * from users where id = 42`
- `select * from users where id = 7`

Both produce the same `query_id`.

`query_id` SHOULD be used for:

- Aggregated metrics
- Slow log deduplication
- Regression comparison (optional)

## Build-Time Observability

### Default

`axom db check` MUST perform:

- Migration application on ephemeral DB
- Query prepare/describe validation
- Type mapping validation

`axom db check` MUST produce no performance output by default.

`axom db check --report` MUST output aggregated metrics:

- total queries validated
- top N slowest queries
- average duration
- optional percentile data

SQL text MUST NOT be included unless `--verbose` is set.

`axom db check --plan` MAY include execution plan output during validation.

Plan output MUST be explicitly requested.

### Optional Query Metrics Snapshot (Experimental)

If enabled via:

`axom db check --snapshot`

Axom SHOULD generate:

`.axom/query-metrics.json`

Contains:

- `query_id`
- `average_duration`
- `plan_hash` (optional)
- `execution_count`

Future builds MAY compare against snapshot:

`axom db check --compare`

Comparison behavior SHOULD be:

- Warn on significant duration regression
- Warn on plan hash changes
- Never fail build unless explicitly configured

## Output Policy

Observability output MUST follow progressive disclosure:

- `query_id` + duration
- SQL text (only if requested)
- parameters (only if requested)
- execution plan (only if requested)

## Safety Guarantees

Observability MUST NOT:

- Rewrite SQL
- Inject hints
- Retry queries automatically
- Create indexes
- Modify schema
- Modify transaction boundaries
- Change isolation levels

Instrumentation is read-only.

## Provider Scope

Axom:

- MUST execute provider-native `EXPLAIN`.
- MUST NOT normalize plan semantics across providers.
- MUST NOT promise cross-database cost comparability.

## Non-Goals

Axom is not:

- An APM system
- A distributed tracing system
- A query optimizer
- An auto-indexing tool
- A performance tuning engine

Observability is informational and diagnostic only.

## Philosophy Alignment

This layer preserves:

- SQL as the source of truth.
- Explicit schema evolution.
- Deterministic execution.
- Zero hidden behavior.

Observability exists to increase visibility, never to introduce magic.
