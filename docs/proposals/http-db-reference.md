# HTTP + DB Reference Spec (Vision)

Status: Reference document (vision and proposal backlog), not source of truth for
current implementation status. For delivery sequence and executable scope, use
`docs/roadmap/http-db-plan.md` and `roadmap.md`. For DB query observability
constraints, use `docs/roadmap/query-observability-performance-instrumentation.md`.

---

## Axom File-Based Routing Spec (MVP)

### 1) Root delle route

Una directory per servizio, es:

* `routes/` (root routing)

Tutti i file `*.ax` sotto `routes/` definiscono handler HTTP.

---

## 2) Mapping filesystem → path

### 2.1 Cartelle = segmenti

Ogni cartella aggiunge un segmento.

* `routes/users/index.ax` → `/users`
* `routes/users/posts.ax` → `/users/posts`

**Regola `index`**

* `index.ax` non aggiunge segmento: rappresenta la root della cartella.

---

### 2.2 Flat routing via underscore (opzionale, ma utile)

Se un file è direttamente sotto `routes/`, puoi usare `_` per creare segmenti:

* `routes/users_index.ax` → `/users`
* `routes/admin_users_list.ax` → `/admin/users/list`

**Regola**

* `_` separa segmenti del path.
* `index` come ultimo token è “virtuale” (non compare nel path).

> Nota: puoi anche decidere “o cartelle o flat” per evitare mix. Se li fai convivere, vedi collisioni.

---

## 3) Metodi HTTP dal nome file

Il metodo è determinato dal suffisso finale:

* `_get`, `_post`, `_put`, `_patch`, `_delete`, `_head`, `_options`

Esempi:

* `users_index_get.ax` → `GET /users`
* `users_index_post.ax` → `POST /users`

Se non c’è suffisso metodo: **default `GET`**.

---

## 4) Parametri nel path (match dinamico)

### 4.1 Sintassi file-based per parametri

Uso `__` (doppio underscore) per introdurre un parametro come segmento:

* `users__id_get.ax` → `/users/:id`
* `users__id_posts_get.ax` → `/users/:id/posts`

**Regola**

* `__name` genera `:name` come segmento dinamico.

---

### 4.2 Vincoli sui parametri (per evitare ambiguità)

Aggiungi un tipo/vincolo dopo il nome: `__id_int`, `__id_uuid`, `__slug_slug`.

* `users__id_int_get.ax` → `/users/:id<int>`
* `orders__id_uuid_get.ax` → `/orders/:id<uuid>`

**Vincoli builtin consigliati**

* `<int>` (solo cifre, opzionale segno se vuoi)
* `<uuid>` (formati UUID standard)
* `<slug>` (a-z0-9 e trattini, o quello che decidi)
* `<alpha>` / `<alnum>` (facoltativi)

Se manca vincolo: default `<string>` (matcha qualsiasi segmento non vuoto, escluso `/`).

---

## 5) Regole di match runtime (semplici)

Dato un request path:

1. split su `/` in segmenti
2. matcha solo route con stesso numero di segmenti
3. per ogni segmento:

   * statico: deve essere uguale
   * dinamico: deve passare il vincolo (parse/validate)
4. se match: bind dei parametri e chiama handler

---

## 6) Niente precedenze: conflitti = errori di compilazione

Durante build/compile, generi una “route table” e **fallisci** se due route sono ambigue.

### 6.1 Quando c’è conflitto

Due route A e B confliggono se:

* stesso metodo (o uno è ANY)
* stesso numero di segmenti
* per ogni posizione `i`:

  * statico vs statico: devono essere uguali, altrimenti non confliggono
  * statico vs dinamico: conflitto se il dominio del dinamico include quello statico
  * dinamico vs dinamico: conflitto se i loro domini si intersecano

Esempi:

* `GET /users/me` vs `GET /users/:id<string>` → **conflitto** (me ∈ string)
* `GET /users/me` vs `GET /users/:id<int>` → **OK** (me ∉ int)
* `GET /users/:a<int>` vs `GET /users/:b<int>` → **conflitto** (stesso dominio)

