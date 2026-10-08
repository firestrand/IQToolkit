---
title: Native metadata cache correctness hardness report
version: 0.1
last_updated: 2026-10-02
status: Scoped implementation; phase closure open
owner: IQToolkit project owner
tags: [modernization, verification, atlas]
---

# ATLAS hardness report: cache execution, lookup and root identity

The V1c packet in the [implementation plan](modernization-implementation-plan.md) corrects three observed cache defects. `QueryCache.Execute(IQueryable)` now executes the expression instead of returning an equality Boolean. MRU lookup returns the actual matching entry, preserves default output on a miss and promotes the canonical cached object. Compiled queries retain captured `IQueryable` root identity, preventing a delegate compiled against one root from substituting its results for another same-type root. Public signatures, ordinary constant comparison, parameterization and the existing lock/version protocol are preserved.

Later [deferred ownership](modernization-deferred-ownership-hardness.md) and [isolated consumer](modernization-package-consumer-hardness.md) reports supersede integrated gate/coverage observations; this cache checkpoint remains historical to its stated snapshot.

This is scoped synchronous correctness evidence under the owner's selected .NET standard, ENG-STD-DOTNET-XPLAT 1.1.0-draft.1. It does not close M0 or the full [roadmap](modernization-roadmap.md). Coverage, broader cache boundaries/concurrency and later provider obligations remain open.

## Edge cases tested

The [cache tests](../IQToolkitTest/RealProvider/CacheMetadataTests.cs) replay unchanged, live native SQLite metadata values through actual .NET query roots. Typed and expression execution controls protect successful compilation. Tests cover untyped sequence results, default output on a nonadding miss, matching head/canonical identity, add-enabled promotion and eviction, a nonadding hit's unchanged recency, Clear followed by insertion, and preservation of a cached object when an equal but distinct native string is looked up.

Two ordered Test→Implement pairs retain the same test/helper source and observed native values within each pair:

| Pair | Retained RED | Retained GREEN |
| :--- | :--- | :--- |
| Execution and sequential MRU | `artifacts/command-cache/runs/red-lookup.rsrxq1b4`: 7 intended failures, 2 passing compilation controls | `artifacts/command-cache/runs/green-lookup.c1rpo6oa`: all 9 pass |
| Distinct captured root substitution, after the MRU repair | `artifacts/command-cache/runs/red-root.tx3hz28a`: 2 result-substitution failures, 11 passes | `artifacts/command-cache/runs/green-root.58usw519`: all 13 pass |

The second pair checks both source orders, repeat access to the same root and parameterization while the captured selection changes between two unchanged native rows. Native version/options sequences independently determine expected results; cache entry counts alone are not the oracle. Source copies, SHA-256 manifests, logs, TRX and actual native observations are retained per run. Oracle independently reviewed the source/data identity and semantic failures; these pair reviews are CLEAR.

The separate [relational characterization tests](../IQToolkitTest/RealProvider/NativeProviderCacheTests.cs) first acquire real native virtual-table schema and direct SQL result multisets. Two distinct SQLite providers share one cache and the same CLR/table-ID shape while mapping `Name` to either compile options or function names. They read mapped **Name string projections**; the CLR type declares the mapping contract, and no `NativeMetadataEntry` objects are manually constructed or claimed as materialized entities. Comparisons preserve duplicate function names. Actual executor logs confirm the mapped native tables were queried.

A second characterization uses the existing `EntityPolicy.Apply` filter with two actual observed option names, two separately configured policies and direct parameterized native SQL controls. Both characterization tests alternate providers, repeat queries, clear the cache and exercise the reverse insertion order. Each expects independent results and two cache entries. Final `artifacts/command-cache/runs/verify-provider.kw5_6xyn` reports 2 passes/0 skips, with saved source identities, schemas, rows and SQL logs. This is a Verify task after the repair, with legitimate first-green protection; no relational RED is invented. Oracle's source/evidence review is CLEAR.

## Real-source provenance

