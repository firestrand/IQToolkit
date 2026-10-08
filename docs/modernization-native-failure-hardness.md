---
title: Native reader acquisition failure hardness report
version: 0.1
last_updated: 2026-10-02
status: Scoped implementation; integrated review CLEAR
owner: IQToolkit project owner
tags: [modernization, verification, atlas]
---

# ATLAS hardness report: native reader acquisition failure

V1f in the [implementation plan](modernization-implementation-plan.md) disposes the eager command if its existing protected virtual `ExecuteReader` call throws before returning a reader. The catch surrounds only that acquisition call and uses bare `throw`. It preserves immediate acquisition, successful projected resource ownership, outer `EnumerateOnce`, native results and the connection lease finally. Public surfaces and dependencies remain unchanged. The selected ENG-STD-DOTNET-XPLAT 1.1.0-draft.1 governs this scoped work; M0 and the full roadmap remain open.

## Native acquisition and provenance

An ordinary independent Microsoft.Data.Sqlite10.0.12 connection successfully reads the actual `pragma_compile_options` schema and all 51 unchanged metadata rows, then attempts fixed `DELETE FROM pragma_compile_options;`. It captures the genuine `SqliteException` and rereads the complete native metadata, which is unchanged. Ordinary command using/disposal and open connection controls pass before teardown. The retained acquisition run is `artifacts/native-command-failure/runs/acquire.uYeUuUbL`: one discovered pass, no skips, actual source/schema/error and copied source hashes. Independent acquisition Oracle review is CLEAR.

