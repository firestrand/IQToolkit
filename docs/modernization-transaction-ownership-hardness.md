---
title: Native transaction scope ownership hardness report
version: 0.1
last_updated: 2026-10-03
status: Scoped verification; integrated Oracle CLEAR
owner: IQToolkit project owner
tags: [modernization, verification, atlas]
---

# ATLAS hardness report: native transaction scope ownership

V1h in the [implementation plan](modernization-implementation-plan.md) verifies existing synchronous transaction/connection ownership with real native state, unchanged SQLite metadata and its actual rejected write. This is test-only, first semantic GREEN Verify under selected ENG-STD-DOTNET-XPLAT1.1.0-draft.1. Production, public APIs, dependencies and locks are unchanged; M0 and the full roadmap remain open.

## Native acquisition and independent oracle

`artifacts/transaction-ownership/runs/acquire.LUO8U3Q0` records one discovered pass/no skips. An ordinary native connection supplies all 51 unchanged string/TEXT compile options and installed SQLite3.53.3/source d4c0e51e...782c62, driver10.0.12, raw wrapper2.1.12 and runtime10.0.12 identities. Native autocommit reports1→0→1 on begin/commit before disposal. A second actual transaction reports0 after DELETE FROM pragma_compile_options fails, stays attached, then caller rollback reports1. Actual SqliteException primary/extended1/message `SQLite Error 1: 'table pragma_compile_options may not be modified'.` is acquired inside the transaction; it is not an injected response. The provider's existing ReadCommitted configuration resolves to actual Serializable in this driver, with no cross-provider isolation equivalence claimed. All metadata stays unchanged.

Existing [SQLitePCLRaw2.1.12](https://raw.githubusercontent.com/ericsink/SQLitePCL.raw/v2.1.12/src/SQLitePCLRaw.core/raw.cs) invokes the real native autocommit API; [SQLite's contract](https://www.sqlite.org/c3ref/get_autocommit.html) gives nonzero/zero transaction-state meaning. Only open, exclusively used handles are observed. No new dependency, fabricated rows/errors/states/schedules, randomized inputs or fixture mutation are used. Missing valid source/state/rejection is a failed prerequisite, never a passing skip. Acquisition Oracle review is CLEAR.

## Edge cases verified

Seven [tests](../IQToolkitTest/RealProvider/TransactionScopeOwnershipTests.cs) include the acquisition control and six explicit configurations: toolkit-created transaction with caller-opened/initially-closed connection × success/native rejection, and caller-supplied transaction × success/native rejection. Every case reacquires actual native rows and active-transaction rejection independently. Inside the toolkit action the actual transaction is attached, native autocommit disabled and actual rows match the independent capture.

Created transactions complete, Transaction clears and caller-opened connections stay open. Initially-closed connections close; a transparent Close observer records enabled autocommit/completed transaction/cleared property immediately before ordinary base.Close. These captured states are asserted after scope exit, preventing connection teardown from masking incomplete work. The observer never completes transactions or substitutes native cleanup. Supplied transactions remain the same reference, attached and native-active after the toolkit scope returns/throws. Only after those checks does caller rollback/disposal occur.

Failure actions capture the actual driver exception and bare-rethrow; the caller receives that same reference with independently captured native type/codes/message. Ownership assertions precede caller connection/transaction cleanup. Transaction.Connection null proves completion only: [version-matched driver](https://raw.githubusercontent.com/dotnet/efcore/v10.0.12/src/Microsoft.Data.Sqlite.Core/SqliteTransaction.cs) clears it on commit or rollback, so it is not a Dispose-event oracle.

`artifacts/transaction-ownership/runs/verify.4ZhITvBe` passes seven discovered cases/no skips on its first semantic execution. Earlier retained verify.Qh1qUc9h and verify.SyBZpD34 are compiler failures for a missing QueryPolicy namespace, not semantic RED; no tests ran. The corrected source, hashes, TRX, statuses and named-case native observations are retained. No production repair was needed or fabricated.

## Tool failures, concurrency and security

Root serializes SDK/collector operations and each test exclusively uses isolated in-memory native connections. No concurrent scope, load, performance or asynchronous I/O claim follows. Only library metadata/native state and actual errors are retained; no credentials/business rows/tenant data are introduced. Native acquisition and compiler failures remain visible in retained evidence. Fixture, policy and mandatory coverage decisions remain pending.

## Protecting verification and limits

| Check | Observed result |
| :--- | :--- |
| Filtered strict Release acquisition | acquire.LUO8U3Q0: one discovered pass/no skips, actual native state/rejection/provenance. |
| Filtered strict Release Verify | verify.4ZhITvBe: seven discovered passes/no skips; focused Oracle CLEAR. |
| `GITHUB_ACTIONS=true bash eng/verify.sh` | artifacts/verification/runs/run.t2dTZuYI: zero warnings/errors, 72 MSTest passes/325 unconfigured SQL Server skips, 247 translation passes; 252 in-run inputs unchanged. |
| Existing managed collector, `--no-build --no-restore` | artifacts/transaction-ownership/coverage.AwtFQdRR: 72 passes/325 skips; 2709/8351 lines (32.43923%), 1438/5320 branches (27.03008%); all 336 fresh captured binary hashes restored immediately and still current. Two XML copies have identical SHA-256. |
| Library/archive/consumer identity | artifacts/transaction-ownership/library-identity-inspection.json: 138 library/compiler/configuration inputs unchanged from API run.yD03s1yu, including eng/dead-code.globalconfig; 12 current DLL/PDB bytes match six exact original library/symbol archive payloads. Both retained consumer input sets and app/test/engineering dependency locks remain unchanged. |

The 138-input inspection adds the unchanged analyzer configuration to the prior V1g 137-input set. The only additional C# sources relative to the API snapshot are the V1g and V1h test files. No library, public API, tooling/dependency or package-feed change occurred. The [V1f API/package checkpoint](modernization-native-failure-hardness.md), SQLite run.nWqYEXgB and six-independent run.VKtHnSdE remain the exact retained consumer evidence; direct input/payload/consumer identity gates justify reuse without repacking or regenerating locks. This is not a new consumer run or an unchanged whole-worktree claim.

Coverage includes core/Data/SqlClient/SQLite only. Counts increase from V1g2693/8351 lines and1436/5320 branches. Instrumented DoTransacted is100% lines/branches on these actual cases; mandatory changed-file/full-project targets and broader failures remain unmet/unqualified. No denominator exclusion or waiver was introduced. Report/status-only post-gate deltas and current restored binaries are recorded in artifacts/transaction-ownership/final-input-inspection.json. Final integrated Oracle review is CLEAR for V1h only. The reviewer independently confirmed the scoped source/results, 252 stable inputs, 138 library/config identities, 12 packaged payloads and 336 restored/current binary hashes.

This verifies observed completion and ownership, not persisted business-write rollback, transaction Dispose events, nested/savepoint behavior, begin/commit/rollback/disposal failures, arbitrary callback failures, concurrent use, cancellation or other engines. SQLite automatically attaches active transactions to created commands; portable raw command.Transaction assignment remains unqualified. Full coverage targets, async/new providers and the complete roadmap remain required.
