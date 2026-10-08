---
title: Eager enumeration ownership hardness report
version: 0.1
last_updated: 2026-10-02
status: Scoped implementation; integrated review CLEAR
owner: IQToolkit project owner
tags: [modernization, verification, atlas]
---

# ATLAS hardness report: eager enumeration ownership

This report retains the batch/eager checkpoint and its original feed/consumer/coverage identities. The later actual-native reader-acquisition failure guard and current combined verification are recorded in the [native failure report](modernization-native-failure-hardness.md); the focused normal/explicit-disposal evidence here remains unchanged.

V1e in the [implementation plan](modernization-implementation-plan.md) privately wraps the existing projected enumerable and enumerator to own its already-acquired native reader and command. Exhaustion, active disposal and explicit disposal before the first `MoveNext` release those resources. Eager acquisition, protected virtual `Project` invocation, outer `EnumerateOnce`, caller-owned connections, owned buffering and native values remain preserved. Public API comparisons pass against the pinned 1.1.1 release. The selected ENG-STD-DOTNET-XPLAT 1.1.0-draft.1 governs this work; full ownership and roadmap gates remain open.

## Edge cases tested

Four [native tests](../IQToolkitTest/RealProvider/EagerCommandOwnershipTests.cs) cover borrowed-open exhaustion, active early disposal, repeated explicit disposal of an unstarted enumerator, and initially-closed buffered execution. Borrowed paths assert native command/reader acquisition immediately after `Execute`. Complete enumeration manually advances until `MoveNext` returns false and checks cleanup before caller disposal, preventing automatic `ToArray` cleanup from masking missing exhaustion ownership. Unstarted enumeration invokes `Dispose` twice without any `MoveNext`, then checks native reader closure, exactly one command-disposal event and the open borrowed connection before teardown.

Retained `artifacts/eager-command/runs/red.rgjMcTni` records three intended semantic failures and one passing buffered control. Full and early cases close the reader but omit command disposal; unstarted disposal leaves the reader open and produces no command event. `green.bWWz9nCo` passes all four after the private wrapper change. `artifacts/eager-command/pair-inspection.json` binds identical tests/helper hashes and unchanged actual native observations. Focused independent Oracle review is CLEAR.

## Real-source provenance and failure handling

The unchanged [SQLiteMetadata helper](../IQToolkitTest/RealProvider/SQLiteMetadata.cs) captures all actual compile options, version, source ID and driver provenance from an independent ordinary native connection. Actual projection results match that capture. Transparent observers return the exact native resources. No generated rows, errors, schedules or substitute driver behavior were introduced; insufficient source cardinality fails explicitly.

The owning enumerator disposes the projection first, then closes an actual reader still open, then disposes the command through nested `finally` blocks. Its cleanup is idempotent, and a projection `MoveNext` error attempts cleanup before rethrowing. No injected or actual error-path experiment qualifies exception handling here.

An existing custom-projection gap remains: `_projection.GetEnumerator()` can fail before the owning enumerator is constructed, leaving already-acquired resources unowned. Command/reader acquisition failures and virtual `Project` construction failures also remain outside this proof. Custom buffered providers, reset, cancellation, concurrent enumeration and abandonment of an enumerable without an explicit enumerator/disposal call remain unqualified. No deterministic abandoned-result cleanup, finalizer or lazy execution promise is added.

## Concurrency, load and security

Root serialized all SDK and collector work. Tests use independent native in-memory connections and measure no concurrency, load, allocations or timing thresholds. Only native library metadata is retained; credentials and business/tenant rows are absent. Public APIs and runtime dependencies remain unchanged. Existing policy mutation and sensitive parameter logging remain separate roadmap work.

## Verification commands and results