### 6.2 Messaggio di errore (consigliato)

Il compiler deve stampare:

* method + template di entrambe
* file sorgenti
* motivo (dominio che si sovrappone)

---

## 7) Collisions tra cartelle e flat (se li permetti insieme)

Se due file risolvono allo **stesso method+path template**, errore.
Esempio:

* `routes/users/index_get.ax` e `routes/users_index_get.ax` → entrambi `GET /users` → errore.

(Alternativa: vietare flat quando esiste una cartella omonima, ma l’errore è più pulito.)

---

## 8) Esempio completo

```
routes/
  health_get.ax                 -> GET /health
  users_index_get.ax            -> GET /users
  users_index_post.ax           -> POST /users
  users__id_int_get.ax          -> GET /users/:id<int>
  users_me_get.ax               -> GET /users/me   (OK: no conflitto con id<int>)
```


## Spec: Customer Docs “battery included” esposte via HTTP

### Scopo

Generare in fase di build una documentazione “client-ready” (HTML + asset + script) e servirla dal microservizio su una rotta configurabile (default `/docs`) protetta da autorizzazione.

---

## 1) Build artifacts

### 1.1 Spec interna (input del generatore)

Durante `axom build` il compilatore produce una spec macchina:

* `build/specs/service.spec.json`

Contiene almeno:

* `service`: `{ name, version, generatedAt }`
* `docs`: `{ title, baseUrls[], authHints }`
* `endpoints[]`: `{ method, pathTemplate, summary, description, params[], requestBody?, responses[], errors[], examples[] }`
* `models[]` (opzionale): schemi/tipi usati

### 1.2 Bundle statico (output generatore docs)

Durante `axom build` (o step successivo automatico) viene generato un bundle statico:

* `build/docs-bundle/`

  * `index.html`
  * `assets/` (css/js/fonts/logo)
  * `downloads/`

    * `api.sh`
    * `api.ps1`

**Requisiti**

* `index.html` deve essere self-contained in termini di link relativi (tutto sotto mount).
* Gli script devono essere scaricabili senza dipendenze esterne (solo `curl` e shell/pwsh).

### 1.3 Packaging

In fase di publish:

* modalità `embedded`: il bundle viene incorporato come resource nell’assembly (consigliato prod)
* modalità `filesystem`: il bundle viene copiato accanto all’eseguibile/container (consigliato dev)

---

## 2) Configurazione runtime

Nel config del servizio (es. `axom.toml`):

* `docs.enabled = true | false` (default `true` in dev, `false` in prod se non configurato)
* `docs.mount = "/docs"` (default)
* `docs.source = "embedded" | "filesystem"` (default `"embedded"`)
* `docs.fs_path = "./docs-bundle"` (usato se `filesystem`)
* `docs.auth.mode = "same_as_api" | "scope" | "role" | "basic"`
* `docs.auth.scope = "docs:read"` (se mode=`scope`)
* `docs.auth.role = "docs_reader"` (se mode=`role`)
* `docs.cache.assets_max_age_seconds = 604800` (default 7 giorni)

---

## 3) Routing ed esposizione

### 3.1 Mount

Il servizio registra un handler statico su:

* `GET {docs.mount}/*`

Comportamento:

* `GET /docs` → `302` verso `/docs/`
* `GET /docs/` → serve `index.html`
* `GET /docs/assets/<path>` → serve file asset
* `GET /docs/downloads/<file>` → serve file download

### 3.2 Tipi MIME e download

MIME consigliati:

* `.html` → `text/html; charset=utf-8`
* `.css` → `text/css; charset=utf-8`
* `.js` → `text/javascript; charset=utf-8`
* `.sh` / `.ps1` → `application/octet-stream`

Per `downloads/*` aggiungere:

* `Content-Disposition: attachment; filename="<file>"`

---

## 4) Autorizzazione

Tutte le route sotto `{docs.mount}` sono protette.

### 4.1 Mode: `same_as_api`

* applica lo stesso middleware/guard usato per le API.

### 4.2 Mode: `scope`

