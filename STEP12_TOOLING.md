# Step 12: Tooling, Packaging, CI (12 Sub-steps)

Goal: ship developer tooling, packaging, and CI foundations.

1) CLI installation plan
   Decide how to install the CLI (local tool, global tool, or release zip).
   DoD:
   - Approach documented.

Decision:
- Use `dotnet tool` packaging via NuGet (global or local install).

2) Versioning scheme
   Define versioning and release tagging.
   DoD:
   - Version source of truth documented.

Decision:
- SemVer in `src/axom/axom.csproj` (`<Version>`).
- Tag releases as `vX.Y.Z` matching the tool version.

3) `dotnet tool` packaging (optional)
   Add NuGet packaging if applicable.
   DoD:
   - Tool package builds locally.

Command:
- `dotnet pack src/axom -c Release`

4) Release build profile
   Add a release build command/profile.
   DoD:
   - `dotnet publish` instructions documented.

Command:
- `dotnet publish src/axom -c Release -o out/publish`

5) CI build/test workflow
   Add CI workflow for build + test.
   DoD:
   - CI runs `dotnet test`.

6) CI hardening targets
   Add CI job for golden/snapshot tests.
   DoD:
   - `make test-hardening` runs in CI.

7) CI fuzz (optional)
   Add short fuzz run in CI.
   DoD:
   - `make fuzz` runs in CI.

8) Release artifacts
   Create build outputs for multiple platforms.
   DoD:
   - Output is versioned and reproducible.

Commands:
- `dotnet publish src/axom -c Release -r linux-x64 -o out/publish/linux-x64`
- `dotnet publish src/axom -c Release -r win-x64 -o out/publish/win-x64`
- `dotnet publish src/axom -c Release -r osx-x64 -o out/publish/osx-x64`

9) CLI help polish
   Improve `--help` content and command descriptions.
   DoD:
   - Usage includes options and examples.

10) Shell completion (optional)
    Provide completions for Bash/Zsh/PowerShell.
    DoD:
    - Instructions documented.

11) Licensing and metadata
   Ensure license and metadata are set.
   DoD:
   - License file present, README updated.

Notes:
- LICENSE: Apache-2.0.
- Tool metadata set in `src/axom/axom.csproj`.

12) Documentation
    Update AGENTS/README with tooling and CI usage.
    DoD:
    - Commands and CI steps documented.

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
- Sub-step 11: complete
- Sub-step 12: complete
- Sub-step 10: complete