| Check | Retained evidence |
| :--- | :--- |
| Focused strict Release MSTest, `EagerCommandOwnershipTests` | Separate same-source RED 3 failures/1 control → GREEN 4 passes; runs above. |
| `GITHUB_ACTIONS=true bash eng/verify.sh` | `artifacts/verification/runs/run.w85FnMzC`: zero warnings/errors, 50 MSTest passes/325 unconfigured SQL Server skips, 247 translation passes. |
| Final strict gate after all consumer-lock reviews | `artifacts/verification/runs/run.5GPZMLxq`: zero warnings/errors, 50 MSTest passes/325 skips, 247 translations. Subsequent report/status bookkeeping is inspected separately. |
| `GITHUB_ACTIONS=true bash eng/verify-api.sh <pinned-release-packages>` | `artifacts/api-compatibility/runs/run.A9nS44yE`: six fresh SDK API comparisons and library/symbol package checks against rebuilt pinned 1.1.1. |
| `dotnet msbuild eng/VerifyPackages.proj -nologo -p:PackageDirectory=<final-api-run>/packages` | Six library/symbol pairs pass; `artifacts/eager-command/package-gate.log`. |
| Explicit SQLite lock preparation → review → locked consumer | `run.YfmpU7WM` → `artifacts/eager-command/sqlite-lock-review.json` → `run.Ix9C3QR6`; only three toolkit content hashes change. Native executor version/options match the independent direct-driver oracle. |
| Six independent exact sole-direct consumers | Explicit preparation `run.VxGRbojO` → `artifacts/eager-command/independent-lock-review.json` → locked verification `run.tWm4quSN`; all six cold graphs restore unchanged and compile named public symbols. Only toolkit content hashes change; versions, dependency edges and external hashes remain unchanged. |
| Existing managed coverage collector, `--no-build --no-restore` | `artifacts/eager-command/coverage.szwen6sq`: 50 passes/325 skips, 2679/8347 lines (32.09536%), 1432/5320 branches (26.91729%); all 336 captured DLL/PDB hashes restored immediately. Both report attachments have identical SHA-256. |

Coverage includes core/Data/SqlClient/SQLite only. Overall measured ratios improve over the preceding mapping measurement (2595/8307 lines and 1405/5312 branches). The collector reports the private eager enumerator at 78.125% lines/87.5% branches; nongeneric enumeration, reset and errors remain unqualified. Base nonquery batch iterator reports 100% lines/branches, generic batch 94.44444% lines/75% branches, and the SQL Server batch override 0%. These measurements do not satisfy changed-file/full-project coverage requirements or qualify uncovered paths. No exclusion or approved waiver was added.

The consumers bind the exact final API feed `run.A9nS44yE/packages`, verify actual copied inputs and package/assembly provenance, and retain executed projects as byte-identical `.csproj.snapshot` files. Signed external package archive and NuGet logical content-hash domains remain distinguished by the existing verifier; toolkit unsigned bytes match the feed. The six profiles establish compilation/package contracts only; SQLite alone executes native queries. No other backend or operating-system runtime is certified.

`artifacts/eager-command/final-input-inspection.json` records stable in-run strict/API snapshots and subsequent reviewed consumer-lock/report bookkeeping. Compiled library/test inputs remain unchanged; document updates and explicit consumer lock refreshes are qualified separately rather than claiming unchanged whole-worktree snapshots. Historical mapping and consumer reports preserve their original run identities; this report is the current combined batch/eager checkpoint.

Independent integrated Oracle review is CLEAR: source, retained pairs, strict/API/packages, exact-feed consumer source/locks/hash domains, raw coverage totals and all restored/current binary identities were checked read-only. Report/status bookkeeping after the gate is qualified separately. This verdict does not waive any required coverage or remaining phase gate.

## Remaining work

The scoped normal/explicit-disposal fix is implemented. Integrated independent Oracle review is CLEAR for this checkpoint. Coverage targets and fixture decisions remain open; no phase or whole-roadmap completion follows from these tests. Broader resource/error/transaction ownership, mutable policy invalidation, async execution and additional provider milestones remain required.
