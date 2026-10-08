---
title: Normal nonquery command ownership hardness report
version: 0.1
last_updated: 2026-10-02
status: Scoped implementation; phase closure open
owner: IQToolkit project owner
tags: [modernization, verification, atlas]
---

# ATLAS hardness report: normal nonquery command ownership

The V1r pair in the [implementation plan](modernization-implementation-plan.md) fixes command disposal in raw `ExecuteCommand(string)` and `Executor.ExecuteCommand(QueryCommand, object[])`. Two `using` declarations dispose commands inside the existing connection lease. Public signatures, return logic, parameter setup and transaction setup are preserved. This proves normal-path disposal with native SQLite observations; broader resource ownership and the full roadmap remain open.

## Edge cases tested

Five [focused tests](../IQToolkitTest/RealProvider/CommandOwnershipTests.cs) use actual Microsoft.Data.Sqlite commands. The connection observer returns the exact base-created native command unchanged and subscribes to its native disposal event. Every assertion occurs before connection teardown.

- A direct consumer-disposal control confirms the native event is observable.
- Raw and executor commands release their command while preserving a caller-opened connection.
- Raw and executor commands release their connection lease when they open the connection themselves.

The retained RED run has two failures for missing native disposal on the caller-opened paths and three passes. After the two-line fix, the identical test source passes all five cases. Closed-start cases alone would be insufficient: SQLite closes registered commands when its connection closes, masking a toolkit leak.

## Real-source provenance

The tests execute documented [running-library metadata](https://www.sqlite.org/lang_corefunc.html#sqlite_version) using [native in-memory configuration](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/connection-strings). They create no user tables or business records and invent no returned value, error or expected version string. The control records native SQLite `3.53.3`, source ID `2026-06-26 20:14:12 d4c0e51e4aeb96955b99185ab9cde75c339e2c29c3f3f12428d364a10d782c62` in both TRX files.

Execution used SDK `10.0.401`, runtime `10.0.12`, Linux ARM64, Microsoft.Data.Sqlite/Core `10.0.12` and SQLitePCLRaw components `2.1.12`. The actual package versions/content hashes are captured from the existing lock in `artifacts/command-ownership/sqlite-package-provenance.json`. Test/source copies and SHA-256 identities are retained beside the RED/GREEN logs. Live metadata evidence does not authorize generated fixtures for any other task.

## Tool and service failure handling

The RED failures are semantic assertions after successful native execution, rather than missing imports, unavailable services or injected failures. The retained RED was rerun after adding metadata provenance output; the earlier preliminary log was overwritten, so only the retained source-identical RED is claimed here.

No execution-error, command-creation/setup failure, cancellation or disposal-error regression ran. Although lexical disposal applies on execution exceptions, those outcomes are unproved by this evidence. Failure-specific behavior needs its own expanded packet and provenance or authorization.

## Concurrency and load considerations

Root serialized all build, restore, pack and instrumentation operations. Each focused test uses a separate native in-memory connection. No concurrency, throughput, allocation or cross-provider qualification follows from these checks.

## Security and data exposure

No credentials, business rows or synthetic payloads were introduced. The only printed provider values identify the native SQLite build. Existing parameter logging remains a separate adoption finding. Tests do not alter native transaction defaults or replace readers/results/errors with a simulator.

## Verification commands and results

| Check | Actual result and evidence |
| :--- | :--- |
| Filtered strict Release MSTest for `CommandOwnershipTests` | RED: 2 fail/3 pass; GREEN: 5 pass/0 skips. Same test source in `artifacts/command-ownership/red/` and `green/`; provider copies differ only by the two `using` declarations. |
| `GITHUB_ACTIONS=true bash eng/verify.sh` | Passed stable snapshots: zero warnings/errors, 12 MSTest passes/325 unconfigured integration skips, 247 translations. `artifacts/verification/runs/run.niFdPZiA`. |
| `GITHUB_ACTIONS=true bash eng/verify-api.sh <pinned-release-packages>` | Six fresh static comparisons passed with stable source/package/reference hashes. `artifacts/api-compatibility/runs/run.5UTsrF9A`. |
| `dotnet msbuild eng/VerifyPackages.proj -p:PackageDirectory=<current-api-run>/packages -warnaserror` | Six library/symbol pairs passed; `artifacts/command-ownership/package-inspection.log`. |
| Full MSTest with existing static managed collector/settings | 12 pass/325 skip. Four loaded libraries: 2300/8300 lines (27.71084%), 1232/5314 branches (23.18404%), exceeding the preceding scoped ratios. All 336 captured DLL/PDB hashes match immediately before/after collection. `artifacts/command-ownership/coverage.jkzq2j8t/`. |
| Source review and `git diff --check` | Two production-line changes; diff check passed. Final independent Oracle integration review CLEAR, including all 173 current API inputs and durable restoration hashes. Phase closure remains open. |

The collector reports raw-command method coverage of 8/9 lines and 1/2 branches: the existing optional logging branch is uncovered. The executor method reports full coverage in this scope. The changed-file/final-project coverage targets are not established; the coverage-policy decision remains unanswered. No comprehensive exception/finally sensitivity claim follows from line coverage.

## Remaining work

A later [deferred ownership report](modernization-deferred-ownership-hardness.md) records scoped complete/early deferred command disposal. Eager and never-started eager readers, batches, errors/cancellation and explicit raw transaction assignment still require separate regressions and implementation packets. SQLite automatically attaches its active transaction, so an unchanged SQLite driver cannot expose the missing explicit assignment by itself. Other provider qualification, package consumers, cache/array regressions, protected governance and the remaining roadmap milestones are still required. This report records a verified narrow implementation and does not close V1r, M0 or the full goal under unresolved coverage gates.
