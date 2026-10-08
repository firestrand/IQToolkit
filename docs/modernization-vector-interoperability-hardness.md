---
title: Vector abstraction source discovery hardness
version: 0.1
last_updated: 2026-10-05
status: Source verification; compilation and runtime unverified
owner: IQToolkit project owner
tags: [modernization, vectors, discovery, verification]
---

# ATLAS hardness report: vector abstraction source discovery

The [interoperability probe](modernization-vector-interoperability-probe.md) maps the roadmap's retrieval requirements against three actual published packages and pinned implementation source. It identifies reuse opportunities and semantic gaps before a competing public API is designed. Compilation, engine behavior, retrieval quality and provider support remain unverified.

## Edge cases inspected

VectorData10.10.0, CommunityToolkit PgVector1.0.1 and Qdrant1.0.0 archives match their catalog SHA512 values. Nuspec commits bind source; public XML and DLL payloads are retained. Eighty-eight selected Git blobs match the acquired trees. Current stable package identities are separated from deprecated SK1.74.0-preview predecessors and the alpha Milvus memory-store release.

Inspected differences include key/vector subsets, positive schema dimensions versus absent explicit search-value checks, missing per-query exactness/projection/score metadata, bounded expression-filter translation, hybrid fusion/threshold behavior, native client escape paths, ownership, streaming/buffering and trimming annotations. PgVector search propagates cancellation; its collection-deletion command omits the token. These are source findings, not runtime tests or universal absence claims.

## Tool and service failure handling

The initial source selector used package IDs instead of CommunityToolkit's MEVD/src/PgVector and Qdrant paths, so its passed acquisition receipt covers abstraction source and connector root metadata only. That original receipt remains unchanged. An explicit supplement acquired35 actual connector implementation files, with Git identities checked. The validator requires both actual implementation subtrees.

Three auxiliary acquisition commands exited1. The first stopped on an HTTP expectation before preserving the unsuccessful response; retained earlier bodies are available, but its exact original status/cause is unqualified. A diagnostic repeat mistakenly expected404 from a PgVector registration that returned200; its actual body is retained. The third treated an annotated Git tag as a direct commit and rejected it. The tag object remains retained; following its actual object URL and peeling it resolved the release commit. Distinct evidence-driven corrections did not rewrite failures or run an SDK. The Milvus release source is explicitly weaker than a nuspec package/source binding.

## Concurrency, load and security

Root owns all writes; independent readiness/source agents are read-only. No SDK, driver/client construction, service, schema, embeddings or provider data is executed. Actual acquisitions use HTTPS, bounded responses and inactive source/archive/payload snapshots. Source paths are validated against traversal; downloaded sources and DLLs are inspected as data. No credentials or application records are acquired.

The read-only validator confines reads to the owned area, enforces input/archive limits and uses explicit checks that remain active under optimized Python. Passive import creates no bytecode or files. Reading the actual external /usr/bin/python3.12 path is rejected before its contents are read. Local stdlib tooling is not a distributed Python package or service. Process load, engine throughput, memory scaling and platform behavior are untested.

## Verification commands and results

From repository root, `python3 artifacts/vector-interoperability/verify-sources.py` exits0: three archives, selected payloads,88 Git blobs and retained ecosystem metadata authenticate. Repeated execution remains read-only. `python3 -O artifacts/vector-interoperability/verify-sources.py` also exits0 with validation active. Passive-import and real path-boundary checks pass; retained originals remain unchanged.

Source/controller validation does not establish a resolved dependency graph or current vulnerability audit. Those require a future actual cold restore and reviewed matching locked compile profile. No Csc, SARIF, runtime or coverage result is claimed by this packet.

## Remaining work

Source/API comparison is complete for this bounded scope. Independent review must inspect the exact saved documents/evidence; its actual outcome is recorded separately. The next compile experiment needs a separately expanded plan and independent planning/source/command reviews. Real engines, corpus, tenant/security policy, workload, public contract and platform support remain owner/data gates. The source recommendation is an optional integration with existing abstractions, not adopted architecture.

MariaDB compatibility remains required. Earlier MP0/AP0/V1aj/V1ai native results and stopped budgets, finalprofile0.110, full coverage targets and the broader M0/M1/M4/roadmap gates remain unchanged. This source result does not authorize staging, release or publication.