* richiede bearer token valido
* richiede scope `docs:read` (configurabile)

### 4.3 Mode: `role`

* richiede bearer token valido
* richiede role `docs_reader` (configurabile)

### 4.4 Mode: `basic` (opzionale)

* HTTP Basic Auth solo per docs
* sconsigliato come default; ammesso per richieste cliente

Risposte:

* `401` se manca/invalid token
* `403` se token valido ma senza permesso

---

## 5) Sicurezza

### 5.1 Path traversal

Il server **deve**:

* normalizzare il path richiesto
* rifiutare ogni path che contenga `..` o escape equivalenti
* servire solo file presenti nel bundle

### 5.2 No directory listing

* richieste a directory senza `index` → `404` (o redirect solo per mount root)

### 5.3 Header di sicurezza

Consigliati su `index.html`:

* `X-Content-Type-Options: nosniff`
* `Content-Security-Policy` (almeno `default-src 'self'`)
* `Referrer-Policy: no-referrer`

---

## 6) Caching

* `assets/*`:

  * `Cache-Control: public, max-age=<assets_max_age>`
  * `ETag` o hash nel filename (opzionale)
* `index.html`:

  * `Cache-Control: no-cache`
  * `ETag` consigliato

---

## 7) Endpoint di supporto (consigliato)

### 7.1 Versione docs

* `GET {docs.mount}/version`
  Risposta JSON:

```json
{ "service":"<name>", "version":"<semver>", "generatedAt":"<iso8601>" }
```

---

## 8) Contenuti della pagina (layout “cliente”)

`index.html` deve includere:

* Copertina: nome servizio, versione, data generazione
* Sezione “Ambienti”: base URL (dev/stage/prod) + variabili
* Sezione “Autenticazione”: header richiesti, esempi token
* Convenzioni: error model, pagination, rate limit (se presenti)
* Endpoint:

  * summary/descrizione
  * method + path
  * params + body schema
  * responses + esempi
  * tabella errori (codice, causa, azione)
  * snippet `curl`
* Download:

  * link a `/docs/downloads/api.sh`
  * link a `/docs/downloads/api.ps1`

Pagina print-friendly (CSS per stampa) in modo che il cliente possa esportare PDF dal browser.

---

## 9) Script generati

### 9.1 `api.sh`

Deve includere:

* `BASE_URL`, `TOKEN` (placeholder)
* funzioni per endpoint (nome deterministico)
* esempi di invocazione
* uso di `curl` con header auth e content-type


# Axom REST Auth Spec v0.1

## 1. Scopo

Definire un sistema “battery included” per:

* **Autenticazione** (chi sei)
* **Autorizzazione** (cosa puoi fare)
* dichiarato tramite **intent** sugli handler/route
* con risposte HTTP standard (401/403) e comportamento deterministico.

---

## 2. Intent supportati

### 2.1 Public

Rende una route pubblica (nessuna auth richiesta).

```axom
@public
```

### 2.2 Auth base

Richiede solo che l’utente sia autenticato.

```axom
@auth
```

### 2.3 Roles

Richiede che il principal contenga **tutti** i ruoli elencati (AND).

```axom
@auth(roles[canread, canwrite])
```

### 2.4 Scopes

Richiede che il principal contenga **tutti** gli scope elencati (AND).

```axom
@auth(scopes["users:read", "users:write"])
```

### 2.5 Claims (presenza)

Richiede che i claim elencati siano presenti.

```axom
@auth(claims[tenant, plan])
```

### 2.6 Composizione (AND implicito)

Se specificati più requisiti, sono in AND.

```axom
@auth(roles[admin], scopes["users:read"])
```

> Estensioni future (non parte della v0.1): `any(...)` (OR), claim con valore (`tenant="acme"`), policy named.

---

## 3. Livelli di applicazione e ereditarietà

Un intent può essere applicato a:

* file route (top-level)
* funzione handler
* group (se presente)

### 3.1 Precedenza (override)

1. intent su handler
2. intent su group
3. intent su file
4. default globale (config)

### 3.2 Regole

