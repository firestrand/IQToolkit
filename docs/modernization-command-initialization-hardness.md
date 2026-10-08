---
title: Command initialization failure ownership hardness report
version: 0.1
last_updated: 2026-10-04
status: Scoped closure; integrated Oracle CLEAR
owner: IQToolkit project owner
tags: [modernization, ownership, atlas]
---

# Hardness report: command initialization failure ownership

V1aa in the [implementation plan](modernization-implementation-plan.md) repairs command ownership when initialization fails before `GetCommand` returns. The newly created command remains owned through command text, transaction assignment and virtual parameter initialization. The catch disposes it and uses bare rethrow; successful return transfers ownership as before. The production change is confined to `DbEntityProvider.Executor.GetCommand`.

The owner-selected [engineering standard 1.1.0-draft.1](standards/source/Cross-Platform-DotNet-Engineering-Standard-1.1.0-draft.1.md), development plan guides and AGENTS_TEMPLATES ATLAS format govern this checkpoint. This report records observed Linux ARM64 evidence, not full standards compliance or M0 completion.

## Edge cases tested

Actual SQLite `GetValues` supplies unchanged one- and two-column arrays from engine metadata, with native TEXT/String schema and copied counts one and two. A separate ordinary connection provides complete one- and two-parameter result oracles. Tests reacquire the values on each execution; saved observations are not future fixtures.

The one-column array deliberately underfills a configured two-parameter query. This is invalid caller configuration, not a valid query, injected driver failure or new public exception contract. Transparent observers call the real implementation, record native disposal events and capture the actual exception and first parameter. They do not inject errors or dispose commands. The connection observer records disposal immediately before calling its real `Close` implementation.

| Case | Observed RED | Observed GREEN |
| :--- | :--- | :--- |
| Borrowed-open initialization failure | No command disposal before caller teardown; connection Open. | One disposal before teardown; connection Open. |
| Initially closed initialization failure | No disposal before Close; one after Close; connection Closed. | One disposal before Close and caller teardown; connection Closed. |
| Complete two-column arguments | Complete independent oracle equality; one reader, one disposal, connection Open. | Same successful behavior. |
| Matching one-column configuration | Complete independent oracle equality; one reader, one disposal, connection Open. | Same successful behavior. |
| Native acquisition | Pass. | Pass. |

Both failure cases preserve the original observed `IndexOutOfRangeException` object, exactly one initialized native parameter and zero reader attempts. The owned RED shows why checking only after connection closure would hide this leak. Exception identity is qualified where disposal succeeds. Throwing custom disposal and command-text or transaction setter failures were not separately exercised, although the source catch encloses those setters.

## Tool/service failure handling

`artifacts/command-initialization/red.bg8dit7i/result.json` remains FAILED: the original classifier anticipated legacy MSTest assertion text. The separate read-only inspection authenticates the retained native TRX and exact installed assertion syntax, preserving three passing controls and two genuine disposal failures without rerunning SDK tests. `green.gl2ik3j8` then passes the identical five-method test source. Six Test compilation outputs are byte-identical across RED to GREEN; ten Data DLL/PDB paths change. The full strict rebuild subsequently propagates the repaired pair to six remaining Data copy paths.

The first API controller stops before any SDK command because of an incorrect evidence-directory assumption. Its failed receipt is retained. The corrected API run passes. Package and symbol checks complete in `feed.3su6_6a8`, but its consumer command exceeds the 512 MiB storage bound and is forcibly contained. Its wrapper incorrectly writes `passed` despite unfinished consumers; that status is unqualified. The separate consumer-only run has a prospectively reviewed 1 GiB bound, completes all six profiles and SQLite runtime execution, and records no forced termination or live descendants. Repairing the reusable wrapper's interruption status remains required follow-up work.

Coverage attempt `coverage.xvfwuqf6` fails preparation because the required output directory was missing; no collection runs. Corrected `coverage.gw5awfx_` completes collection, both preservation checks and descriptor generation, then fails comparison because the qualifier anticipated four added leaves. Actual coverage includes the catch header and method epilogue as well as disposal and rethrow. A separately compiled private qualifier accepts exactly eight covered class/method entries at lines 640, 642, 643 and 645. It compares the retained report without another collection run. Both original failed receipts and raw reports remain unchanged.

