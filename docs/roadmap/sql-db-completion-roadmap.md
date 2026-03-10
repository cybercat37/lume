# SQL/DB Completion Roadmap

Stato di partenza: runtime DB e `db verify` sono usabili in produzione alpha,
ma il track SQL/DB non e' ancora completo rispetto alla visione M17/M18.

Obiettivo: chiudere i gap su affidabilita', DX e parity provider senza
introdurre nuove semantiche core del linguaggio.

## Principi

- Core invariato: niente espansione semantica del linguaggio oltre sugar/librerie/tooling.
- Diagnostica prima di tutto: errori chiari, deterministici, orientati all'azione.
- Provider parity: `sqlite` e `postgres` devono avere comportamento coerente nei casi coperti.
- Incrementale: ogni fase deve essere rilasciabile in modo indipendente.

## Fase R1 - Stabilizzazione `db verify` MVP

### Scope

- Uniformare i messaggi di errore (migrations, seeds, query validation).
- Rendere stabile l'output di `--report`, `--snapshot`, `--compare`, `--plan`.
- Consolidare l'ordine applicazione script (`db/migrations/*.sql`, `db/seeds/*.sql`).

### Deliverable

- Contratto output CLI documentato.
- Test snapshot/golden dedicati ai flag di reporting.
- Diagnostiche con contesto minimo: file SQL, tipo script, causa errore.

### Definition of Done

- Flussi `axom db verify` verdi in CI su fixture representative.
- Nessuna regressione nel comportamento attuale di migrations/seeds.
- Output stabile tra esecuzioni identiche.

## Fase R2 - Typed projection `{Record}` v1 completa

### Scope

- Ridurre/eliminare la dipendenza da `AXOM_DB_RECORD_PROJECTIONS` nei casi standard.
- Mapping automatico `colonna -> campo` con diagnostica su mismatch tipo/nullability.
- Gestione robusta dei casi: campi mancanti, colonne extra, alias non risolti.

### Deliverable

- Resolver projection con fallback esplicito.
- Errori comprensibili per mismatch schema-record.
- Esempi aggiornati per projection implicita e casi con mapping esplicito.

### Definition of Done

- `{Record}` funziona out-of-the-box sui casi nominali.
- Suite test copre success/failure path principali.
- Documentazione SQL aggiornata con regole chiare di mapping.

## Fase R3 - Verifica SQL piu' build-time

### Scope

- Aggiungere controlli statici su parametri (`{param}` mancanti/inutilizzati).
- Validare shape attesa su `.one/.all/.exec` dove possibile.
- Segnalare query fragili con warning non bloccanti.

### Deliverable

- Pass di validazione dedicato nel flusso `db verify`.
- Classificazione diagnostiche: error/warning con codici stabili.
- Miglioramento report con sezione "validation findings".

### Definition of Done

- Riduzione errori scoperti solo a runtime su casi coperti.
- Diagnostica riproducibile e assertabile nei test.
- Nessun impatto negativo sui tempi di verify oltre budget concordato.

## Fase R4 - Transazioni v2

### Scope

- Introdurre policy esplicita per nested transaction:
  - supporto savepoint (preferito), oppure
  - fallback con reject configurabile/documentato.
- Mantenere rollback automatico su early return ed error path.

### Deliverable

- Semantica transazionale documentata con esempi minimi.
- Parity interpreter/codegen sui casi nested.
- Test edge-case su rollback/commit annidati.

### Definition of Done

- Comportamento nested deterministico e coperto da test.
- Nessuna regressione sui flussi transaction gia' supportati.

## Fase R5 - Osservabilita' e performance query

### Scope

- Consolidare metriche query (durata, slow threshold, fingerprint).
- Migliorare supporto plan/explain opt-in per provider.
- Rendere output osservabilita' consistente e machine-friendly.

### Deliverable

- Profilazione query attivabile via env/flag senza rumore default.
- Report slow queries e drift plan-hash dove disponibile.
- Guida operativa per troubleshooting performance.

### Definition of Done

- Team puo' identificare query lente e regressioni con strumenti built-in.
- Nessun overhead significativo quando osservabilita' e' disattivata.

## Fase R6 - Provider parity hardening (`sqlite`/`postgres`)

### Scope

- Allineare differenze note su binding, tipi, error mapping, explain behavior.
- Espandere test matrix cross-provider.
- Definire chiaramente i limiti di compatibilita' supportati.

### Deliverable

- Test suite SQL/DB eseguibile su entrambi i provider.
- Documento compatibilita' provider (feature support matrix).
- Runbook per setup locale e CI postgres.

### Definition of Done

- Scenario principali passano su `sqlite` e `postgres`.
- Divergenze residue tracciate con issue esplicite e workaround documentati.

## Ordine consigliato

1. R1 Stabilizzazione `db verify`
2. R2 Projection `{Record}`
3. R3 Verifica build-time
4. R4 Transazioni v2
5. R5 Osservabilita'/performance
6. R6 Provider parity hardening

## KPI di avanzamento

- Tasso regressioni SQL/DB in CI (target in discesa costante).
- Copertura test su path critici (`verify`, projection, transaction).
- Tempo medio `axom db verify` su fixture standard.
- Numero errori runtime evitabili intercettati in verify.

## Note operative

- Evitare scope creep: niente nuove primitive core per chiudere questa roadmap.
- Ogni fase deve includere: test, docs, esempi, checklist di compatibilita'.
- Aggiornare `roadmap.md` e `README.md` al termine di ogni fase completata.
