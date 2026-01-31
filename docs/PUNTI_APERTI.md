# Punti Aperti - Lume Language Spec

Decisioni di design ancora da prendere o dettagli da specificare.

---

## 1. Funzioni

### 1.1 Sintassi base

```lume
fn add(a: Int, b: Int) -> Int {
  a + b
}

pub fn greet(name: String) {
  println "Hello " + name
}
```

**Da decidere:**
- [x] Return implicito (ultima espressione); `return` consentito per early exit.
- [x] Funzioni senza return type hanno tipo `Unit`.
- [x] Parametri opzionali/default values: no (per ora).
- [x] Named arguments: no (per ora).

---

### 1.2 Lambda/Closure

```lume
let double = fn(x: Int) -> Int { x * 2 }
items.map(fn(x) { x * 2 })
```

**Da decidere:**
- [x] Sintassi abbreviata per lambda single-expression: sì (es. `fn(x) => x * 2`).
- [x] Cattura variabili: by value.
- [x] Cattura di `mut` permessa: no.
- [x] Closures in task spawn: sì, restrizioni (solo catture immutabili/copy).

---

### 1.3 Entry Point

**Da decidere:**
- [x] Top-level statements come script; `fn main()` opzionale per programmi strutturati.
- [x] Args da command line come parametri di main: no/poi.

---

## 2. Operatori

### 2.1 Confronto

| Operatore | Significato |
|-----------|-------------|
| `==` | Uguaglianza strutturale |
| `!=` | Disuguaglianza |
| `<` | Minore |
| `>` | Maggiore |
| `<=` | Minore o uguale |
| `>=` | Maggiore o uguale |

**Da decidere:**
- [x] Uguaglianza solo strutturale (`==`); niente `===`.
- [x] Confronto tra tipi diversi: errore compile-time.

---

### 2.2 Logici

| Operatore | Significato |
|-----------|-------------|
| `&&` | AND logico (short-circuit) |
| `\|\|` | OR logico (short-circuit) |
| `!` | NOT logico |

**Da decidere:**
- [x] `and`/`or`/`not` come keyword alternative: no.

---

### 2.3 Aritmetici

| Operatore | Significato |
|-----------|-------------|
| `+` | Addizione / Concatenazione stringhe? |
| `-` | Sottrazione |
| `*` | Moltiplicazione |
| `/` | Divisione intera |
| `%` | Modulo/Remainder |

**Da decidere:**
- [x] `+` per concatenazione stringhe.
- [x] Divisione decimale: dopo (richiede Float).
- [x] Overflow: no silent wrap (checked; diagnostic/panic).

---

### 2.4 Pipe Operator

```lume
// Opzione A: |> come Gleam/Elixir
data
  |> transform
  |> validate
  |> save

// Opzione B: method chaining
data.transform().validate().save()
```

**Da decidere:**
- [x] Pipe operator `|>` incluso: sì, ma dopo (non ora).
- [x] Semantica pipe: passa come primo argomento.

---

## 3. Tipi

### 3.1 Tipi primitivi

| Tipo | Descrizione |
|------|-------------|
| `Int` | Intero 64-bit? 32-bit? |
| `Bool` | true/false |
| `String` | UTF-8 immutabile |
| `Float` | ??? |
| `Unit` | Tipo vuoto (nessun valore) |

**Da decidere:**
- [x] `Float`/`Double` incluso: sì (Double), ma dopo.
- [x] Dimensione di `Int`: 64-bit.
- [x] `Char` separato o solo `String`: solo `String` per ora.
- [x] `Byte` / `UInt`: no per ora.

---

### 3.2 Records

```lume
// Definizione
type User {
  name: String
  age: Int
}

// Costruzione
let user = User { name: "Alice", age: 30 }

// Accesso
user.name
```

**Da decidere:**
- [x] Keyword per records: `type`.
- [x] Costruttore automatico: sì.
- [x] Update syntax (`{ ...user, age: 31 }`): sì.
- [x] Destructuring in pattern match: sì.

---

### 3.3 Sum Types / Enum

```lume
type Status {
  Active
  Inactive
  Pending(reason: String)
}

let s = Status.Pending("waiting approval")

match s {
  Active -> "ok"
  Inactive -> "no"
  Pending(r) -> "waiting: " + r
}
```

**Da decidere:**
- [x] Sintassi varianti con/senza payload: come Rust/Gleam (`Variant` / `Variant(value)`).
- [x] Associated values: stile Rust/Gleam con payload tra parentesi.

---

### 3.4 Generics

```lume
fn identity<T>(x: T) -> T { x }

type Box<T> {
  value: T
}
```

**Da decidere:**
- [x] Sintassi generics: `<T>`.
- [x] Constraints/bounds: non ora.
- [x] Inferenza di tipo per generics: sì, dove possibile.

---

### 3.5 Type Alias

```lume
type UserId = Int
type Handler = fn(Request) -> Response
```

**Da decidere:**
- [x] Alias equivalenti al tipo base.
- [x] Newtype pattern: non ora.

---

## 4. Collezioni

### 4.1 List

```lume
let items = [1, 2, 3]
let first = items[0]  // Oppure items.get(0)?
```

**Da decidere:**
- [x] List immutabile di default: sì.
- [x] Indexing con `[]`: sì.
- [x] Out of bounds: Result (niente panic silenzioso).