The installed native engine is SQLite3.53.3, source `2026-06-26 20:14:12 d4c0e51e4aeb96955b99185ab9cde75c339e2c29c3f3f12428d364a10d782c62`. Its [official release manifest](https://raw.githubusercontent.com/sqlite/sqlite/version-3.53.3/manifest.uuid) matches that hash, and the [versioned pragma module](https://raw.githubusercontent.com/sqlite/sqlite/version-3.53.3/src/pragma.c) has no xUpdate implementation. [SQLite virtual-table rules](https://www.sqlite.org/vtab.html#the_xupdate_method) explain this read-only capability. The actual response has primary and extended codes1/1 and message `SQLite Error 1: 'table pragma_compile_options may not be modified'.`; those fields are acquired afresh and compared unchanged, rather than supplied as expected fixture data.

SQL is operation configuration over actual native metadata. No generated rows, parameters, exception, result or mutated fixture is introduced; prospective routing and packet reviews are CLEAR. Missing table/schema, syntax/import/setup failure or unexpected success blocks acquisition rather than becoming an ownership RED. V0.2 remains unanswered for its separate generated-data tasks.

## Edge cases tested

Six [native tests](../IQToolkitTest/RealProvider/NativeCommandFailureTests.cs) cover the native acquisition/disposal control; borrowed-open raw nonquery, executor nonquery and deferred acquisition failures; borrowed-open eager acquisition failure; and initially-closed eager lease-release control. All toolkit exceptions match independently acquired actual type, primary/extended codes and message. The reader observer stores and rethrows the actual exception and returns base-created readers unchanged; eager/deferred callers receive that same exception object. Exactly one actual reader attempt and zero returned readers are asserted. Deferred cleanup is checked before caller disposal; eager failures occur synchronously at Execute. Resource assertions precede connection teardown.

Retained `red.fFbi6lyt` records exactly one intended borrowed eager missing-command-disposal failure after its native-error, exception-reference, no-returned-reader and borrowed-state assertions pass. Five controls pass. Native closure can mask an initially-closed command leak, so that case remains a lease/error preservation control. After the narrow acquisition catch, `green.WwKcij5g` passes all six. `artifacts/native-command-failure/pair-inspection.json` binds identical test/helper bytes and all native/toolkit error, metadata and provenance observations by named case, with the exact production diff.

## Tool and failure handling limits

The [versioned driver](https://raw.githubusercontent.com/dotnet/efcore/v10.0.12/src/Microsoft.Data.Sqlite.Core/SqliteCommand.cs) may allocate an internal reader before acquisition fails. The evidence establishes zero readers returned to the toolkit; it makes no zero-internal-allocation claim. The catch ensures the locally owned command is disposed for the observed acquisition rejection and rethrows the native exception unchanged.

GetCommand initialization, virtual Project construction, projection-enumerator construction, iteration/disposal failure, custom reader/command overrides, cancellation and transactions remain unqualified. If command Dispose itself throws, the cleanup exception can replace the acquisition failure; no fabricated throwing override is introduced to certify that unresolved contract. Existing broad ownership and error requirements remain active.

## Concurrency, load and security

Root owns and serializes all SDK, package and instrumentation work. Each native operation uses an isolated in-memory connection. No concurrent/fault schedule, load, throughput or allocation claim is made. Only installed library metadata and its rejection are retained; no user/tenant/business records, credentials or new runtime dependency were introduced. Sensitive parameter logging and mutable-policy behavior remain separate work.

## Verification commands and results

| Check | Retained result |
| :--- | :--- |
| Focused acquisition strict Release test | `acquire.uYeUuUbL`: one pass/no skips; actual metadata/error/native disposal controls. |
| Focused strict Release `NativeCommandFailureTests` | Same-source `red.fFbi6lyt` one intended failure/five controls → `green.WwKcij5g` six passes/no skips. |
| `GITHUB_ACTIONS=true bash eng/verify.sh` | `artifacts/verification/runs/run.FCTk8ehr`: zero warnings/errors, 56 MSTest passes/325 unconfigured SQL Server skips, 247 translation passes. |
| Final strict gate after reviewed locks/reports | `artifacts/verification/runs/run.jrnE7gSM`: zero warnings/errors,56 MSTest passes/325 skips,247 translations. Subsequent review/status bookkeeping is inspected separately. |
| `GITHUB_ACTIONS=true bash eng/verify-api.sh <pinned-release-packages>` | `artifacts/api-compatibility/runs/run.yD03s1yu`: six fresh SDK API comparisons against rebuilt pinned1.1.1 and six library/symbol package inspections. |
| `dotnet msbuild eng/VerifyPackages.proj -nologo -p:PackageDirectory=<final-api-run>/packages` | Six library/symbol pairs pass; `artifacts/native-command-failure/package-gate.log`. |
| Explicit SQLite lock preparation → review → locked execution | `run.cC6JER8p` → `artifacts/native-command-failure/sqlite-lock-review.json` → `run.nWqYEXgB`; actual native version/options match the direct-driver oracle. |
| Six independent exact sole-direct consumers | Preparation `run.R3yf1jCV` → `artifacts/native-command-failure/independent-lock-review.json` → locked verification `run.VKtHnSdE`; all six cold graphs restore unchanged and compile named public symbols. |
| Existing coverage collector, `--no-build --no-restore` | `artifacts/native-command-failure/coverage.97o8vboc`: 56 passes/325 skips, 2683/8351 lines (32.12789%), 1432/5320 branches (26.91729%); all336 captured DLL/PDB hashes restored immediately. Duplicate report attachments have identical SHA-256. |

All consumers bind the exact `run.yD03s1yu/packages` feed. Reviewed lock deltas change only toolkit content hashes; versions, dependency edges and external hashes remain unchanged. Actual copied source/feed/lock identities, package/assembly provenance and executed project `.snapshot` bytes are retained. The existing verifier distinguishes signed external archive SHA512 from NuGet logical content hashes; unsigned toolkit bytes match the feed. The six independent profiles qualify public compilation/package graphs, while SQLite also executes native queries. Other backend/OS runtime qualification remains open.

Measured coverage includes core/Data/SqlClient/SQLite only. Compared with the preceding eager checkpoint (2679/8347 lines, 1432/5320 branches), line ratio improves and branch ratio is unchanged. The collector reports eager Execute<T>100% instrumented lines/branches, including the now-executed acquisition catch; this does not certify every possible exception source or disposal failure. Executor class coverage is82.56881%lines/72.41379%branches. Required changed-file/full-project targets remain unmet; no coverage exclusion or waiver was added.

`artifacts/native-command-failure/final-input-inspection.json` records stable in-run strict/API inputs and their subsequent reviewed consumer-lock/report deltas. The final strict gate includes current reviewed consumer locks; its later deltas are documents/status only. All336 captured binaries also match current hashes after this final gate. Compiled library/test inputs remain unchanged; historical ownership/consumer reports retain their original proof identities.

Focused source/pair Oracle review is CLEAR. Independent integrated Oracle review is CLEAR for source, retained acquisition/regression pair, strict/API/package evidence, exact-feed consumer identities/hash domains, coverage totals and all restored/current binary hashes. No full phase, coverage waiver or roadmap completion is claimed.
