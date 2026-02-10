# Module Examples

This directory contains module/import examples for the planned M6 implementation.

## Valid

- `valid/app/import_simple.axom`
- `valid/app/import_alias.axom`
- `valid/app/from_import.axom`
- `valid/app/from_import_alias.axom`
- `valid/math/utils.axom`

## Invalid (expected diagnostics)

- `invalid/non_pub_symbol.axom` (imports non-`pub` symbol)
- `invalid/wildcard_import.axom` (wildcard import)
- `invalid/module_not_found.axom` (module resolution failure)
- `invalid/conflict_import.axom` (imported name conflict)
- `invalid/cycle/a.axom` + `invalid/cycle/b.axom` (import cycle)