* `@public` **non è combinabile** con `@auth(...)` sullo stesso target (errore compile).
* Un handler senza intent eredita dal livello superiore.

---

## 4. Config runtime

Formato esemplificativo:

```toml
auth.require_by_default = true           # secure-by-default

auth.mode = "bearer"                     # bearer | apikey | none
auth.bearer.issuer = "https://issuer/"
auth.bearer.audience = "axom-api"
auth.bearer.jwks_url = "https://issuer/.well-known/jwks.json"
auth.bearer.clock_skew_seconds = 60

auth.apikey.header = "X-API-Key"
auth.apikey.lookup = "env"
auth.apikey.env_prefix = "APIKEY_"

auth.claims.roles = "roles"              # claim name per roles
auth.claims.scopes = "scope"             # claim name per scopes (o "scp")
auth.claims.subject = "sub"
auth.claims.tenant = "tenant"            # opzionale
```

---

## 5. Modello dati runtime

### 5.1 Principal

```axom
type Principal {
  userId: String
  tenantId: Option<String>
  roles: Set<String>
  scopes: Set<String>
  claims: Map<String, String>
  issuedAt: Instant
  expiresAt: Instant
}
```

### 5.2 HttpContext (minimo)

```axom
type HttpContext {
  principal: Option<Principal>
  requestId: String
  // ... request/response, trace, ecc.
}
```

---

## 6. Semantica di autorizzazione

Dato un `AuthRequirement` derivato dagli intent:

* `Public` → consenti sempre
* altrimenti, se `principal` assente → **401**
* `Authenticated` → consenti
* `Roles(R)` → consenti se `R ⊆ principal.roles` altrimenti **403**
* `Scopes(S)` → consenti se `S ⊆ principal.scopes` altrimenti **403**
* `Claims(C)` → consenti se `∀c∈C: c ∈ principal.claims` altrimenti **403**
* composizione (più requisiti) → AND

---

## 7. Pipeline request (ordine obbligatorio)

1. **Route match** (determina handler + metadata AuthRequirement)
2. **Authentication middleware**
3. **Authorization check**
4. **Handler**
5. **Error mapping** (business errors → HTTP)
6. **Observability** (log/trace con requestId e subject se presente)

---

## 8. Authentication middleware

### 8.1 Bypass per `@public`

Se requirement è `Public`, non eseguire auth.

### 8.2 Bearer JWT

* Legge `Authorization: Bearer <token>`
* Valida firma (JWKS), `exp`, `nbf`
* Verifica `iss` e `aud`
* Estrae claims:

  * `userId` da `sub` (configurabile)
  * `roles` da claim `roles` (string o array)
  * `scopes` da `scope`/`scp` (string space-separated o array)
  * `tenantId` da claim `tenant` (se configurato)
* Normalizza in `Principal`

### 8.3 API Key

* Legge header configurato (default `X-API-Key`)
* Lookup (v0.1: env/file)
* Se valida, produce Principal “service/client” (userId = keyId, roles/scopes opzionali)

---

## 9. Risposte standard

### 9.1 401 Unauthorized

Usare quando:

* header auth mancante
* token/key invalidi o scaduti
* principal non creato e route non public

Headers:

* Bearer: `WWW-Authenticate: Bearer`
* Basic (se mai introdotto): `WWW-Authenticate: Basic realm="..."`

Body JSON standard:

```json
{
  "error": "unauthorized",
  "message": "Missing or invalid credentials",
  "requestId": "..."
}
```

### 9.2 403 Forbidden

Usare quando:

* autenticazione OK, ma policy fallisce (ruolo/scope/claim mancante)

Body JSON standard:

```json
{
  "error": "forbidden",
  "message": "Missing role canread",
  "requestId": "..."
}
```

---

## 10. Secure-by-default

Se `auth.require_by_default = true`:

* una route **senza intent** viene trattata come `@auth` (Authenticated)

Opzione addizionale (consigliata):

```toml
auth.strict_intents = true
```

In modalità strict:

* se una route non dichiara `@public` o `@auth...` → **errore di compilazione**.

---

