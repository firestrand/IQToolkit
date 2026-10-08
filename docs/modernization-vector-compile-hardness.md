---
title: Vector abstraction private compile hardness
version: 0.2
last_updated: 2026-10-05
status: Scoped compiler verification; observed Oracle CLEAR
owner: IQToolkit project owner
tags: [modernization, vectors, interoperability, compile-probe, verification]
---

# ATLAS hardness report: vector abstraction API compilation

VectorData 10.10.0, CommunityToolkit PgVector 1.0.1 and Qdrant 1.0.0 resolve together and compile the private caller bindings alongside the unchanged IQToolkit assembly. The strict rebuild has zero warnings, errors or SARIF results. This qualifies a finite compiler/API combination, not an IQToolkit vector provider or runtime/server interoperability. The [probe contract](modernization-vector-compile-probe.md) defines the experiment.

## Edge cases and measured findings

Seven exact direct CPM pins resolve to 13 nodes. The complete profile preserves the source-inspected abstraction/connectors, compatible stable transport versions and the adopted DI/logging servicing line. Every actual archive matches the reviewed candidate SHA256, SHA512 and size; separately verified lock/assets/cache content hashes remain a distinct identity domain. The strict profile targets net10.0 with SDK 10.0.401, runtime/reference pack 10.0.12, C# 14, Release, nullable annotations, SDK analyzers and copied repository editor settings on Linux ARM64. Pruning is disabled and dependency audit is enabled in all/high mode. No RID, apphost, self-contained, trimming, AOT or publication operation is involved.

Fourteen uninvoked methods bind common and concrete collections; float-memory and generic search; get, upsert and delete; hybrid search; filter, record and nullable-score properties; native client access; and IQToolkit.QueryProvider-to-IQueryProvider coexistence. Every argument comes from a hypothetical caller. No client, options, record or vector is constructed; no method is invoked or result enumerated. The IQToolkit DLL is copied unchanged and referenced by the compiler without rebuilding or invoking it.

Authentic Csc arguments include all three selected net10.0 package references and the sealed toolkit DLL. Actual SARIF has one run with no results. The DLL/PDB hashes, CodeView GUID/stamp/age, normalized PDB checksum and four SHA256 source-document bindings authenticate the produced pair and its actual reviewed/generated source copies. The metadata reader loads no assembly. Signature compilation does not qualify dimension/nonfinite validation, exactness, score direction, tenant policy, cancellation, streaming or retrieval quality; those source-discovered gaps remain open.

## Tool and service failure handling

The first independent source and planning reviews requested seven control/source and two planning corrections. Root added complete authenticated empty-file capture, current source/planning/graph acceptance identities, restored graph/archive/extracted-asset authentication before compiler, metadata-only PE/PDB qualification, exec supervision, truthful never-started retirement, cold-parent inventory and strict storage-error rejection. Fresh reviewers closed every finding before SDK dispatch; the exact superseded documents and original reports remain retained.

Proportional local checks authenticate a previously retained Milvus compiler pair and reject authentic cross-paired symbols and unrelated source bytes. Two unchanged extracted controller branches observe genuine log-open and process-creation failures, preserve their primary/secondary errors without invented PIDs or exits, and retire their copied source bytes inactive. The first local check had an incorrect exception expectation: exclusive creation returned FileExistsError rather than IsADirectoryError. The failed check and corrected expectation are preserved. No SDK process was created by these checks.

Actual NuGet extraction lowercases nuspec filenames and omits archive-only packaging XML. Ninety-one delivered members in four authentic prior packages verified this policy before the new restore; the new graph and compile authenticate all 165 applicable delivered payloads before any build. Logging generator/build and DI compatibility inputs remain hash-bound. Neither native attempt failed or needed a retry; cancellation and forced containment were not exercised at runtime.

## Concurrency and load

Root owns all writes, SDK operations and index. Independent reviewers inspect read-only in separate tasks. The collaboration interface reached its thread limit; actual separate Codex tasks supplied independent review, with shared filesystem and explicit no-edit boundaries rather than a tool-enforced sandbox.

The graph and compile use distinct cold parents. Their native exits are 0/0 and 0/0/0. All five command sessions stop without live descendants or forced termination. Both original parents are absent after exact per-file inactive retirement: 422 and 406 files. Preparation/terminal checks authenticate 2,532 and 2,976 input/snapshot pairs. All 2,515 accepted source inputs, raw Git index and 336 ordinary binaries remain unchanged.

Two of three reservations use 12.120857 cumulative seconds of the 900-second budget. Every command is below 120 seconds. Terminal preservation measures 0.328548/0.476087 seconds; retirement stays below 30 seconds. Capture count/bytes and per-input/log limits pass. Final owned storage is 528,794,464 bytes, below 2 GiB. These measurements concern bounded tooling, not application concurrency, throughput or retrieval latency. The unused reservation grants no further SDK dispatch after closure.

