---
title: Isolated SQLite package consumer hardness report
version: 0.2
last_updated: 2026-10-02
status: Scoped verification; phase closure open
owner: IQToolkit project owner
tags: [modernization, verification, atlas, packaging]
---

# ATLAS hardness report: isolated SQLite package execution

This report preserves the original consumer checkpoints and post-run restore incident. The later final combined batch/eager feed, explicit lock refresh and SQLite execution are recorded separately in the [eager ownership report](modernization-eager-ownership-hardness.md); historical damaged copies remain retained and are not relabeled as new evidence.

V1p in the [implementation plan](modernization-implementation-plan.md) verifies a consumer with a sole direct exact dependency on `IQToolkit.Data.SQLite` 1.1.2-preview.1. Declared package dependencies supply core, Data, the real driver and native assets. It uses no project references or repository DLL copies. The [consumer](../eng/PackageConsumer/Program.cs) executes native version/options metadata through public `ICreateExecutor`, `QueryExecutor`, `QueryCommand` and `FieldReader` APIs and compares complete unchanged results to same-connection direct SQL controls.

## Boundaries and provenance tested

The [verification runner](../eng/verify-consumer.sh) copies the consumer and SDK pin into a freshly created outside-repository directory with a cold package cache. Source mapping routes toolkit IDs exclusively to the tested local feed and external dependencies to nuget.org. It hashes original inputs before copying, checks every copied source/package against that snapshot, and rechecks original inputs after verification. Locked restore must leave the reviewed lock unchanged. Strict build/run failures remain failures, and success is published after the owned environment moves into ignored evidence storage.

The runtime independently verifies all three resolved toolkit package SHA-512 hashes against the tested feed, restored cache and assets; `.nupkg.metadata` must identify that local source. Core/Data/SQLite and driver assemblies must load from the consumer's own build output, and their locations/SHA-256 identities are logged. Native rows and source metadata are printed unchanged; no entity objects, business records or expected version strings are constructed.

Historical `artifacts/package-consumer/runs/run.Jedh2VMD` passes restore, strict build and runtime verification against **`artifacts/api-compatibility/runs/run.TfMqnXD2/packages`**. The actual SQLite version is 3.53.3, with source ID `2026-06-26 20:14:12 d4c0e51e4aeb96955b99185ab9cde75c339e2c29c3f3f12428d364a10d782c62`. Both version and all 51 native compile options match direct SQL. SDK/runtime are 10.0.401/10.0.12 on Linux ARM64, with existing driver versions. Original/copy/input identities, lock/assets, native results, package source/hash records and the complete owned environment are retained. Recorded runtime locations describe the original temporary path; `retained-environment.txt` identifies its subsequent location.

The protected current runner passes at `artifacts/package-consumer/runs/run.oRR63aqS` against the post-mapping API feed `run.dqoq1XYU/packages`. Explicit preparation `lock-preparation-runs/run.Q49Xw8wC` and the reviewed lock SHA-256 `a9ea9d05eac1c4bf8f009af111469349dce2e9d55f03f872d10f1ca0713ca079` bind those exact archives. Only the three toolkit content hashes changed; all versions, dependency edges and external hashes are unchanged (`artifacts/implicit-mapping/consumer-lock-review.json`). Locked restore, strict build and the same native version/all51options controls pass. Retained projects now use snapshots as described below. Final independent integrated evidence/report review is CLEAR.

## Lock preparation and tooling corrections