## 11. Compile-time checks (obbligatori)

Il compilatore deve:

1. parsare e normalizzare intent `@public`, `@auth(...)`
2. costruire `AuthRequirement` per ogni handler
3. generare errore se:

   * `@public` e `@auth(...)` coesistono sullo stesso target
   * liste vuote (`roles[]`, `scopes[]`, `claims[]`)
   * sintassi invalida
4. (opzionale ma raccomandato) validare ruoli/scope contro un registry:

   * ruolo/scope sconosciuto → errore compile (o warning in modalità non-strict)

---

## 12. Integrazione con generazione docs/spec

Ogni endpoint nella spec interna deve includere:

* `auth.type`: `public | authenticated | roles | scopes | claims`
* `auth.values`: lista (se applicabile)

Esempio:

```json
"auth": { "type": "roles", "values": ["canread"] }
```

## 1. Scopo

Definire un blocco **`security {}`** dichiarativo nel codice Axom per configurare i meccanismi di autenticazione/autorizzazione **visibili nel progetto**, con segreti e valori runtime referenziati tramite `config.*`, e utilizzo via intent `@auth(...)`.

Obiettivi:

* nessuna “magia” nascosta in TOML per capire come sono protette le API
* binding **compile-time** tra policy/handler e configurazioni security
* compatibile con generazione spec/docs e runtime microservizi

---

## 2. Sintassi generale

Un progetto può contenere **0..N** blocchi `security {}` (consigliato: 1 per servizio).
I blocchi vengono **uniti** in una singola “Security Registry”.

```axom
security {
  <provider-decl>*
}
```

`<provider-decl>` è una dichiarazione di provider, es:

* `apikey <name> { ... }`
* `bearer <name> { ... }`

`<name>` è un identificatore (case-sensitive) univoco per tipo provider.

---

## 3. Provider supportati (v0.1)

### 3.1 API Key Provider

```axom
security {
  apikey <name> {
    header = "<Header-Name>"             # default: "X-API-Key"
    from   = <secret-source>             # obbligatorio
    roles  = [<ident>*]                  # opzionale
    scopes = ["<scope>"*]                # opzionale
    tenant_claim = "<claim-name>"        # opzionale (solo se from produce JWT? v0.1: ignora)
  }
}
```

#### 3.1.1 Secret source (v0.1)

`from` può essere:

* `config.secrets.<path>`
  (es: `config.secrets.client_key`)
* `config.env.<NAME>`
  (es: `config.env.API_KEY_CLIENT1`)

Semantica:

* il valore risolto a runtime è una stringa segreta.
* in v0.1 è ammesso che sia **chiave in chiaro**; estensione futura: hash.

---

### 3.2 Bearer JWT Provider (OIDC)

```axom
security {
  bearer <name> {
    authority = "<https://issuer/>"      # obbligatorio
    audience  = "<audience>"             # obbligatorio
    algorithms = ["RS256"]               # opzionale, default: ["RS256"]
    clock_skew_seconds = 60              # opzionale, default: 60

    claims {
      subject = "<claim>"                # default: "sub"
      roles   = "<claim>"                # default: "roles"
      scopes  = "<claim>"                # default: "scope" (o "scp" se configurato)
      tenant  = "<claim>"                # opzionale
    }
  }
}
```

Semantica:

* runtime ottiene metadata OIDC da `authority` (well-known) e JWKS, gestendo rotazione chiavi tramite stack .NET.
* verifica: firma, exp/nbf, iss, aud, alg consentiti.

---

## 4. Uso tramite intent `@auth`

### 4.1 Forme ammesse

```axom
@public
@auth
@auth(roles[...])
@auth(scopes["..."])
@auth(claims[...])
```

Estensione del binding a provider:

```axom
@auth(apikey:<name>)
@auth(bearer:<name>)
```

Composizione:

```axom
@auth(apikey:client_key, roles[canread])
@auth(bearer:google, scopes["users:read"])
```

### 4.2 Semantica del provider binding

