---
title: Relational aggregate culture hardness
version: 0.1
last_updated: 2026-10-03
status: Scoped verification; integrated Oracle CLEAR
owner: IQToolkit project owner
tags: [modernization, globalization, sql, verification]
---

# ATLAS hardness report: native relational aggregate culture

V1v corrects a machine-keyword defect under DOTNET-GLOB-001 of the owner-selected .NET standard. QueryBinder retains CLR aggregate names; the shared SQL formatter previously uppercased them using the current culture. Actual Turkish LINQ Min emitted `MİN`, which SQLite rejected as an unknown function. The default aggregate-name branch now uses `ToUpperInvariant()`. Average/LongCount spelling, numeric literal formatting and other branches remain unchanged; no public API, dependency or project configuration is changed by this packet.

## Edge cases tested

[NativeAggregateCultureTests](../IQToolkitTest/RealProvider/NativeAggregateCultureTests.cs) acquires direct native min/max responses from unchanged pragma_compile_options, source column name/count, actual TEXT/System.String result schema, native version/source and installed tr-TR/driver/runtime metadata. Existing native mapping types are reused unchanged; no replacement records or handwritten expected aggregate values are constructed. The independent direct native aggregate is the result oracle.

Acquisition `artifacts/sql-aggregate-culture/acquire.mhwaiift` passes one case. Actual source schema and two nonempty native aggregate strings qualify on SQLite 3.53.3 with driver 10.0.12. Source/data/prospective/actual acquisition reviews are Oracle CLEAR. Tests then exercise actual uncached public relational Min/Max through binder, formatter and scalar executor, using default policy.

RED `red.vt2pafag` retains one genuine native unknown-function failure for Turkish Min and three passing acquisition/invariant Min/Turkish Max controls. The test logs emitted `MİN`, actual SqliteException and independent native oracle, then rethrows the original exception. GREEN `green.04ve16sm` passes all four unchanged cases with matching actual aggregate values/profile. Only default keyword normalization changes. Test-source, causal RED and focused pair reviews are Oracle CLEAR; all source snapshots and terminal outcomes are retained.

Evidence qualifies those actual SQLite metadata aggregates under the installed Turkish/invariant profiles. It does not certify every aggregate or dialect, custom aggregate names, nullable/empty inputs, wider cultures, application data collation, concurrency or overall M0. User-facing string behavior is unchanged. Existing V1u floating-literal tests remain protected by the full suite.

## Tool and service failures

Restore/build and focused GREEN pass with zero warnings/errors; the single RED is a genuine native execution failure, not test setup failure. Acquisition captures responses before string prerequisites. Missing native source or installed culture must stop affected work rather than supply values or invent errors. All evidence directories are unique and retained. Unexpected failure signatures require causal review before repair; no retry or skip weakens the semantic gate.

## Concurrency and security

DoNotParallelize prevents this class from overlapping culture tests. Only thread CurrentCulture changes; finally restores and asserts the original instance on success and RED. No UI/default-thread/custom culture is used. Root serializes SDK operations and agents are read-only; neither test serialization nor this casing change establishes a library concurrency/load guarantee.

Actual source/aggregate strings are engine configuration metadata, not application or production records. SQL logging is confined to these non-sensitive test operations. The fix normalizes a machine keyword and does not claim general injection protection, diagnostic redaction or user-data collation behavior.

## Verification

Fresh API `artifacts/api-compatibility/runs/run.5ufBDbfn` completes six comparisons against the pinned 1.1.1 baseline with resolved references and no suppression. Package inspection `packages.3t0a61g3` checks six exact library/symbol manifest pairs. Symbol `artifacts/package-symbols/runs/run.OcdjS8qx` validates six identity/checksum/commit-bound Source Link pairs. Dirty source metadata does not prove clean remote source retrieval or artifact disclosure approval.

Explicit preparations `artifacts/independent-consumers/prepare-runs/run.eXCaw5mP` and `artifacts/package-consumer/lock-preparation-runs/run.MFP1Blzn` refresh seven actual locks against that exact feed. The existing strict verifier permits only feed-bound Toolkit contentHash leaves; node sets, dependencies, versions, external hashes and the core-only one-node graph remain identical. App/test locks are unchanged. CI `artifacts/ci-consumers/runs/run.gc4CF10a` passes seven shadow reviews, six cold locked public-symbol compiles and native SQLite executor verification. No CI guard is widened.

Fresh ordinary `artifacts/verification/runs/run.pZJ4jvTR` passes 86 tests, with 325 unconfigured SQL Server skips, and 247 translation checks with zero warnings/errors and264 stable inputs. Collector `artifacts/sql-aggregate-culture/coverage.4g3h_mc5` measures 2728/8349 lines (32.67457%) and 1459/5320 branches (27.42481%), compared with V1u 2724/8349 and 1449/5320. Only core/Data/SqlClient/SQLite load. All 336 captured DLL/PDB hashes immediately restore and match ordinary outputs. This is measured no-regression; mandatory workflow targets remain unmet and no exception is approved.

`python3 -I artifacts/sql-aggregate-culture/validate-checkpoint.py` passes: identical actual pair sources/oracles/profiles/restorations, precisely one production keyword conversion, 137 current library/configuration inputs, 12 current archive payloads, 135 current source document checksums, seven strict hash-only locks, consumers and strict/restored coverage are qualified. Only plan/state/this report follow the final gate. Final integrated Oracle review is CLEAR for the bounded V1v repair and current protecting artifacts. Prior V1u payload/coverage qualifications are historical following this production edit. Mandatory coverage targets, hosted/native matrix, pending data/policy/public decisions and complete roadmap remain open.
