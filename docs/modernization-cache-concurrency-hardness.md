---
title: Bounded native cache concurrency Probe hardness report
version: 0.1
last_updated: 2026-10-03
status: Scoped Probe outcomes; integrated Oracle CLEAR
owner: IQToolkit project owner
tags: [modernization, probe, atlas]
---

# ATLAS hardness report: bounded native cache concurrency Probe

V1j in the [implementation plan](modernization-implementation-plan.md) measures cold cache calls over unchanged acquired native strings. QueryCache has one eligible observed overlapping invocation/result-isolation case. MRU has no eligible overlap after three bounded attempts; its concurrency prerequisite is unmet. This is an honest technical Probe outcome, not general concurrency certification or a completed M0/full roadmap.

## Acquisition, source fidelity and independent oracle

`artifacts/cache-concurrency/runs/acquire.s1q67GPN` records one pass/no skips with source/hashes/TRX/live provenance. Installed SQLite3.53.3/source d4c0e51e...782c62 returns51 compile options and192 function names; driver/runtime10.0.12 and actual schemas are captured. The two actual options are ATOMIC_INTRINSICS=1 and COMPILER=gcc-13.3.0; two actual group_concat name strings compare equal but have distinct acquired object references. Read-only wrapping preserves the original strings. Ordinary acquisition connections dispose before actors launch. Oracle acquisition review is CLEAR; no fabricated string copies/rows/errors/state/schedules or pending data authorization are used.

One AsQueryable root wraps the unchanged read-only native sequence. The same query-building helper captures each actual selection, leaving closures unchanged. Independent expected results come from direct enumeration/filtering of the original capture outside IQToolkit. Runtime-compatible EnumerableQuery compilation is supported by existing passing CacheMetadataTests and matched primary sources in the plan; no fake provider or new dependency is introduced.

## Recorded execution and edge cases

`artifacts/cache-concurrency/runs/probe.nAhJLVIw` discovers exactly three methods/no skips. Native acquisition and QueryCache pass. MRU fails solely with OverlapPrerequisiteUnmet after the declared maximum three attempts; this is neither semantic cache RED nor an implementation request. All attempted inputs/call windows/results/thread IDs/errors/source copies and TRX are retained.

| Experiment | Actual result |
| :--- | :--- |
| Cold QueryCache attempt1 | Threads12/8; call windows strictly overlap. Each actual selection matches its own direct native oracle, cache.Count1 and both expressions remain present. No invocation/enumeration error. |
| Cold MRU attempt1 | Threads14/8 but disjoint invocation windows. Both returned references equal the retained canonical first native reference, Count1. |
| Cold MRU attempts2–3 | Both calls use thread8 with disjoint windows and opposite observed winner. Both returned references equal the retained second native reference, Count1. |

QueryCache Execute is timed separately from enumeration, which occurs after both actors complete. With Stopwatch.Frequency1000000000, the actual QueryCache overlap is21,364,096ticks (about21.36ms), calculated from retained endpoints. This is an observed invocation interval, not a throughput/latency benchmark or proof of critical-section overlap. MRU reference identity is asserted against the live retained canonical object and the original acquired references; no winner/order/Boolean-return history is imposed.

Only two ordinary Task.Run calls per attempt are submitted, with fresh cold caches and at most three attempts per case. There are no barriers, sleeps, comparer pauses/hooks, prewarming, random schedules, padding or forced histories. All functional assertions apply even without overlap. No functional defect was observed and no production repair or artificial RED was created.

## Disposition, failures, concurrency and security

The workflow's Technical Probe contract removes experimental code afterward. Root relocated only its own temporary NativeCacheConcurrencyProbe.cs from the build tree to ignored `artifacts/cache-concurrency/disposed-experiment.cs.snapshot`; exact executed source copies remain in each run and disposition.json records identity. No csproj, library, dependency, package or permanent opt-in setting was changed. Source snapshots preserve reproducibility without placing a scheduler-dependent overlap test in the default suite.

The focused MRU failure and all attempts remain visible; missing overlap is not converted to a passing skip or retried beyond the bound. Root owns all source/instrumentation and serializes SDK operations. Actors share immutable captured values/cache only, never a SQLite connection. Only library metadata and runtime observations are retained, without credentials/business/tenant data. This Probe neither resolves pending fixture/policy/coverage decisions nor supplies performance/load/security qualification.

## Protecting gates and residual limits

| Protecting check | Actual evidence |
| :--- | :--- |
| `GITHUB_ACTIONS=true bash eng/verify.sh` | artifacts/verification/runs/run.TPH63yBX: zero warnings/errors,72passes/325unconfigured SQL Server skips,247translations;253 in-run inputs stable. Ordinary TRX contains no experimental Probe method. |
| Existing managed collector, `--no-build --no-restore` | artifacts/cache-concurrency/coverage.2wmHKIaG:72passes/325skips;2709/8351lines (32.43923%),1438/5320branches (27.03008%), exactly unchanged from V1h. All336 freshly captured DLL/PDB hashes restored immediately and still current; two XML copies share SHA-256. |
| Ordinary binary identity after source removal | artifacts/cache-concurrency/ordinary-binary-identity.json: all336 ordinary binaries match the V1h pre-experiment checkpoint, including the test assembly; experimental code is absent from the restored build. |
| Library/archive/consumer identity | artifacts/cache-concurrency/library-identity-inspection.json: all138 library/compiler/config inputs unchanged from API run.yD03s1yu,12 current DLL/PDB payloads byte-equal to six original library/symbol archive payloads, both retained consumer input sets and app/test/engineering dependency locks unchanged. |

No new package/feed/lock generation or consumer run occurred. Exact library/input/payload/consumer identity justifies retaining [V1f API/package/consumer evidence](modernization-native-failure-hardness.md); documents/ignored evidence are deliberate deltas, never an unchanged whole-worktree claim. Coverage includes core/Data/SqlClient/SQLite only and excludes the removed experiment. No concurrency branch-coverage increase, denominator exclusion, whole-project/touched-file target or waiver is claimed. Report/status-only post-gate deltas and current restored binaries are recorded in artifacts/cache-concurrency/final-input-inspection.json. Prospective/acquisition/focused and final integrated Oracle reviews are CLEAR for V1j only. The reviewer independently confirmed the separate qualified/unmet outcomes, temporary-source removal,253 stable inputs, unchanged coverage/binary/library/feed/consumer identities and corrected historical labels.

Observed QueryCache invocation overlap is not proof of linearizability, all schedules, version-rescan race execution, compilation uniqueness, delegate-initialization race safety, concurrent clearing/eviction, mutable-policy semantics, tenant isolation or relational/provider/connection thread safety. MRU concurrency remains unqualified. Complete roadmap, broader cache obligations and mandatory coverage targets remain open.