* Se `@auth(apikey:<name>)` è presente, l’endpoint accetta **solo** quel provider.
* Se `@auth(bearer:<name>)` è presente, l’endpoint accetta **solo** quel provider.
* Se `@auth` senza provider, vale il **default provider** (vedi §5.1) o “any configured provider” (modalità opzionale).

---

## 5. Default e modalità

### 5.1 Default provider (v0.1)

Il progetto può dichiarare un default provider nel blocco security:

```axom
security {
  default = apikey:client_key
  apikey client_key { ... }
}
```

Oppure:

```axom
security {
  default = bearer:google
  bearer google { ... }
}
```

Regole:

* se un handler usa `@auth` senza provider, usa `security.default`
* se `security.default` non è definito e `@auth` non specifica provider → errore compile (in strict), warning (in non-strict)

### 5.2 Secure-by-default

Opzione di progetto (in codice o config, a scelta tua; qui in codice):

```axom
security {
  require_by_default = true
  default = bearer:google
  ...
}
```

Regola:

* se `require_by_default = true`, un endpoint senza `@public` e senza `@auth...` viene trattato come `@auth` (che usa `default`).

Modalità `strict_intents` (consigliata):

* se true → endpoint senza `@public`/`@auth...` => errore compile.

---

## 6. Runtime contract

### 6.1 Principal

Come già definito:

```axom
type Principal {
  userId: String
  tenantId: Option<String>
  roles: Set<String>
  scopes: Set<String>
  claims: Map<String, String>
  issuedAt: Instant
  expiresAt: Instant
}
```

### 6.2 API Key auth runtime

Per `apikey <name>`:

* legge header `header`
* confronta con segreto risolto da `from`
* se match:

  * `Principal.userId = "<name>"` o un `clientId` derivato (v0.1: `<name>`)
  * `roles/scopes` aggiunti dal provider config (se presenti)
* se no: 401

Estensione futura (non v0.1): multiple keys, hashing, key id.

### 6.3 Bearer auth runtime

Per `bearer <name>`:

* valida token con OIDC metadata/jwks
* costruisce principal usando mapping claims

---

## 7. Errori HTTP standard

* 401 se manca/invalid credential per il provider richiesto
* 403 se credential valida ma fallisce `roles/scopes/claims` richiesti dall’intent

Body JSON standard:

```json
{ "error":"unauthorized|forbidden", "message":"...", "requestId":"..." }
```

---

## 8. Compile-time checks (obbligatori)

Il compilatore deve:

1. **Unicità**

* non possono esistere due provider dello stesso tipo con lo stesso `<name>`.

2. **Riferimenti**

* `@auth(apikey:x)` richiede che esista `apikey x { ... }`
* `@auth(bearer:y)` richiede che esista `bearer y { ... }`
* se non esiste → errore compile

3. **Obbligatorietà campi**

* `apikey`: `from` obbligatorio
* `bearer`: `authority` e `audience` obbligatori

4. **Validazione valori**

* header non vuoto
* authority deve essere URL https
* algorithms devono essere whitelist supportata

5. **Conflitti intent**

* `@public` non combinabile con `@auth(...)` sullo stesso target

6. **Default**

* se `@auth` senza provider e `security.default` mancante:

  * strict: errore compile
  * non-strict: warning + runtime error 500 (sconsigliato)

---

## 9. Integrazione con docs/spec generator

La spec interna per endpoint include:

* provider richiesto: `apikey:<name>` o `bearer:<name>`
* header richiesto (per apikey)
* authority/audience (senza segreti) per bearer
* ruoli/scope richiesti

Esempio:

```json
"auth": {
  "provider": "apikey:client_key",
  "header": "X-Client-Key",
  "requirements": { "roles":["canread"] }
}
```

---

## 10. Esempio completo

```axom
security {
  require_by_default = true
  strict_intents = true
  default = apikey:client_key

  apikey client_key {
    header = "X-Client-Key"
    from   = config.secrets.client_key
    roles  = [api_user]
  }

  bearer google {
    authority = "https://accounts.google.com"
    audience  = config.google.client_id
    algorithms = ["RS256"]
    claims {
      subject = "sub"
      scopes  = "scope"
    }
  }
}
```

