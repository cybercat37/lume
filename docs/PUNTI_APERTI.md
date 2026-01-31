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
- [ ] Return implicito (ultima espressione) o esplicito (`return`)?
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
- [ ] Sintassi abbreviata per lambda single-expression?
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
- [ ] Uguaglianza referenziale (`===`)? Oppure no perché immutabile?
- [ ] Confronto tra tipi diversi: errore compile-time?

---

### 2.2 Logici

| Operatore | Significato |
|-----------|-------------|
| `&&` | AND logico (short-circuit) |
| `\|\|` | OR logico (short-circuit) |
| `!` | NOT logico |

**Da decidere:**
- [ ] `and`/`or`/`not` come keyword alternative?

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
- [ ] `+` per concatenazione stringhe o operatore dedicato?
- [ ] Divisione decimale? (richiede Float)
- [ ] Overflow: wrap, panic, o checked?

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
- [ ] Pipe operator `|>` incluso?
- [ ] Se sì, semantica (passa come primo o ultimo argomento)?

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
- [ ] `Float`/`Double` incluso?
- [ ] Dimensione di `Int` (32 o 64 bit)?
- [ ] `Char` separato o solo `String`?
- [ ] `Byte` / `UInt`?

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
- [ ] Keyword `type` o `record`?
- [ ] Costruttore automatico?
- [ ] Update syntax (`{ ...user, age: 31 }`)?
- [ ] Destructuring in pattern match?

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
- [ ] Sintassi per varianti con/senza payload?
- [ ] Associated values come Rust o come Gleam?

---

### 3.4 Generics

```lume
fn identity<T>(x: T) -> T { x }

type Box<T> {
  value: T
}
```

**Da decidere:**
- [ ] Sintassi `<T>` o `[T]`?
- [ ] Constraints/bounds (`<T: Comparable>`)?
- [ ] Inferenza di tipo per generics?

---

### 3.5 Type Alias

```lume
type UserId = Int
type Handler = fn(Request) -> Response
```

**Da decidere:**
- [ ] Alias sono distinti o equivalenti al tipo base?
- [ ] Newtype pattern supportato?

---

## 4. Collezioni

### 4.1 List

```lume
let items = [1, 2, 3]
let first = items[0]  // Oppure items.get(0)?
```

**Da decidere:**
- [ ] List immutabile di default?
- [ ] Indexing con `[]` o solo metodi?
- [ ] Out of bounds: panic o Result?

---

### 4.2 Map/Dict

```lume
let ages = { "Alice": 30, "Bob": 25 }
```

**Da decidere:**
- [ ] Sintassi literal per mappe?
- [ ] Chiavi: solo String o qualsiasi hashable?

---

### 4.3 Tuple

```lume
let pair = (1, "hello")
let (x, y) = pair
```

**Da decidere:**
- [ ] Tuple incluse?
- [ ] Destructuring nelle let?
- [ ] Accesso con `.0`, `.1` o solo destructuring?

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
- [ ] Interpolazione con `{}` o `${}`?
- [ ] Prefisso `f""` necessario?
- [ ] Espressioni dentro interpolazione (`{x + 1}`)?

---

### 5.2 Operazioni

**Da decidere:**
- [ ] Concatenazione: `+` o `++` o metodo?
- [ ] `String.length`, `String.split`, etc. in stdlib?

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
- [ ] Un file = un modulo?
- [ ] Moduli annidati?
- [ ] `import` vs `use`?
- [ ] Aliasing (`import math as m`)?
- [ ] Import selettivo (`import math.{add, sub}`)?

---

### 6.2 Visibilità

**Da decidere:**
- [ ] Solo `pub` e privato?
- [ ] `pub(module)` per visibilità interna?

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
- [ ] Rest pattern (`..`) per ignorare campi?
- [ ] Guards (`if condition`)?
- [ ] Range pattern (`1..10`)?
- [ ] List pattern (`[head, ..tail]`)?

---

### 7.2 Exhaustiveness

**Da decidere:**
- [ ] Warning o errore per match non esaustivo?
- [ ] `_` obbligatorio o opzionale come catch-all?

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
- [ ] Built-in o definito in stdlib?
- [ ] Operatore `?` funziona anche su Option?
- [ ] `.unwrap()` esiste? (panic se None)

---

### 8.2 Result

```lume
type Result<T, E> {
  Ok(T)
  Error(E)
}
```

**Da decidere:**
- [ ] `Error` o `Err` come nome?
- [ ] Tipo errore standard? (`type Error = String`?)
- [ ] `.unwrap()` esiste?

---

## 9. Commenti e Documentazione

### 9.1 Commenti

```lume
// Single line

/* Multi
   line */
```

**Da decidere:**
- [ ] Commenti annidati permessi?

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
- [ ] `///` o `/** */` per doc?
- [ ] Formato (Markdown)?
- [ ] Tool per generare docs?

---

## 10. Altre decisioni

### 10.1 Shadowing

```lume
let x = 1
let x = 2  // Permesso?
```

**Da decidere:**
- [ ] Shadowing permesso nello stesso scope?

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
- [ ] Funzioni si vedono prima della definizione?
- [ ] Keyword per forward declaration?

---

### 10.3 Attributes/Annotations

```lume
@test
fn test_add() { ... }

@deprecated("use new_fn instead")
pub fn old_fn() { ... }
```

**Da decidere:**
- [ ] Sistema di attributi incluso?
- [ ] Sintassi `@attr` o `#[attr]`?

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
