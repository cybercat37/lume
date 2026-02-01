# Step 11: Performance and Caching (12 Sub-steps)

Goal: introduce basic performance improvements and caching without changing language semantics.

1) Performance baselines
   Capture simple baseline timings for compile and run.
   DoD:
   - Document baseline commands.

Baseline commands:
- `dotnet run --project src/axom -- check hello.axom`
- `dotnet run --project src/axom -- build hello.axom`
- `dotnet run --project src/axom -- run hello.axom`
- `dotnet test tests/Axom.Tests/Axom.Tests.csproj --filter "FullyQualifiedName~CompilerPipelineTests"`

2) Parser allocation review
   Identify hot allocations in lexer/parser.
   DoD:
   - Notes captured with candidates.

Notes (candidates):
- Lexer uses `Substring` for identifiers/numbers/strings; allocates per token.
- Lexer builds `StringBuilder` for every string literal.
- SyntaxTree collects tokens into `List<SyntaxToken>` and filters bad tokens.
- Parser allocates `List<StatementSyntax>` for blocks and `List<ExpressionSyntax>` for call arguments.
- Diagnostics aggregation uses `Concat(...).ToList()`.

3) Syntax tree reuse (optional)
   Cache syntax trees by source hash.
   DoD:
   - Re-parse avoided for identical input.

4) Binder cache (optional)
   Cache bound programs per syntax tree hash.
   DoD:
   - Binding avoided for identical input.

5) Emitter cache (optional)
   Cache generated code when bound program unchanged.
   DoD:
   - Codegen avoided for identical input.

6) Incremental compile surface
   Add a compile method that accepts a cache object.
   DoD:
   - API supports reuse of cached stages.

7) Memory usage guardrails
   Add a small guard or limit for large inputs.
   DoD:
   - Errors are surfaced via diagnostics.

8) Large file smoke test
   Add a test that compiles a large input.
   DoD:
   - Ensures no timeouts in CI.

9) Determinism check
   Ensure cached and uncached outputs are identical.
   DoD:
   - Tests compare outputs.

10) CLI caching toggle (optional)
   Add a CLI flag to enable cache.
   DoD:
   - Cache on/off behavior documented.

11) Perf notes
   Document findings and tradeoffs.
   DoD:
   - Notes stored in STEP11 doc.

Notes:
- Cache keys use in-memory identifiers (hash codes); suitable for same-process reuse only.
- Caches reduce repeated parse/bind/emit for identical sources within a run.
- Max source length guard prevents pathological inputs from dominating memory/time.
- No timing instrumentation added yet; baseline commands serve manual checks.

12) Documentation
   Update AGENTS/README with performance tools.
   DoD:
   - Commands documented.

## Status
- Sub-step 1: complete
- Sub-step 2: complete
- Sub-step 3: complete
- Sub-step 4: complete
- Sub-step 5: complete
- Sub-step 6: complete
- Sub-step 7: complete
- Sub-step 8: complete
- Sub-step 9: complete
- Sub-step 10: complete
- Sub-step 11: complete
- Sub-step 12: complete