The [separate preparation command](../eng/prepare-consumer-lock.sh) deliberately resolves a new lock from the selected exact archive bytes before locked verification. It uses a fresh outside cache, validates copied inputs and original stability, and retains the actual lock/assets. It is never an automatic retry after a locked failure. [NuGet source mapping](https://learn.microsoft.com/en-us/nuget/consume-packages/package-source-mapping) does not check pre-existing global-cache packages; the cold cache is therefore part of provenance, not a performance setting. [Locked restore](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-restore) enforces the resulting dependency/hash contract.

Archive inspection found all six packages in two fresh API runs had different archive hashes but identical extracted member bytes and changed ZIP timestamps. Evidence is retained at `artifacts/package-consumer/archive-identity-inspection.json`. A lock cannot bind a different archive merely because its DLL contents are equal. Earlier preparation `artifacts/package-consumer/lock-preparation-runs/run.kVpjZtc8` binds historical feed run.TfMqnXD2. Inspection confirms only the three toolkit content hashes changed from the earlier lock; every package version, dependency edge and external hash is unchanged. `final-lock-inspection.json` retains the reviewed SHA-256 `09ff15af1b29cfb478cbc8e9f16eb96fb6d5c554ef210fba02c54ea134ebd294` of that historical lock.

Oracle found a source-order defect in the initial runner: copying occurred before initial hashes, potentially disconnecting the snapshot from executed bytes. The corrected runner captures before copying and verifies copies; its source review is provisional CLEAR. The earlier successful `run.0V9S2bBo` is historical and does not validate the corrected runner. Historical `run.Jedh2VMD` is the evidence for that correction. No fabricated concurrent mutation test or provider response was introduced.

## Failure, concurrency and security limits

### Post-run evidence mutation and preservation

After the reported run, unsolicited restores of project copies inside this repository overwrote the source lock and several top-level retained locks with empty version2 graphs. Failed restore artifacts identify repository CPM inheritance and `NU1008`; the responsible tool is unverified. The actual outside-run environment locks, original assets, input manifests and native results survive unchanged. Therefore the original successful execution remains supported, but the earlier statement that all retained lock copies still agree is no longer valid.

Root preserved the observed damaged bytes, hashes, timestamps and failed source restore artifacts under `artifacts/package-consumer/post-run-restore-inspection`, then restored the task-owned source lock from the SHA-256-bound original preparation environment. Historical damaged copies remain preserved, rather than silently repaired. `project-renames.json` records byte-identical renaming of nine retained projects to `.csproj.snapshot`.

Evidence maintenance V1p.2 uses a non-discoverable `.csproj.template` source and `.csproj.snapshot` evidence. The runners reconstruct the exact project bytes only outside the repository, verify the filename/hash correspondence, and rename runnable projects before relocating environments into ignored artifacts. These mechanics preserve the consumer and locked-restore semantics. Source review is provisional CLEAR; the protected runner has now passed against the post-mapping package feed above. No failure-injection or hosted-platform qualification is inferred.

There is no simulated service/error response, generated fixture or compatibility suppression. Preparation/restore/build/runtime failures are visible; toolkit source/hash/result mismatches fail instead of becoming skips. Missing native metadata fails a real data prerequisite. The actual successful run does not characterize all filesystem, source-mapping rejection, unavailable-feed or cleanup failures.

Root serialized package and consumer operations. The owned temporary environments move into ignored artifacts after execution, preserving review evidence and releasing the temporary paths. No concurrent invocation or hosted Windows/macOS path qualification is claimed. Only public native metadata and dependency provenance are recorded; no credentials or business data are present.

## Verification commands and results

| Check | Result |
| :--- | :--- |
| `bash eng/prepare-consumer-lock.sh <run.dqoq1XYU>/packages` | Current run.Q49Xw8wC: actual resolved lock/assets retained; copy/input identities stable; only toolkit content hashes change in reviewed dependency delta. |
| `bash eng/verify-consumer.sh <same-run>/packages` | Current run.oRR63aqS: unchanged locked cold restore, zero-warning strict build, native runtime controls and protected snapshot retention pass. |
| Integrated strict/API/package checks | Final strict run.0gNSTn47: 39 passes/325 SQL Server skips, 247 translations; six API comparisons in run.dqoq1XYU and six library/symbol inspections pass. See the [mapping report](modernization-mapping-hardness.md). |
| Shell syntax and document/source inspection | Both scripts pass bash syntax; source/flow/evidence maintenance reviews are provisional CLEAR. Final current independent integrated evidence/report review is CLEAR. |

## Remaining work

The historical Oracle review independently confirmed copied/original identities, retained locks, package/source/cache SHA-512 hashes, exact toolkit DLL bytes and native result controls at review time. The later mutation and surviving evidence are qualified above. This review does not approve fixture or coverage exceptions.

This establishes SQLite **executor** package runtime and its declared dependencies on this host. It does not establish LINQ translation in an external consumer, SQL Server/MySQL/Access runtime, other native OS/architectures, trimming/AOT, full coverage, publication or the original generated consumer case. Six independent package restores/public-symbol compilations now pass separately, as recorded in the [independent package report](modernization-independent-packages-hardness.md). The remaining runtime/quality obligations and full roadmap remain open. The [README](../README.md) documents explicit preparation/review/locked verification for each selected package snapshot.
