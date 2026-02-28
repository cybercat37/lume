# RFC: Axom SQL Module (SQL-first, Typed Mapping, Build-time Verification)

## Status

Draft (partially implemented)

Current implementation snapshot:

* `sql"""..."""` literals are supported.
* `.one()`, `.all()`, `.exec()` sugar is wired to DB builtins.
* `{param}` runtime binding is implemented.
* `{Record}` runtime projection is implemented via explicit projection map (`AXOM_DB_RECORD_PROJECTIONS`).
* `axom db verify` / `axom db check` validates queries on an ephemeral SQLite database and applies `db/migrations/*.sql` next to the input file.

Still planned to fully match this RFC:

* compile-time type validation for `{param}` and `{Record}`.
* richer provider coverage and parity (PostgreSQL-first target).
* explicit `transaction {}` language/runtime lane.

Related RFCs:

* `docs/roadmap/query-observability-performance-instrumentation.md` (query observability and performance instrumentation)
* `docs/roadmap/http-db-plan.md` (delivery milestones M17-M18)

## Goals

* SQL è la **source of truth**.
* Query e migrazioni sono scritte in **SQL puro**.
* Axom aggiunge solo:

  * **param binding tipizzato** (`{param}`)
  * **row → record mapping** (`{Record}`)
  * **API minime** (`one/all/exec/transaction`)
  * **validazione a build-time** contro DB effimero.
* Niente DSL, niente ORM.

## Non-goals

* Query builder fluente.
* ORM, change tracking, lazy/eager loading automatico.
* Auto-mapping 1:N da join in liste annidate.
* “Magic schema generation”: Axom non genera schema.

---

## Repository Layout

```
db/
  migrations/
    0001_init.sql
    0002_add_users.sql
  seeds/
    0001_base.sql
    0002_dev.sql
  verify.axom        (opzionale: config per verify)
src/
  ...
```

### Conventions

* `db/migrations/*.sql` applicate in ordine lessicografico.
* `db/seeds/*.sql` applicabili separatamente (dev/test).
* Le migrazioni devono essere deterministiche e applicabili su DB effimero.

---

## Language Surface (minimal)

### SQL literal

```axom
sql"""
  -- SQL puro
"""
```

Dentro `sql""" ... """` Axom supporta due sole estensioni:

1. **Param binding**: `{param}`
2. **Record mapping**: `{Record}`

Nient’altro.

---

## Param Binding

### Syntax

* `{name}` dentro SQL indica un parametro bindato (non interpolato).
* Il binding è sempre parametrizzato (no concatenazione).

### Typing

* Il tipo del parametro è quello della variabile Axom (`Int`, `String`, `Decimal`, `DateTime`, `Bool`, `Bytes`, `Option<T>`, ecc.).
* `Option<T>`:

  * `Some(x)` → parametro valorizzato
  * `None` → `NULL`

Esempio (filtri opzionali in SQL puro, senza DSL):

```axom
sql"""
  select {User}
  from users
  where ({name} is null or name ilike {name})
    and ({minAge} is null or age >= {minAge})
""".all()
```

---

## Record Mapping

### Mapping “diretto”

```axom
sql"""
  select {User}
  from users
  where id = {id}
""".one()
```

Regole:

* `{User}` in `select` richiede che le colonne selezionate corrispondano ai campi del record per nome e tipo (o tramite alias standard del driver).
* Il mapping è **row → record** (1:1).

### Mapping “esplicito” (alias/expr)

Per query con join, aggregazioni, alias, espressioni, è supportata la forma con inizializzatore:

```axom
record UserSummary {
  userId: Int
  email: String
  totalSpent: Decimal
}

sql"""
  select {
    UserSummary
      userId: u.id,
      email: u.email,
      totalSpent: coalesce(sum(o.total_amount), 0)
  }
  from users u
  left join orders o on o.user_id = u.id
  group by u.id, u.email
""".all()
```

Regole:

* Ogni campo del record deve essere assegnato esattamente una volta.
* Tipi coerenti (es. `sum(...)` → `Decimal`).
* Il blocco di mapping non è una DSL: è solo un **mapping dichiarativo**.