Endpoint:

```axom
@auth(apikey:client_key, roles[api_user])
fn handler(ctx) => OkJson({ "now": Time.nowUtcIso8601() })
```


## Spec: Typed SQL Interpolation con Proiezioni di Record

### 1. Obiettivo

Estendere `sql"..."` con marcatori tipati che:

* dichiarano implicitamente il tipo di ritorno della query,
* espandono record in liste di colonne,
* permettono accesso tipato ai campi,
* definiscono parametri tipati e sicuri.

### 2. Sintassi

All’interno di `sql"..."` sono validi tre costrutti:

1. **Proiezione di record**

* `{T}` dove `T` è un tipo record.
* In contesto `SELECT`, `{T}` espande i campi del record in una lista di colonne.

2. **Riferimento a campo tipato**

* `a.{f}` dove `a` è un alias e `f` è un campo di un record precedentemente proiettato con alias.

3. **Parametro tipato**

* `@{p:U}` dove `p` è il nome parametro e `U` è il tipo Axom.

### 3. Tipizzazione (ritorno implicito)

* Se nella query è presente almeno una proiezione `{T}` in `SELECT`, allora il tipo della query è `Sql[T]`.
* Se in `SELECT` è presente una singola espressione tipata (es. `a.{id}`) senza `{T}`, allora il tipo è `Sql[U]` dove `U` è il tipo del campo.
* Altrimenti il tipo è `Sql[Row]`.

Ambiguità:

* Se `SELECT` contiene più proiezioni `{T1}`, `{T2}` o mix non riducibile a un singolo tipo, la query è `Sql[Row]` (o errore se `StrictTypedSelect` è attivo).

### 4. Espansione SQL

#### 4.1 `{T}` in SELECT

Dato `type T = { f1: A1, ..., fn: An }`:

* `{T}` espande in `f1, ..., fn`.
* `{T} a` (alias) espande in `a.f1, ..., a.fn`.

#### 4.2 `a.{f}`

* Si espande in `a.f`.
* È un errore compile-time se `f` non è campo del record associato ad `a`.

### 5. Binding parametri

`@{p:U}`:

* genera un placeholder nel testo SQL secondo il driver (es. `@p`, `$1`, `?`),
* registra metadati `(name=p, type=U)`,
* vieta concatenazione non tipata di valori nella query.

Il valore del parametro viene passato a runtime tramite:

* named args (es. `db.one(q, p: value)`), o
* ambiente catturato (se consentito dal linguaggio).

Mismatch tra `U` e valore runtime produce `DbError.ParamTypeMismatch`.

### 6. Mapping su record (byName implicito)

Per `Sql[T]` con `{T}`:

* il mapping riga→record è byName sui campi di `T`.
* ogni campo `fi` cerca una colonna risultante con nome `fi` (case policy definita dall’implementazione).
* `NULL` è ammesso solo se il tipo del campo è opzionale (`X?`).

Errori runtime:

* `MissingColumn(fi)`
* `TypeMismatch(fi, expected, got)`
* `NullViolation(fi)`

Colonne extra:

* ignorate di default.

### 7. Modalità di verifica

* Default: verifica durante la lettura della prima riga (fail-fast ma senza describe).
* Opzionale: `db.prepare(sql)` può eseguire prepare/describe e validare compatibilità shape prima dell’esecuzione.

### 8. Esempi

```axom
type User = { id: Int, name: String, age: Int? }

db.many(sql"select {User} from users")?

db.many(sql"
  select {User} u
  from users u
  where u.{age} > @{min:Int}
", min: 42)?

let id: Int =
  db.one(sql"
    select u.{id}
    from users u
    where u.{name} = @{n:String}
  ", n: "mario")?
```

### 9. Compatibilità e limiti

* `{T}` è definito solo per record “flat” (niente nested) nella prima versione.
* `a.{f}` è valido solo se `a` è alias legato a una proiezione `{T} a`.
* L’uso di `{T}` fuori da `SELECT` è non definito (errore o no-op a scelta implementativa).
