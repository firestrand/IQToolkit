---
title: Deferred command ownership hardness report
version: 0.1
last_updated: 2026-10-02
status: Scoped implementation; phase closure open
owner: IQToolkit project owner
tags: [modernization, verification, atlas]
---

# ATLAS hardness report: deferred command ownership

V1d in the [implementation plan](modernization-implementation-plan.md) adds one `using` declaration to `DbEntityProvider.Executor.ExecuteDeferred<T>`. Command acquisition still occurs at first enumeration. Existing cleanup closes the reader, then disposes the command, then releases the connection lease. Public signatures, projection, connection ownership and eager execution timing are unchanged. This extends the separate [normal nonquery disposal fix](modernization-command-ownership-hardness.md); broader ownership and the full roadmap remain open under the selected ENG-STD-DOTNET-XPLAT 1.1.0-draft.1.

## Edge cases tested

Six [native tests](../IQToolkitTest/RealProvider/DeferredCommandOwnershipTests.cs) cover complete and early-disposed enumeration with a borrowed-open or toolkit-opened connection, plus disposing an unstarted enumerator in each connection state. Full results and the first partial result match independently captured native compile options. The command observer returns the exact base-created native command and registers its disposal event; the reader observer returns the exact base reader. Neither cleans up for the toolkit. Reader `IsClosed`, command events and connection state are asserted before teardown.

Retained `artifacts/deferred-command/runs/red.a6yl7do6` has two intended missing-command-disposal failures and four controls passing. Both failures occur after successful native data, reader closure and borrowed-connection assertions. Closed-start connection closure masks the leak, so those passing cases alone are insufficient. After the declaration change, `green.35_blm_5` passes all six with identical tests/helper and actual native sequences. Unstarted cases acquire no command or reader. Oracle's focused review is CLEAR.

## Real-source provenance and failure handling

Tests capture live `PRAGMA compile_options` through the existing [helper](../IQToolkitTest/RealProvider/SQLiteMetadata.cs), with actual SQLite version/source output. Capture uses a separate connection to the installed native library; enumeration uses the observed connection. Required source cardinality fails explicitly, with no padding, fake rows, invented expected version or driver replacement.

The retained runs use SDK 10.0.401/runtime 10.0.12, Linux ARM64, Microsoft.Data.Sqlite/Core 10.0.12, SQLitePCLRaw 2.1.12 and SQLite 3.53.3. Native source ID is `2026-06-26 20:14:12 d4c0e51e4aeb96955b99185ab9cde75c339e2c29c3f3f12428d364a10d782c62`; existing lock provenance is captured in `artifacts/command-cache/sqlite-package-provenance.json`. Per-run source copies/hashes, logs and TRX preserve both sides of the regression.

The real driver's [reader Close](https://raw.githubusercontent.com/dotnet/efcore/v10.0.12/src/Microsoft.Data.Sqlite.Core/SqliteDataReader.cs) releases the reader independently of [command Dispose](https://raw.githubusercontent.com/dotnet/efcore/v10.0.12/src/Microsoft.Data.Sqlite.Core/SqliteCommand.cs). Observed semantic failures validate this distinction. No injected error, projector exception, setup/disposal failure, cancellation, eager/never-started reader or batch behavior is qualified here.

## Concurrency, load and security

Root serialized all builds, restores, packs and instrumentation. Each enumeration uses a separate native in-memory connection. There is no concurrent scope, contention, throughput or allocation claim. Only native library metadata is logged; no credentials, tenant/business rows or generated payloads were introduced. Live resource evidence grants no broader fixture authorization. Existing sensitive parameter logging remains an independent adoption finding.

## Verification commands and results

| Check | Actual evidence |
| :--- | :--- |
| Filtered strict Release MSTest, `DeferredCommandOwnershipTests` | Same-source RED 2 fail/4 pass → GREEN 6 pass/0 skips; retained runs above. |
| `GITHUB_ACTIONS=true bash eng/verify.sh` | `artifacts/verification/runs/run.44BwtQqp`: stable in-run snapshot, zero warnings/errors, 33 MSTest passes/325 unconfigured SQL Server skips, 247 translation passes. |
| `GITHUB_ACTIONS=true bash eng/verify-api.sh <pinned-release-packages>` | `artifacts/api-compatibility/runs/run.TfMqnXD2`: six fresh static comparisons, stable in-run source/reference/package identities. All library inputs match; the isolated consumer lock was deliberately refreshed afterward for these exact archive bytes and verified separately. |
| Native package inspection | Six library/symbol pairs pass; `artifacts/deferred-command/package-inspection.log`. |
| Managed coverage collector with existing settings | `artifacts/deferred-command/coverage.o9wbc6b9`: 33 pass/325 skips, 2554/8305 lines (30.75256%), 1386/5312 branches (26.09187%); all 336 captured DLL/PDB hashes match immediately before/after collection. |

Coverage includes core/Data/SqlClient/SQLite only; unloaded providers and translation executable are excluded. Scoped ratios exceed the preceding cache run's 2540/8305 lines and 1384/5312 branches. The collector reports the generated ExecuteDeferred iterator class at 100% lines/branches, including its reported finally methods. This does not prove exception/finally sensitivity or satisfy changed-file/whole-project coverage requirements; no coverage waiver is approved. Duplicate collector attachments have identical SHA-256 hashes, recorded in `summary.json`.

`artifacts/deferred-command/final-input-inspection.json` records snapshot identity. Documentation bookkeeping after the gate is reviewed separately; immediate binary restoration does not promise identities after a later rebuild. Final independent integrated Oracle review is CLEAR: identical focused sources/native responses, strict/API/package results, consumer lock exception and all restored binary identities were verified read-only. Local document target and diff checks pass.

## Remaining work

Observed complete/early deferred command disposal now passes without changing data or acquisition timing. Eager and never-started eager readers, batches, errors/cancellation, raw explicit transaction assignment, mutable policy invalidation, broader cache/array/consumer evidence, protected governance, coverage gates and subsequent provider milestones remain required. This report closes neither M0 nor the full goal.