---

## Query Execution API

Ogni `sql""" ... """` produce un oggetto query eseguibile con queste API minime:

* `.one() -> Option<T>`
* `.all() -> List<T>`
* `.exec() -> ExecResult`

Dove `T` è:

* `Record` (quando c’è `{Record}` in select)
* oppure un record inline minimal (es. `returning { id: Int }`)
* oppure `Unit` per query senza result-set (tipicamente `.exec()`)

Esempi:

```axom
let user = sql"""
  select {User}
  from users
  where id = {id}
""".one()
```

```axom
let rows = sql"""
  update users
  set name = {name}
  where id = {id}
""".exec()
```

```axom
let deleted = sql"""
  delete from users
  where id = {id}
  returning { id: Int }
""".one()
```

---

## Early Exit (`?`) and HTTP Policy

In funzioni “normali”:

* `Option<T>?` propaga `None` (early return)
* `Result<T,E>?` propaga `Err(e)`

In handler HTTP (`-> HttpResponse`), `?` applica una policy predefinita (documentata altrove), tipicamente:

* `None` → 404
* `ValidationError` → 400
* `AuthError` → 401/403
* altri errori → 500

Esempio:

```axom
let user = sql"""
  select {User}
  from users
  where id = {id}
""".one()?   // in HTTP: None -> 404
```

---

## Transactions

### Syntax

```axom
transaction {
  ...
}
```

Semantica:

* Tutte le query dentro il blocco condividono la stessa transazione.
* Se il blocco termina con successo → commit.
* Se il blocco termina con early-exit (`?`) o errore → rollback.

Esempio:

```axom
let user = transaction {
  sql"""
    insert into users(name)
    values ({name})
    returning {User}
  """.one()?
}
```

---

## 1:N Relationships (deliberate non-feature)

Axom **non** supporta mapping automatico `JOIN -> nested lists`.

Soluzioni supportate:

1. **Due query** esplicite (consigliata)
2. **JSON aggregation** DB-specific (esplicito, opt-in)

Esempio (due query):

```axom
let user = sql"select {User} from users where id={id}".one()?
let orders = sql"select {Order} from orders where user_id={id}".all()?
Ok(UserWithOrders { user, orders })
```

---

## Build-time Verification

### Command

`axom db verify`

### Behavior (normativo)

1. Crea un DB effimero (driver-specific).
2. Applica `db/migrations/*` in ordine.
3. (Opzionale) Applica `db/seeds/*` se richiesto.
4. Scansiona il progetto per `sql""" ... """`.
5. Per ogni query:

   * valida sintassi SQL
   * valida param binding (`{param}` esiste e tipo compatibile)
   * valida record mapping (`{Record}` compatibile con result-set)
6. Se una query fallisce → **build fallisce** con errore puntuale (file/linea + dettaglio).

Note:

* La validazione è “best effort” driver-specific, ma deve essere affidabile per Postgres e SQLite (target iniziali).

---

## Error Model

Il modulo SQL deve esporre errori *stabili* e piccoli, ad es.:

* `DbError.Connection`
* `DbError.Timeout`
* `DbError.Constraint`
* `DbError.Serialization`
* `DbError.Unknown`

Il mapping HTTP di questi errori è demandato alla policy HTTP.

---

## Examples

### Read-one (404 via `?`)

```axom
fn getUser(id: Int) -> HttpResponse {
  let user = sql"""
    select {User}
    from users
    where id = {id}
  """.one()?
  ok(user)
}
```

### Update returning (404 via `.one()?`)

```axom
let user = sql"""
  update users
  set name = {name}
  where id = {id}
  returning {User}
""".one()?
```

---

## Compatibility Targets

* Primary: PostgreSQL
* Secondary: SQLite (dev/test + embedded)

---

## Summary Principles (normative)

1. SQL is the source of truth.
2. Records define the contract.
3. Params are bound, typed, never concatenated.
4. `.one()?` keeps the happy path linear.
5. `transaction {}` is explicit.
6. Verification happens at build-time.
