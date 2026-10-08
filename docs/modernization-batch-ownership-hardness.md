---
title: Batch command ownership hardness report
version: 0.1
last_updated: 2026-10-02
status: Scoped implementation; phase closure open
owner: IQToolkit project owner
tags: [modernization, verification, atlas]
---

# ATLAS hardness report: batch command ownership

V1b in the [implementation plan](modernization-implementation-plan.md) adds iterator-scoped `using` declarations to both private `DbEntityProvider.Executor.ExecuteBatch` methods. Commands now dispose on exhaustion and active enumeration disposal. Existing preparation, command reuse, parameter binding, generic reader cleanup, results, connection lease and buffering behavior remain unchanged. The selected ENG-STD-DOTNET-XPLAT 1.1.0-draft.1 governs this scoped work; M0 and the full roadmap remain open.

## Edge cases tested

Seven [native tests](../IQToolkitTest/RealProvider/BatchCommandOwnershipTests.cs) cover a native command-disposal observer control, borrowed-open streamed nonquery/generic full and early cases, and initially-closed buffered nonquery/generic controls. Early generic enumeration confirms the reader is open at the yield before explicit disposal. Resource assertions precede connection teardown. Initially-closed execution buffers before returning; those controls establish completed cleanup and preserved values, rather than early native enumeration.

Retained `artifacts/batch-command/runs/red.uM00qdDV` records four intended absent-command-disposal failures and three passing controls. `green.rCIeO5Ti` passes all seven after only the two declarations change. `artifacts/batch-command/pair-inspection.json` records identical test bytes and native observations in both runs. The focused independent Oracle verdict is CLEAR.

## Real-source provenance and failure handling

Tests acquire actual `PRAGMA compile_options` schema and all 51 native rows through `GetValues`, using each reader's actual `FieldCount`. The first two unchanged returned option buffers become parameters. An independent ordinary native connection obtains actual nonquery counts and matching reader values from the same parameterized native SQL. No expected count, native row, error or provider response is invented. Transparent observers return the exact base-created command and reader, without cleaning up on the toolkit's behalf.

Source/version/driver provenance is retained with the test runs. Missing metadata or ambiguous source results fail acquisition. The default batch size remains 50. Empty inputs, other sizes, acquisition/iteration/disposal failures, transactions and cancellation remain unqualified; normal-path green tests do not certify those paths.

## Concurrency, load and security

Root serialized SDK, packaging and instrumentation operations. These tests do not measure concurrent batches, contention, throughput or allocations. Only native library metadata is captured; no tenant/business data, credentials or new runtime dependencies were introduced. Broader sensitive-parameter logging and transaction ownership remain separate adoption work.

## Verification commands and results

The joint final batch/eager verification is recorded in the [eager ownership report](modernization-eager-ownership-hardness.md). It binds strict tests, API and package comparisons, isolated consumers and measured coverage to the final combined source. Separate focused RED/GREEN evidence above remains the batch regression proof.

No generated-fixture authorization or coverage waiver is assumed. This report qualifies the observed normal/active-disposal behavior only; it does not close the phase or the roadmap.
