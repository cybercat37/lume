# Module Examples

This directory contains module/import examples for the M6 implementation (partial, resolver/parser v1).

## Valid

- `valid/app/import_simple.axom`
- `valid/app/from_import.axom`
- `valid/math/utils.axom`

## Parsed-only (alias forms; resolver diagnostics expected)

- `valid/app/import_alias.axom`
- `valid/app/from_import_alias.axom`

## Invalid (expected diagnostics)

- `invalid/non_pub_symbol.axom` (imports non-`pub` symbol)
- `invalid/wildcard_import.axom` (wildcard import)
- `invalid/module_not_found.axom` (module resolution failure)
- `invalid/conflict_import.axom` (imported name conflict)
- `invalid/cycle/a.axom` + `invalid/cycle/b.axom` (import cycle)
