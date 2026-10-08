---
title: Null-array membership hardness report
owner: implementation
tags: [dotnet, sqlite, verification]
version: 0.1
last_updated: 2026-10-04
status: Scoped verification complete; integrated Oracle CLEAR
---

# Null-array membership hardness report

The compiler-produced array membership predicate now has native regression evidence for the originally reported null boundary. The existing normalization fix passes ordinary SQLite queries both with and without QueryCache. No production source changed in this checkpoint.

The governing standard is ENG-STD-DOTNET-XPLAT 1.1.0-draft.1, explicitly selected by the owner. Its [retained source](standards/source/Cross-Platform-DotNet-Engineering-Standard-1.1.0-draft.1.md) has SHA256 `8c56930ec1cd26e8b58a273d47cb0b9713f55e6d4961b6d29a636ffaf4f0ebe2`. DOTNET-TEST-001 supplies native test/discovery and coverage preservation requirements; DOTNET-PROC-001 and DOTNET-FS-001 govern the private execution boundaries. This is scoped evidence, with standards adoption still PARTIAL.

## Edge cases tested

The original trusted review and fix request supply the canonical captured-null input and false membership expectation. Null is labeled requirement-supplied, never native-acquired. All nonempty arrays, empty arrays and expected result multisets come unchanged from real SQLite responses. Native nonempty readers report System.String/TEXT; the genuine zero-row readers report System.Byte[]/BLOB, retained as observations without an inferred string schema.

[NativeNullArrayMembershipTests](../IQToolkitTest/RealProvider/NativeNullArrayMembershipTests.cs) retains the acquisition case and adds exactly two behavioral cases. Each constructs one ordinary compiler-produced query with a null capture, then reuses it across null→native nonempty→native empty→original nonempty→null. Actual MemoryExtensions.Contains and ReadOnlySpan implicit-array conversion are checked. Both cached and uncached queries return the full independent native oracle multiset; every borrowed connection is Closed before and after enumeration. Actual SQL uses WHERE 0 <> 0 for null/empty and a parameterized IN predicate for the observed nonempty option. No predicate/delegate Compile, manual expression tree, cache-history assertion or fabricated entity row is used.

## Tool and service failure handling

Root ran all SDK processes serially with reviewed absolute executable paths, controlled environments, frozen source/argv/inputs, private directories, 120-second leaves, a 480-second primary deadline, bounded 16 MiB logs/512 MiB new evidence, native exit checks and terminal source/index/binary fences. Independent Oracle review identified and corrected failure-publication and post-cancellation preservation issues before dispatch. The private cleanup-only observer has a separate 30-second deadline; its design is reviewed, but cancellation/timeout execution is not qualified by these successful runs.

Every executed report/log remains native and unchanged. The four run environments were archived by same-filesystem rename with byte-identical files and original paths absent; both coverage helper project filenames are inactive. No forced termination, live session member, cancellation, storage or cleanup error occurred. Retained pre-acquisition Test DLL/PDB/generated inputs preserve the earlier compilation; aliases represent retained bytes, not new native executions.

## Concurrency and load considerations

The cached case uses a fresh QueryCache(2), with no policy replacement or shared concurrent access. The tests establish sequential capture replacement only. No cache-count/history, concurrent operation, performance or load claim follows. Native runtime evidence is limited to this Linux ARM64 host, .NET SDK 10.0.401/runtime 10.0.12 and SQLite 3.53.3 through Microsoft.Data.Sqlite 10.0.12.

## Security and data exposure

Native metadata values are selected as parameters for acquisition/oracle requests; no engine records or strings are invented. SQL, driver/runtime identities and metadata rows are retained locally without credentials or production data. No database installation, external service, package publication or remote Git action occurred.

## Verification commands and results

Exact argv, settings, source hashes and retained input maps are in each ignored execution packet. All commands ran from the repository or their explicitly recorded private helper workspaces.

| Evidence | Actual checks | Result |
| :--- | :--- | :--- |
| artifacts/native-null-array/acquire.7ovl4p05 | SDK version, locked CI restore, analyzed Release build, exact acquisition VSTest method | Four commands exited 0; one pass; zero warnings/errors; 317 inputs; six Test binary changes/330 others stable. |
| artifacts/native-null-array/behavior.nms6tn73 | Default optimized Release restore/build and exact class-filter VSTest | Four commands exited 0; three passes/ten enumerations; zero warnings/errors; 318 inputs; six Test binary changes/330 others stable. |
| artifacts/native-null-array/strict.n0witzgx; verification/runs/run.zRUV6Rn0 | GITHUB_ACTIONS=true bash eng/verify.sh and native result verification | 419 discovered;94 passes/325 explicit SQL Server skips;247 translations; zero warnings/errors;319 inputs/all 336 binaries/source/index stable. |
| artifacts/native-null-array/coverage.yrni_8j2 | Two private framework-only restore/build pairs, unchanged Collector prepare, copied-DLL native coverage test, collection/terminal preservation, descriptor and private comparator | Ten commands exited 0;848 inputs/all336 binaries/source/index stable;219 output files/shadow restored.97 sources/16,716 leaf structures and prior coverage preserved. |

The coverage transition compares the pinned V1y descriptor against the actual current Test compilation, permits only the new Test DLL/PDB pair and one added PDB document, and binds the three compiled public test methods to their source and authentic passing reports. All 416 inherited definitions/categories/outcomes remain unchanged. The permanent original baseline and original→V1y transition/input chain remain intact; durable coverage tools and policy were not changed. Root retained219 compiled outputs for each run, plus 45 behavioral, 54 strict and 301 coverage environment files. Acquisition's 45 environment files and 219 outputs are also retained.

Actual coverage remains 2732/8349 lines and 1460/5320 branches. No line, branch, condition or denominator regression was observed; individual branch-arm identity and the project target are not qualified.

## Outcome and follow-ups

Scoped acquisition, behavioral, strict and source-changing coverage observations all received independent Oracle CLEAR. Protecting inspection passed: 137 production/configuration inputs, ten source/consumer locks, twelve archive payloads and all 336 current binaries remain unchanged. Independent integrated Oracle review is CLEAR for this scoped checkpoint. The original null boundary is now tested through actual translation/execution, including cached replacement and a genuinely acquired empty array.

Broader ARRAY.NULL requirements for explicit Enumerable behavior, other types/providers/overloads remain unqualified. The durable final profile still pins the older Test compilation; authenticating this transition in that entry point is separate work. Coverage targets remain unmet/unwaived, as do the outstanding fixture/policy/public/backend/platform decisions. This checkpoint does not close M0, M1 or the full roadmap.