The [capture helper](../IQToolkitTest/RealProvider/SQLiteMetadata.cs) reads documented [compile options](https://www.sqlite.org/pragma.html#pragma_compile_options), [function metadata](https://www.sqlite.org/pragma.html#pragma_function_list) and [running-library version/source metadata](https://www.sqlite.org/lang_corefunc.html#sqlite_version). Relational characterization uses documented [result-bearing pragma tables](https://www.sqlite.org/pragma.html#pragfunc) and actual column schema before declaring its mappings. Values are captured and replayed unchanged; tests require sufficient native distinct/duplicate values and fail a data prerequisite if unavailable, with no generated fallback.

All retained runs observe SQLite `3.53.3`, source ID `2026-06-26 20:14:12 d4c0e51e4aeb96955b99185ab9cde75c339e2c29c3f3f12428d364a10d782c62`. Execution used SDK `10.0.401`, runtime `10.0.12`, Linux ARM64, Microsoft.Data.Sqlite/Core `10.0.12` and SQLitePCLRaw components `2.1.12`. Exact resolved package versions/content hashes are exported from the existing lock at `artifacts/command-cache/sqlite-package-provenance.json`.

In relational tests, provenance helper calls use a separate in-memory connection to the same installed process/library; schema, source rows, parameterized controls and ORM execution use the tested connection. This evidence does not certify a different native installation or provider. No generated payloads, expected option/version strings, cloned records or simulated provider results are used. Live metadata evidence grants no generated-input authorization for other tasks.

## Tool and service failure handling

Both RED pairs fail semantic assertions after native data acquisition and successful compilation controls. Missing infrastructure/imports or unavailable metadata would not count as RED. The earlier provider Verify run `verify-provider.1mx_e1lc` remains historical; the final run adds both insertion orders and is not substituted into either RED/GREEN pair.

Invalid capacity, injected comparison errors, query compilation/execution failures, cancellation and same-root provider/policy mutation are not characterized. In particular, `EntityProvider.Policy` and `EntityPolicy.Apply` remain mutable after compilation. Root identity alone does not establish an invalidation contract. That contract needs a separate plan/characterization packet before a mutation fix or context-token API.

## Concurrency and load considerations

Root serialized all builds, restores, packs and instrumentation. Native lookup tests use fixed sequential operations, not generated schedules. The version-rescan code follows the existing lock protocol by inspection, but its intervening-mutation path is not executed in this evidence. No concurrent cache, contention, throughput, allocation or tenant-isolation qualification is claimed.

Retaining strict query-root identity can reduce reuse across distinct root instances that previously appeared equal solely by CLR type. That unsafe reuse produced observed wrong results; no benchmark or performance-improvement claim follows from correcting it.

## Security and data exposure

Only native library metadata is printed. No credentials, business/tenant rows, synthetic payloads or simulated policies/drivers were introduced. Separate configured provider policies demonstrate result separation for these native filters; they are not security-boundary or bypass tests. Existing parameter logging remains a separate adoption finding. No warning suppression, compatibility suppression, coverage waiver or publication was introduced.

## Verification commands and results

| Check | Actual result and evidence |
| :--- | :--- |
| Filtered strict Release MSTest for `CacheMetadataTests` | Ordered same-source RED/GREEN pairs above; final 13 pass/0 skips. |
| Filtered strict Release MSTest for `NativeProviderCacheTests` | Final 2 pass/0 skips; actual schema, direct SQL controls, both orders and execution logs retained. |
| `GITHUB_ACTIONS=true bash eng/verify.sh` | `artifacts/verification/runs/run.ug2yIPB9`: stable before/after snapshots, zero warnings/errors, 27 MSTest passes/325 unconfigured SQL Server skips, 247 translation passes. |
| `GITHUB_ACTIONS=true bash eng/verify-api.sh <pinned-release-packages>` | `artifacts/api-compatibility/runs/run.WzuGcqaM`: six fresh static SDK comparisons passed against rebuilt pinned v1.1.1, stable source/package/reference identities. All 176 API compiler inputs matched at the V1c checkpoint; later deferred/consumer changes have separate evidence. |
| `dotnet msbuild eng/VerifyPackages.proj -p:PackageDirectory=<current-api-run>/packages -warnaserror` | Six preview library/symbol pairs passed; `artifacts/command-cache/package-inspection.log`. |
| Full MSTest with existing managed coverage collector/settings | `artifacts/command-cache/coverage.lbnr2aig`: 27 pass/325 skips, four loaded libraries, 2540/8305 lines (30.58399%) and 1384/5312 branches (26.05422%). All 336 captured DLL/PDB SHA-256 identities match immediately before/after collection. |

The four loaded libraries are core, Data, SqlClient and SQLite; unloaded Access/MySQL and the translation executable are excluded. Scoped ratios exceed the prior command-ownership run's 2300/8300 lines and 1232/5314 branches. They do not establish whole-project or touched-file targets. Collector detail reports MRU Lookup at 78.37838% lines/72.22222% branches, `QueryCache` at 90.38462% lines/65.38462% branches and `CompareConstantValues` at 75% lines/40% branches; all three Execute overloads are fully covered. Version-rescan/concurrency and other constant/parameterization paths remain uncovered. No sensitivity or comprehensive algorithm qualification follows from those percentages.

`artifacts/command-cache/final-input-inspection.txt` records stable in-run snapshots and compiler identities at the V1c checkpoint. Documentation bookkeeping after the strict run is separately reviewed; the full strict snapshot is historical rather than claimed byte-identical to later documentation. Binary restoration describes the immediate collector boundary, not arbitrary later rebuilds. Final independent integrated Oracle review is CLEAR: retained pairs/provider evidence, current API inputs, strict/API/package metrics and all restored binary identities were checked read-only. `git diff --check` and local Markdown target inspection pass.

## Remaining work

Pass: observed untyped execution, sequential MRU and distinct-root result-substitution defects are corrected with ordered regressions and protecting controls; actual SQLite mapping/configured-policy characterization, strict build, API/package checks and measured no-regression scope pass.

Open: required coverage targets and the unanswered coverage-policy decision, boundary/concurrency/error contracts, same-root policy invalidation, constructor/type/mapping caches, package consumers and array regressions, wider command/reader/transaction ownership, protected governance and all subsequent roadmap milestones. The [normal command-disposal report](modernization-command-ownership-hardness.md) remains a separate scoped implementation. No full phase or goal is closed by this report.