## Security and data exposure

The private profile clears inherited feeds, explicitly maps all 13 IDs to the sole approved public NuGet source and uses fresh package/HTTP caches. Current vulnerability responses were retained; no restore diagnostic was reported under the all/high audit policy. This is time-bound audit evidence, not a permanent vulnerability-freedom or production-license guarantee.

Commands are literal argv arrays with a minimal environment. Initial inventories, symlink rejection, current acceptance hashes and actual owned storage guard dispatch. Original graph receipts and complete retired caches stay sealed throughout compilation. No credentials, application records, generated provider payloads, embedding models or paid services are consumed. Production project dependencies, public APIs and binaries remain unchanged by this probe.

## Verification commands and results

From the repository root, `python3 artifacts/vector-compile-discovery/inspect-sources.py` and its `-O` form pass for 13 archive candidates, seven direct pins, 30 retained metadata responses and 14 passive methods. New tooling imports passively; assertion-based controllers deliberately reject optimized execution before I/O. Real external file/directory and missing-acceptance denials pass.

The independently reviewed historical dispatches were `python3 artifacts/vector-compile-discovery/run-prepared.py graphs` and then `python3 artifacts/vector-compile-discovery/run-prepared.py compile`. The latter performs a distinct cold locked restore and forced no-restore Rebuild after authentic graph qualification. These commands are settled receipts, not instructions to rerun them. Actual compiler results and cleanup pass; independent observed compile review is unconditional CLEAR with no requests.

| Evidence under `artifacts/vector-compile-discovery/` | SHA256 |
| :--- | :--- |
| `graphs.er2tja6h/result.json` | `023dccac172106e7c1fd40d1606888cee473a877801ce6dd203b2d7353d1f3c1` |
| `compile.3chky56i/result.json` | `a4054f2d1b270ce002dfeae5ca8cbf5bdd59a93b2a66f9e5135204a76cb37c02` |
| `compile.3chky56i/compiler-captures.json` | `98430f2c10d1ed81708b3329a561891303c74da15c94e267f8fa7467efc87f55` |
| `compile.3chky56i/parent-retirement.json` | `83af948966793bb62a7106b0064f74d78dabaef7ee48cb41ba11deb33230e134` |
| `planning-oracle-round-2.json` | `812d66ee12cb72a0745beae9a2ef2993741acb7335c291a784b6333cb84a8f51` |
| `source-oracle-round-2.json` | `88bfda9eca96135d0621b138490c47202b142c8e319a6a30d67f142ed3a7c305` |
| `observed-graph-oracle.json` | `8cd9b81426a2b50c71face6cb46718f21a27e6dbdd0ec5fc01c2c8bc903cf569` |
| `observed-compile-oracle.json` | `80281bf53f62b92312e2e00f53df5eec849fd5402d075226d97034a81584b8c2` |

Pre-execution acceptance binds exact plan 0.120/probe 0.2 and manifest `71c297a5fec941d0dc5cfacd8bd2a7574da24029476d5e8c904ad4d6697f95c7`. Both prepared snapshot sets preserve this tested epoch. Later documentation updates require their own review; they do not rebind native evidence or retroactively compile a changed checkout. The selected .NET draft and scoped Python/planning/Markdown guides govern; whole-project compliance is unclaimed. Ignored artifacts are local evidence and are not available from a fresh checkout without their producers.

## Documentation review corrections

The first independent post-execution reviews requested two documentation corrections: the manifest locator must distinguish `review-inputs-round-2.json` from the separate three-document epoch directory, and a register of this size needs a separate Evidence Index under Guide2.7. Probe0.4 fixes the locator; plan0.122 adopts the [planning index](modernization-evidence-index.md) without changing any historical fact, native binding, result or profile. Both original reports and exact outcome0.121 document snapshots remain retained. Fresh review must confirm these proposed closures; no SDK rerun follows.

## Remaining work

Pass: authentic dependency graph, cold locked strict compiler/API coexistence, symbol/source metadata and containment/preservation. Fail: the original local check harness expectation, retained as failed; no native attempt failed. Independent observed compile review is CLEAR. Integrated/current-document planning reviews remain required for this subsequent documentation revision.

Real retrieval requires separately accepted workload/public/mapping/security/engine/data/corpus/platform contracts and actual execution. Existing source findings do not establish a bridge, quality metric, tenant isolation or engine certification. Required MariaDB compatibility discovery, full M0/M1/M4/roadmap, coverage and unanswered owner decisions remain open. Earlier stopped V1aj/V1ai/AP0/finalprofile 0.110/BLOCKED and all native caps remain unchanged. No staging, commit, push, tag or release follows from this scoped result.
