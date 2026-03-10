# SQL/DB Ticket Backlog (1 PR = 1 Task)

Questo backlog traduce `docs/roadmap/sql-db-completion-roadmap.md` in ticket
piccoli e consecutivi. Ogni ticket e' pensato per una singola PR.

Legenda priorita': `P0` alta, `P1` media, `P2` bassa.

## R1 - Stabilizzazione `db verify`

### SQLDB-001 (`P0`) - Definire contratto output `db verify`
- Task: formalizzare righe output per `--report`, `--compare`, `--plan`, `--snapshot`.
- DoD: documento contratto in docs + test CLI che assertano le chiavi output canoniche.

### SQLDB-002 (`P0`) - Snapshot deterministico completo
- Task: stabilizzare ordine, deduplica e serializzazione del file `.axom/query-metrics.json`.
- DoD: due run identici producono snapshot byte-stable su fixture uguale.

### SQLDB-003 (`P0`) - Errori migrations/seeds con contesto file
- Task: unificare error messages includendo `scriptKind`, path relativo script, causa.
- DoD: test su script invalido per migration e seed con messaggio assertabile.

### SQLDB-004 (`P1`) - Copertura report edge cases
- Task: coprire zero query, query duplicate, quiet mode, report+compare combinati.
- DoD: suite `CliDbVerifyCommandTests` verde con i nuovi casi.

### SQLDB-005 (`P1`) - Plan output parity sqlite/postgres
- Task: rendere coerente il formato output `plan query_id` / `plan detail` tra provider.
- DoD: test provider-specific e golden output stabile.

## R2 - Typed projection `{Record}`

### SQLDB-006 (`P0`) - Resolver auto-mapping base colonne->campi
- Task: mapping implicito case-sensitive nominale senza `AXOM_DB_RECORD_PROJECTIONS`.
- DoD: query `{Record}` nominale passa senza config extra su sqlite.

### SQLDB-007 (`P0`) - Diagnostica campi mancanti/extra
- Task: errori dedicati per mismatch tra schema risultato e record target.
- DoD: codici diagnostici stabili + test success/failure per missing/extra fields.

### SQLDB-008 (`P1`) - Nullability checks projection
- Task: validare conversione colonna nullable -> campo non-nullable.
- DoD: mismatch nullable produce errore chiaro con nome colonna/campo.

### SQLDB-009 (`P1`) - Fallback esplicito a mapping env
- Task: mantenere `AXOM_DB_RECORD_PROJECTIONS` come override/fallback documentato.
- DoD: precedenza mapping definita e coperta da test.

## R3 - Verifica SQL piu' build-time

### SQLDB-010 (`P0`) - Validazione placeholder mancanti
- Task: segnalare placeholder `{param}` senza valore disponibile nel seed/binding.
- DoD: `db verify` fallisce con diagnostica mirata al placeholder.

### SQLDB-011 (`P0`) - Validazione placeholder inutilizzati
- Task: warning per parametri forniti ma non usati nella query.
- DoD: warning emesso in output e testato su fixture dedicata.

### SQLDB-012 (`P1`) - Shape checks `.one/.all/.exec`
- Task: verificare congruenza minima tra metodo invocato e natura statement.
- DoD: mismatch evidenti (es. `.exec` su `select`) segnalati con warning/error policy definita.

### SQLDB-013 (`P1`) - Reporting "validation findings"
- Task: aggiungere sezione findings in `--report` con conteggi error/warning.
- DoD: output include contatori stabili e testati.

## R4 - Transazioni v2

### SQLDB-014 (`P0`) - Specifica nested transaction policy
- Task: decidere policy ufficiale (savepoint vs reject configurabile) e documentarla.
- DoD: spec aggiornata + test che riflettono policy scelta.

### SQLDB-015 (`P0`) - Implementazione savepoint (se policy=savepoint)
- Task: supportare nested transaction via savepoint in runtime verify/execution path.
- DoD: nested commit/rollback deterministici con test edge-case.

### SQLDB-016 (`P1`) - Parity interpreter/codegen sulle transazioni
- Task: allineare comportamento transaction tra interpreter e C# emesso.
- DoD: stessi esiti su fixture duplicate in entrambe le modalita'.

## R5 - Osservabilita'/performance

### SQLDB-017 (`P1`) - Slow query report consolidato
- Task: output dedicato per query sopra `AXOM_DB_SLOW_MS` con query_id e durata.
- DoD: report slow query stabile e testabile con fixture controllata.

### SQLDB-018 (`P1`) - Plan hash drift details
- Task: migliorare compare con dettagli su drift `plan_hash` per query.
- DoD: warning include query_id e hash old/new in verbose mode.

## R6 - Provider parity

### SQLDB-019 (`P0`) - Matrix test sqlite/postgres per `db verify`
- Task: eseguire subset comune di test su entrambi i provider in CI.
- DoD: job CI dedicato verde e ripetibile.

### SQLDB-020 (`P1`) - Compatibility matrix documentata
- Task: tabella feature SQL/DB supportate per provider con limiti noti.
- DoD: documento in `docs/roadmap/` linkato da README/roadmap.

### SQLDB-021 (`P1`) - Normalizzazione error mapping provider
- Task: uniformare categorie errore runtime/verify tra sqlite e postgres.
- DoD: stessa categoria diagnostica per classi equivalenti di failure.

## Ordine consigliato di esecuzione

1. SQLDB-001
2. SQLDB-002
3. SQLDB-003
4. SQLDB-004
5. SQLDB-006
6. SQLDB-007
7. SQLDB-010
8. SQLDB-011
9. SQLDB-014
10. SQLDB-019

Gli altri ticket possono procedere in parallelo a blocchi, mantenendo il vincolo
"1 PR = 1 task".