---

### 4.2 Map/Dict

```lume
let ages = { "Alice": 30, "Bob": 25 }
```

**Da decidere:**
- [x] Sintassi literal per mappe: sì.
- [x] Chiavi: solo String per ora.

---

### 4.3 Tuple

```lume
let pair = (1, "hello")
let (x, y) = pair
```

**Da decidere:**
- [x] Tuple incluse: sì.
- [x] Destructuring nelle let: sì.
- [x] Accesso: solo destructuring (no `.0/.1` per ora).

---

## 5. Stringhe

### 5.1 Interpolazione

```lume
// Opzione A
let msg = "Hello {name}, you are {age} years old"

// Opzione B
let msg = "Hello " + name + ", you are " + age.to_string() + " years old"

// Opzione C
let msg = f"Hello {name}, you are {age} years old"
```

**Da decidere:**
- [x] Interpolazione con `{}`.
- [x] Prefisso `f""` necessario.
- [x] Espressioni dentro interpolazione: sì.

---

### 5.2 Operazioni

**Da decidere:**
- [x] Concatenazione: `+`.
- [x] `String.length`, `String.split`, etc. in stdlib: sì.

---

## 6. Moduli e Visibilità

### 6.1 Moduli

```lume
// file: math.lume
pub fn add(a: Int, b: Int) -> Int { a + b }
fn helper() { ... }  // privato

// file: main.lume
import math
let x = math.add(1, 2)
```

**Da decidere:**
- [x] Un file = un modulo: sì.
- [x] Moduli annidati: no per ora.
- [x] Keyword: `import`.
- [x] Aliasing (`import math as m`): sì.
- [x] Import selettivo (`import math.{add, sub}`): sì.

---

### 6.2 Visibilità

**Da decidere:**
- [x] Solo `pub` e privato.
- [x] `pub(module)`: no per ora.

---

## 7. Pattern Matching

### 7.1 Pattern supportati

| Pattern | Esempio |
|---------|---------|
| Literal | `0`, `"hello"`, `true` |
| Variable | `x`, `_` |
| Constructor | `Some(x)`, `None` |
| Record | `User { name, .. }` |
| Or | `0 \| 1 \| 2` |
| Guard | `x if x > 0` |

**Da decidere:**
- [x] Rest pattern (`..`): sì.
- [x] Guards (`if condition`): sì.
- [x] Range pattern (`1..10`): no per ora.
- [x] List pattern (`[head, ..tail]`): sì.

---

### 7.2 Exhaustiveness

**Da decidere:**
- [x] Match non esaustivo: errore.
- [x] `_` catch-all: opzionale (ma consigliato).

---

## 8. Option e Result

### 8.1 Option

```lume
type Option<T> {
  Some(T)
  None
}
```

**Da decidere:**
- [x] Option: definito in stdlib.
- [x] Operatore `?` funziona anche su Option: sì.
- [x] `.unwrap()` esiste (panic su None): sì.

---

### 8.2 Result

```lume
type Result<T, E> {
  Ok(T)
  Error(E)
}
```

**Da decidere:**
- [x] Nome variante: `Error`.
- [x] Tipo errore standard: `type Error = String` (per ora).
- [x] `.unwrap()` esiste (panic su Error): sì.

---

## 9. Commenti e Documentazione

### 9.1 Commenti

```lume
// Single line

/* Multi
   line */
```

**Da decidere:**
- [x] Commenti annidati: no.

---

### 9.2 Doc comments

```lume
/// Adds two numbers
/// 
/// # Examples
/// ```
/// add(1, 2) == 3
/// ```
pub fn add(a: Int, b: Int) -> Int { a + b }
```

**Da decidere:**
- [x] Doc comments: `///`.
- [x] Formato: Markdown.
- [x] Tool per generare docs: sì, ma dopo.

---

## 10. Altre decisioni

### 10.1 Shadowing

```lume
let x = 1
let x = 2  // Permesso?
```

**Da decidere:**
- [x] Shadowing nello stesso scope: no.

---

### 10.2 Ricorsione mutua

```lume
fn is_even(n: Int) -> Bool {
  match n {
    0 -> true
    _ -> is_odd(n - 1)
  }
}

fn is_odd(n: Int) -> Bool {
  match n {
    0 -> false
    _ -> is_even(n - 1)
  }
}
```

**Da decidere:**
- [x] Funzioni visibili prima della definizione: sì (supporto mutual recursion).
- [x] Forward declaration keyword: no.

---

### 10.3 Attributes/Annotations

```lume
@test
fn test_add() { ... }

@deprecated("use new_fn instead")
pub fn old_fn() { ... }
```

**Da decidere:**
- [x] Sistema di attributi: sì, ma dopo.
- [x] Sintassi: `@attr`.

---

## Priorità suggerita

1. **Funzioni** - senza di esse nulla funziona
2. **Operatori confronto/logici** - necessari per match
3. **Pattern matching completo** - il cuore del linguaggio
4. **Sum types** - per Result/Option
5. **Records** - per dati strutturati
6. **Generics base** - per collezioni tipizzate
7. **Moduli** - per organizzare codice
8. **Tutto il resto** - può aspettare

---

Ultimo aggiornamento: dopo Step 7
