---
title: Native cold-initialization Probe hardness report
version: 0.1
last_updated: 2026-10-03
status: Scoped outcomes; integrated Oracle CLEAR
owner: IQToolkit project owner
tags: [modernization, probe, atlas]
---

# ATLAS hardness report: native cold initialization

V1k in the [implementation plan](modernization-implementation-plan.md) separately probes AttributeMapping entity publication and EntityProvider table-root publication for one existing immutable native mapping. Both bounded experiments ended with OverlapPrerequisiteUnmet. Every returned object matched its retained canonical object, but no recorded calls overlapped. Concurrent initialization remains unqualified; no semantic defect or repair follows from this observation.

## Acquisition and source fidelity

`artifacts/cold-initialization/runs/acquire.SEHG9msv` records one pass/no skips. SQLite3.53.3/source d4c0e51e...782c62, Microsoft.Data.Sqlite10.0.12 and runtime10.0.12 provide the unchanged actual `SELECT compile_options FROM pragma_compile_options;` response. The actual column is compile_options, System.String/TEXT, with51 rows. Existing public NativeProviderCacheTests.NativeMetadataEntry and CompileOptionsContext declare Entries, Table pragma_compile_options and Name mapped to compile_options. Reflection confirms that configuration; independent ordinary-driver and toolkit sequential projections produce the same native multiset. The initially closed connection returns Closed. Seven source/project/lock hashes and exact source copies accompany the TRX. Prospective routing and acquisition Oracle reviews are CLEAR.

No business rows, replacement native records, generated provider responses or error fixtures were constructed. AttributeMapping.GetEntity and EntityProvider.GetTable discard the return value of their ConcurrentDictionary.GetOrAdd calls; that source mechanism remains a hypothesis requiring separately reviewed Test→Implement and actual semantic RED before any repair. MappingEntity equality is structural, so reference divergence alone would not establish duplicate dictionary keys, incorrect rows or a security defect.

## Edge cases and bounded observations

`artifacts/cold-initialization/runs/probe.Ym60p2gX` discovers exactly three methods/no skips: one native acquisition pass, two empirical-prerequisite failures. Source snapshots, hashes, TRX and probe-observations.json preserve every attempt and actual native control.

| Target | Attempt1 | Attempts2–3 | Functional result |
| :--- | :--- | :--- | :--- |
| AttributeMapping, fresh entity cache | Threads5/7, disjoint call windows | Both calls on thread7, disjoint windows | Every returned reference equals the retained entity; structural equality also holds. |
| EntityProvider, fresh table cache with mapping dependency preacquired | Threads5/14, disjoint call windows | Both calls on thread14, disjoint windows | Every returned reference equals the retained table; each table retains the same preacquired entity. |

Each target independently used at most three fresh-cache attempts with two ordinary Task.Run calls each. Stopwatch frequency is1000000000; retained endpoints determine strict distinct-thread invocation overlap. All actual calls returned without exceptions. Actors only acquire metadata objects; table-provider connection state stays Closed. After joining, both returned roots execute Name projections sequentially against the independent actual native multiset. All twelve projections return the same51 rows; each connection returns Closed, and actual SQL is retained.

The focused Oracle independently reviewed execution/source fidelity and classified both failures CLEAR as OverlapPrerequisiteUnmet. No eligible concurrency interval, canonical mismatch or semantic RED was observed. Matching identities under these disjoint calls cannot prove the competing creation/publication path, exactly-once construction, linearizability or general provider thread safety.

## Tool failures, concurrency and security

The empirical failures remain in their original TRX and are not changed to passing skips. There are no barriers, sleeps, factory/comparer pauses, hooks, forced interleavings, random schedules, padding or retries beyond the prospective bound. No production, csproj or dependency change was made. SDK work is serialized by root; actors never perform concurrent database I/O. Retained data consists of native engine configuration, SQL, source and runtime observations, without credentials, business records or tenant data.

Per the workflow guide's disposable Technical Probe contract, root relocated only its temporary ColdInitializationProbe.cs to ignored `artifacts/cold-initialization/disposed-experiment.cs.snapshot`. disposition.json proves byte identity with the exact executed source. The experiment is absent from the normal build tree; acquisition and full Probe copies remain retained. The normal suite was rebuilt afterward; the protecting evidence below establishes its restored checkpoint.

## Protecting verification and remaining work

| Protecting check | Actual evidence |
| :--- | :--- |
| `GITHUB_ACTIONS=true bash eng/verify.sh` | artifacts/verification/runs/run.FVaJG04X:72passes/325unconfigured SQL Server skips,247translations, zero warnings/errors,254 stable in-run inputs; no temporary Probe methods. |
| Managed collector, `--no-build --no-restore --collect 'Code Coverage' --settings eng/coverage.runsettings` | artifacts/cold-initialization/coverage.jfq4xa6k:72passes/325skips;2709/8351lines (32.43923%),1438/5320branches (27.03008%), unchanged from V1j; four loaded core/Data/SqlClient/SQLite libraries only. Two identical XML copies, all336 fresh binary hashes restored immediately and still current. |
| Ordinary binary identity | artifacts/cold-initialization/ordinary-binary-identity.json: all336 binaries byte-equal to the V1j ordinary checkpoint, including the test assembly. |
| Library/archive/consumer identity | artifacts/cold-initialization/library-identity-inspection.json:138 library/compiler/config inputs unchanged from V1f API run.yD03s1yu,12 DLL/PDB payloads equal to their six original library/symbol archive payloads; both retained consumer input sets and app/test/engineering locks unchanged. |

No new package, lock refresh or consumer execution occurred. Exact library/input/archive/consumer identity supports retaining [V1f API/feed/consumer evidence](modernization-native-failure-hardness.md). Only this packet's documents/ignored evidence differ; this is not an unchanged whole-worktree claim. artifacts/cold-initialization/final-input-inspection.json qualifies report/status-only post-gate deltas and current restored binaries. No denominator exclusion, concurrency branch-coverage gain, target achievement or coverage waiver is claimed. Final integrated Oracle review is CLEAR for V1k only. The reviewer independently checked classifications, exact source disposition,254 stable inputs, ordinary test/translation counts, unchanged coverage,336 restored/current/V1j-identical binaries and library/archive/consumer identities.

Both canonical-initialization concurrency facts remain hypotheses. BasicMapping's separate unsynchronized dictionary/sharing contract, MRU overlap, cache clearing/eviction, mutable policy semantics, wider ownership/error cases, backend/OS certification and full-roadmap implementation remain open. Pending fixture/policy/coverage decisions are not answered by this Probe; mandatory coverage targets remain authoritative.