SDK leaves have 120-second deadlines, parents 480 seconds, and logs a 16 MiB bound. Collection retains a separately bounded 30-second terminal-preservation fallback. Storage uses complete scans: 512 MiB except the reviewed consumer-only 1 GiB packet. Successful supervisors record stable checked inputs, source and index, with no cancellation, forced/live members or cleanup errors. These observations do not certify unexecuted timeout/cancellation paths. Private environment files are hash-verified before removal of their original parents; retained generated projects have inactive names.

## Concurrency / load considerations

Evidence covers sequential native operations and eager execution. Concurrent provider sharing, asynchronous execution, cancellation, disposal races and performance under load remain unqualified.

## Security (injection, data exposure)

Requests use actual SQLite engine metadata in private in-memory connections and native parameters. No constructed entity rows, padded arguments, credentials, user tables or external database service are introduced. Consumer restoration uses reviewed package feeds and graphs. This is scoped evidence, not general injection or security certification.

## Verification commands and results

The sealed packets retain literal argv, source/controller hashes, native logs, TRX and authentic before/after fences.

| Check | Actual retained evidence |
| :--- | :--- |
| Locked Release restore, strict build and filtered `NativeCommandInitializationTests` | `red.bg8dit7i`: three passes/two disposal failures; separate semantic qualification. `green.gl2ik3j8`: five passes. Builds have zero warnings/errors. |
| `GITHUB_ACTIONS=true bash eng/verify.sh` | `artifacts/verification/runs/run.vPBONFzm`: 99 passes, 325 explicit unconfigured SQL Server skips, 247 translations, zero warnings/errors; all 419 inherited definitions/outcomes preserved plus five added methods. |
| `bash eng/verify-api.sh <retained-1.1.1-packages>` | `artifacts/api-compatibility/runs/run.UGPPa1Zm`: six API comparisons; all six DLL and six PDB archive payloads match current outputs. |
| `dotnet msbuild eng/VerifyPackages.proj -p:PackageDirectory=<fresh-feed>` | Native exit zero in the original feed packet; subsequent consumer failure does not qualify the whole packet. |
| `bash eng/verify-symbols.sh <fresh-feed>` | `artifacts/package-symbols/runs/run.OjyJJVia`: native exit zero and passed provenance checks. |
| `bash eng/verify-ci-consumers.sh <fresh-feed>` | `consumers.zxiuf3o5` / `artifacts/ci-consumers/runs/run.7rDXEmBb`: six distinct public-symbol compilations, seven private lock reviews, complete SQLite native executor comparison; 43.123 seconds and 900,927,455 terminal owned bytes under the revised bound. |
| Unchanged managed collector against copied current outputs | `coverage.gw5awfx_/collection/descriptor.json`: 99 passes/325 skips, byte-equivalent report copies and both actual preservation checks; original comparison FAILED and preserved. |
| Separately compiled retained-report qualifier | `coverage-qualification._c6y3y6f`: native exit zero, zero build warnings/errors, 97 sources, 16,724 leaves, all 16,716 mapped inherited leaves/conditions preserved without regression and exactly eight covered additions. |

Coverage is 2,738/8,353 lines (32.77864%) and 1,460/5,320 branches (27.44361%), compared with the qualified V1z 2,732/8,349 lines and 1,460/5,320 branches. The exact source edit maps unchanged old statements to new lines; no file or method is excluded. The other 96 measured sources and three unaffected library pairs remain unchanged. The added Test PDB document contains exactly five public test methods; all 419 inherited strict definitions/outcomes persist.

The private qualifier authenticates five historical Data/Test/source aliases, nine permanent chain pins and 386 historical input snapshots, preserving original-to-V1y-to-V1z evidence. Its 782 sealed dispatch inputs and all 336 current ordinary binaries remain stable. Its native verifier separately retains 587 authenticated inputs. Current six library pairs and the Test pair are preserved in `artifacts/command-initialization/compiled-current.json` for later qualification.

## Summary

The scoped GetCommand repair and fresh protecting checks pass. Source-changing coverage is qualified against actual retained native reports. Observed and integrated protecting, documentation and ATLAS Oracle reviews are CLEAR for this scoped closure. The retained verdict is artifacts/command-initialization/integrated-oracle.json.

Broader RESOURCE.OWNERSHIP, unwaived coverage targets, full provider/platform coverage, optimized cache qualification, current durable final-profile expansion and outstanding owner decisions remain required. The earlier V0.6d final assessment is historical and does not certify this changed source. The interrupted consumer wrapper's misleading status also remains open. No publication, full M0 or full roadmap completion follows from this checkpoint.
