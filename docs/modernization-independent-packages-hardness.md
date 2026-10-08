---
title: Independent package contract hardness report
version: 0.1
last_updated: 2026-10-02
status: Scoped verification; phase closure open
owner: IQToolkit project owner
tags: [modernization, verification, atlas, packaging]
---

# ATLAS hardness report: six independent package consumers

This report retains the original independent-consumer checkpoint and its historical run identities. The final combined batch/eager feed has separately reviewed locks and new consumer results in the [eager ownership report](modernization-eager-ownership-hardness.md); the source mechanics described here remain unchanged.

V1i in the [implementation plan](modernization-implementation-plan.md) verifies every active package with its own sole exact direct reference to version1.1.2-preview.1. [Six profiles](../eng/IndependentConsumers/Profiles) compile existing public toolkit symbols and the relevant transitive driver types. This exposes missing dependency declarations without borrowing another provider's direct references. Backend code is not executed.

## Isolation, locks and source provenance

The [preparation](../eng/prepare-independent-consumers.sh) and [locked verification](../eng/verify-independent-consumers.sh) commands use separate newly created outside-repository environments and per-profile cold caches. Matching restore/build properties exclude ambient repository props/targets/CPM. SDK selection follows the copied `global.json`; source mapping routes toolkit IDs exclusively to the exact tested local feed and external dependencies to nuget.org. Captures precede copies, every copy is checked, and original source/feed identities must remain unchanged.

Preparation `artifacts/independent-consumers/prepare-runs/run.8860AsAV` produces six actual locks bound to `artifacts/api-compatibility/runs/run.dqoq1XYU/packages`. Root reviewed sole direct dependencies, exact requested/resolved versions, declared toolkit/driver edges and lock identities at `artifacts/implicit-mapping/independent-lock-review.json`. Verification `verify-runs/run.QgzUdy9X` passes all six unchanged locked restores and strict no-restore public-symbol compilations with zero warnings/errors.

| Sole direct package | Actual resolved package count | Compile boundary |
| :--- | ---: | :--- |
| IQToolkit | 4 | Core public query/cache symbols. |
| IQToolkit.Data | 5 | Data provider, mapping/policy and core symbols. |
| IQToolkit.Data.SQLite | 12 | SQLite provider and Microsoft.Data.Sqlite. |
| IQToolkit.Data.SqlClient | 27 | SQL Server provider and Microsoft.Data.SqlClient. |
| IQToolkit.Data.MySqlClient | 15 | MySQL provider and MySql.Data. |
| IQToolkit.Data.Access | 8 | Access provider and System.Data.OleDb inside an explicit Windows support annotation. |

The framework-only verifier checks the sole exact dependency, complete package count/version identities, toolkit graph, source/cache paths, actual nuspec IDs/versions and public-symbol compile bodies/assembly references. Toolkit archive/DLL bytes must match the tested feed. NuGet unchanged locked restore enforces dependency edges; the verifier does not independently parse equivalent dependency-range syntax.

## Real tooling failures and corrections

Initial preparation `run.TpBlvZYO` failed because the verifier incorrectly equated a signed package's logical content hash with its full archive hash. Actual response metadata is preserved in `artifacts/implicit-mapping/signed-package-hash-inspection.json`. [NuGet's metadata contract](https://github.com/NuGet/Home/wiki/Nupkg-Metadata-File) distinguishes these hashes; its [archive reader](https://github.com/NuGet/NuGet.Client/blob/dev/src/NuGet.Core/NuGet.Packaging/PackageArchiveReader.cs) computes signed content separately.

The corrected verifier always checks complete archive SHA-512 against its sidecar, and logical content hash across lock/assets/source metadata. Differing domains require an actual root signature entry. Signature presence is not signature validation: signed content verification belongs to the trusted NuGet restore. Unsigned toolkit packages additionally require both hashes to equal the exact feed's bytes. Output labels archive and content hashes separately. No actual package, metadata or expected hash is changed to make verification pass.

Oracle also found aggregate success published before cleanup. The runner now records success only after project snapshot renaming and evidence relocation; failed moves produce failed status/nonzero exit. These are fact-preserving evidence mechanics corrections, not a production package repair or an artificial semantic RED.

## Retention and remaining limits

Sources are `.csproj.template`; only outside copies are executable projects. Before retention, project bytes become `.csproj.snapshot`, with SHA-256/name mappings for replay. This prevents the unsolicited in-repository restore that affected earlier SQLite consumer copies, as documented in the [SQLite report](modernization-package-consumer-hardness.md). Success requires all six per-profile results and retained aggregate evidence; there is no unlocked recovery after failure.

Root serialized all SDK operations. No concurrent runner, cleanup-failure injection, hosted Windows/macOS execution, driver/backend runtime, native OS matrix, LINQ consumer, trimming/AOT or publication qualification is claimed. Source/public artifact metadata contain no business records or credentials. No new runtime library dependency or generated provider fixture was added. Full coverage, fixture authorization, broader package/runtime requirements and the full roadmap remain open. Final independent Oracle source/evidence/report review is CLEAR. The verdict covers this scoped qualification and does not close M0 or approve outstanding policy/data/runtime gates.
