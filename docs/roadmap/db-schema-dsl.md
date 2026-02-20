# DB Schema DSL (Model-First) Draft

This draft defines a schema-first DSL that acts as the source of truth for DB structure.
Migrations are generated from schema diffs, and SQL validation can run against an ephemeral
database built from those migrations.

Goal: catch query/schema mistakes before runtime by shifting failure to `axom check`.

Observability reference: `docs/roadmap/query-observability-performance-instrumentation.md`.

## Design principles

- Keep Axom core language semantics unchanged.
- Put DB schema/migration logic in tooling + DSL (CLI lane), not in runtime primitives.
- Make schema diffs deterministic and reviewable.
- Treat destructive changes as explicit/manual by default.

## Source of truth

- Schema file: `db/schema.axom`
- Migration folder: `db/migrations/`
- Materialized snapshot: `db/.schema.snapshot.json`

`db/schema.axom` is canonical for model intent.
`db/migrations/` is canonical for historical changes applied over time.

## DSL surface (draft)

```axom
schema "main" {
  table users {
    id: Uuid @pk
    email: String @unique
    name: String
    created_at: Instant @default(now())

    index idx_users_email(email)
  }

  table posts {
    id: Uuid @pk
    user_id: Uuid @ref(users.id, onDelete: cascade)
    title: String
    body: String?
    published: Bool @default(false)

    index idx_posts_user(user_id)
  }
}
```

### Decorators/annotations (initial set)

- Column: `@pk`, `@unique`, `@default(...)`, `@ref(table.column, onDelete: ...)`, `@nullable`
- Table: `@rename(from: "old_table")`
- Column rename mapping: `@map(from: "old_column")`
- Index declaration: `index name(col1, col2, ...)`

## Type mapping baseline (PostgreSQL-first)

- `Int -> integer`
- `Float -> double precision`
- `Bool -> boolean`
- `String -> text`
- `Instant -> timestamptz`
- `Uuid -> uuid`

Future types (`Bytes`, decimal precision, jsonb-specific controls) are additive.

## Migration generation workflow

1. Update `db/schema.axom`.
2. Run `axom db diff --name <change_name>`.
3. CLI compares schema AST vs `db/.schema.snapshot.json`.
4. CLI generates `db/migrations/<seq>_<change_name>.axom` with `up` and `down`.
5. Run `axom db migrate` to apply pending migrations.
6. Snapshot is refreshed after successful apply.

Generated migration format (draft):

```axom
migration "004_add_posts" {
  up {
    sql "create table posts (...)"
    sql "create index idx_posts_user on posts(user_id)"
  }

  down {
    sql "drop table posts"
  }
}
```

## Safety rules for diff

Auto-generated safely:

- add table
- add nullable column
- add index
- add foreign key with explicit policy

Marked as manual review (or blocked unless `--allow-destructive`):

- drop table/column
- narrowing type changes
- nullable -> non-nullable without default/backfill
- ambiguous rename (unless `@rename` / `@map` present)

## Compile-time SQL validation target

Primary mode: live DB validation in `axom check`.

Planned flow:

1. start ephemeral Postgres
2. apply migrations from `db/migrations/`
3. validate each `sql"..."` query against real engine metadata
4. fail check on syntax/type/projection mismatch

Fallback mode (`--offline`) can use snapshot-only validation when DB tooling is unavailable.

Performance and plan output are opt-in during validation:

- `axom db check` MUST be silent about performance by default.
- Additional report/plan/snapshot behavior follows the canonical observability RFC.
- SQL text MUST remain hidden unless verbose output is requested.

## CLI contract (draft)

- `axom db status`
- `axom db diff --name <change_name>`
- `axom db migrate`
- `axom db rollback --steps <n>`
- `axom db verify` (checks checksum drift)
- `axom db check`
- `axom db check --report`
- `axom db check --plan`
- `axom db check --snapshot` (experimental)
- `axom db check --compare`
- `axom check --db-live` (compatibility path while `axom db check` stabilizes)

## Out-of-scope for first iteration

- Full ORM behavior
- Implicit destructive migrations
- Engine-agnostic SQL transpilation
- Zero-downtime online migration orchestration
