---
title: Native nullable-column acquisition hardness report
version: 0.1
last_updated: 2026-10-04
status: Observed scoped checkpoint
owner: IQToolkit project owner
tags: [modernization, sqlite, null-semantics, acquisition, atlas]
---

# Hardness report: native nullable-column acquisition

Scope: roadmap M0, tasks V1ah.0–0c. The owner-selected [.NET standard](standards/source/Cross-Platform-DotNet-Engineering-Standard-1.1.0-draft.1.md), especially DOTNET-TEST-001, and Development Plan Creation Guide 2.7 govern this checkpoint.

Two independent native SQLite connections returned the same existing metadata row with a null default. C# string inequality selected that row; ordinary SQL `<>` selected none; the explicit compensation candidate selected it on both connections. This qualifies actual input acquisition and ordinary driver loading. No IQToolkit query, regression test or production repair was executed here.

## Edge cases observed

The helper registers the existing engine virtual table through `SELECT * FROM pragma_compile_options LIMIT 0`, then acquires `name`, `dflt_value` and hidden `arg` through `pragma_table_xinfo`. No table, row or null value is authored. Both connections return the genuine values `compile_options`, native `DBNull`, and `pragma_compile_options`.

The null column's actual schema is BLOB / `System.Byte[]`; its `GetValues` cell is `System.DBNull`. The receipt preserves that distinction. Only the independently computed C# observation decodes DBNull to null. It does not establish IQToolkit's mapping or materialization behavior for this column.

All 12 native requests complete. Unfiltered and hidden-argument multisets match, both engine identities match, both bare-result multisets match, and both compensation-result multisets match the derived CLR selection. All eight required comparisons pass; both recorded bare-vs-CLR equality flags are false. Native schemas, buffers, types, null flags, SQL and parameters are captured before validation. [Raw and final observations](../artifacts/nullable-column/runtime.pgku5bzh/observations.json) are byte-identical; the [summary](../artifacts/nullable-column/runtime.pgku5bzh/acquisition-summary.json) retains counts and scoped flags.

## Tool and service failure handling

The first strict compiler run failed with CS8602 in the helper's parameter type capture. Its SDK/restore/build exits remain `0/0/1`. Root inserted one null-conditional operator in that capture, retaining the actual parameter value and original source. No diagnostic was suppressed. This consumes artifact repair 1 of 3; the [original failed result](../artifacts/nullable-column/compile.vssd7e5v/result.json) is unchanged and is not behavioral RED.

The original 49-file environment was retired by an authenticated same-filesystem move with inactive filename mappings. The corrected source was compiled in a fresh parent. After acquisition, its shared compiler/native environment was separately retired in 0.2473 seconds, preserving 64 files / 3,832,740 bytes. Both original parents are absent; source, project and PE files are retained with inactive names. [Successful retirement](../artifacts/nullable-column/runtime.pgku5bzh/parent-retirement.json) authenticates both compiler and native sessions, input seals and checkout preservation. No SDK replay was used for disposition.

Each process has a 120-second deadline; each packet has a 900-second deadline, 16 MiB log/capture bounds and a 2 GiB newly owned storage limit. Native rows are bounded to 1,024 per query and commands to 10 seconds. Storage scans allow three attempts with 20 ms pauses. Acquisition rejection preserves incremental evidence and exits nonzero; forced termination, survivors or incomplete preservation cannot qualify success.

## Concurrency and load considerations

The two native connections are distinct, independent and nonpooled. Requests run sequentially and both connections are disposed before success publication. The private build and native process are serialized by root. No throughput, shared-connection concurrency, multi-tenant isolation or load claim is made.

## Security and data exposure

Inputs are installed engine metadata and existing driver assets, with no credentials or application records. The helper has four direct assembly references, no package/project references, empty feeds and disabled root build imports. It uses ordinary loading without a resolver or fallback.

Runtime provenance verifies the four actual managed assembly paths, versions and hashes, plus the actual mapped native library path and hash. The observed environment is Linux Arm64 / .NET 10.0.12 / SQLite 3.53.3. This is a local acquisition profile, not a cross-platform or provider certification. Owned private directories, source seals, process settlement and bounded retention protect the checkout; no publication is inferred.

## Verification commands and results

Each packet retains literal argv, environment, input seals, native logs and exits. Compiler and native dispatch received separate prospective and observed Oracle reviews.

| Check | Observed result | Evidence |
| :--- | :--- | :--- |
| SDK version, isolated restore and strict Release build | Fresh exits `0/0/0`; SDK 10.0.401; zero warnings/errors; empty external restore graph; exact driver copies and DLL/PDB identities | [Compiler review](../artifacts/nullable-column/compile.iz77c9k6/observed-oracle.json) |
| Compiled helper, sealed input manifest and owned output path | Exit 0; 12 completed native requests; eight positive comparisons; CLR 1 / bare SQL 0 / compensation 1 on both connections | [Acquisition review](../artifacts/nullable-column/runtime.pgku5bzh/observed-oracle.json) |
| Checkout and evidence preservation | Compiler 727 / native 802 input seals, complete source/index and all 336 ordinary binaries unchanged during execution and disposition | [Native result](../artifacts/nullable-column/runtime.pgku5bzh/result.json), [retirement](../artifacts/nullable-column/runtime.pgku5bzh/parent-retirement.json) |

No ordinary solution rebuild, test recollection or repack was necessary for this isolated helper. The existing canonical-publication checkpoint and its strict/API/package/coverage evidence remain preserved; no broader verification result is inferred from this acquisition.

## Follow-ups and limitations

Independent integrated review is recorded separately in [integrated-oracle.json](../artifacts/nullable-column/integrated-oracle.json). A finite toolkit test contract must establish the actual C# expression, explicit CLR mapping/null materialization and native binding before a conditional repair. The acquired SQL mismatch alone cannot establish an IQToolkit defect or generalized null policy.

M0 and the full roadmap remain open. Current final classification, durable canonical coverage transition, checked consumer-lock/feed refresh, broad coverage floors, other providers/platforms and all unanswered owner/data/workload/public-contract decisions remain required.
