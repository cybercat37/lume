# Dubbi Aperti - Lume

Questioni di design ancora in discussione che richiedono una decisione.

---

## 1. Operatore `?` su Option

### Stato attuale

La spec dice:
> "The postfix operator `?` is defined **only for Result<T, E>**"

Ma nei punti aperti è segnato:
> "[x] Operatore `?` funziona anche su Option: **sì**"

### Il problema

```lume
// Se ? funziona solo su Result:
pub fn get_name(id: Int) -> Option<String> {
  let user = match find_user(id) {
    Some(u) -> u
    None -> return None   // Verboso
  }
  Some(user.name)
}

// Se ? funziona anche su Option:
pub fn get_name(id: Int) -> Option<String> {
  let user = find_user(id)?   // Più conciso
  Some(user.name)
}
```

### Opzioni

| Opzione | Pro | Contro |
|---------|-----|--------|
| **A: Solo Result** | Più esplicito, meno magia | Verboso per Option |
| **B: Result + Option** | Ergonomico, familiare (Rust/Swift) | Due semantiche per stesso operatore |

### Decisione

- [x] Opzione A: Solo Result
- [ ] Opzione B: Result + Option

---

## 2. Operatore `%` (modulo)

### Stato attuale

- Usato nell'esempio della spec: `x % 2 == 0`
- Elencato nei punti aperti
- **Non documentato formalmente** nella sezione operatori della spec

### Decisione

- [x] Aggiungere `%` alla spec formale

---

## 3. Escape sequences nelle stringhe

### Stato attuale

- Implementate nel Lexer: `\n`, `\t`, `\r`, `\\`, `\"`
- **Non documentate** nella spec

### Decisione

- [x] Aggiungere sezione "String Literals" alla spec

---

## 4. Sintassi commenti

### Stato attuale

- Implementati nel Lexer: `//` e `/* */`
- Deciso nei punti aperti: commenti non annidati
- **Non documentati** nella spec principale

### Decisione

- [x] Niente commenti nel linguaggio (scelta voluta; documentata in spec)

---

## Come risolvere

1. Prendi una decisione su ogni punto
2. Aggiorna la spec per riflettere la decisione
3. Rimuovi il dubbio da questo file

---

Ultimo aggiornamento: dopo Step 7
