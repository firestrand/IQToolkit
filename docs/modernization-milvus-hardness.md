---
title: Milvus client feasibility hardness
version: 0.1
last_updated: 2026-10-05
status: Scoped source and compile verification; observed Oracle CLEAR
owner: IQToolkit project owner
tags: [modernization, milvus, discovery, verification]
---

# ATLAS hardness report: Milvus client feasibility

The published Milvus.Client 2.3.0-preview.1 public API compiles under the reviewed private .NET 10 profile with zero warnings or errors. This resolves the source/compiler portion of required Milvus discovery. It establishes no server interoperability, runtime dependency compatibility, retrieval quality or IQToolkit provider support. The [probe contract](modernization-milvus-probe.md) defines the profile and source findings.

## Edge cases and measured findings

Official package metadata, the downloaded archive, pinned SDK source and both protocol revisions are authenticated against retained hashes. Sixty-three acquired SDK files match Git blob identities. Six DLL/XML payloads across three package targets are retained. The private compiler selects the package's net6.0 assembly while targeting net10.0 with SDK10.0.401, runtime/reference pack10.0.12, C#14, Release and CI strict properties on Linux ARM64. No RID, trimming, AOT or publication is involved.

Five direct CPM pins resolve to eight nodes; pruning is explicitly disabled throughout. This is a deliberate modern dependency experiment, not the package's untouched minimum graph. Actual cold restore audit is enabled/all/high and warnings are errors. The compile uses an unchanged reviewed lock and independently downloaded matching archives. Signed archive SHA512 and signature-excluded lock/assets/cache content hashes are separate verified identities; neither establishes publisher authenticity or production adoption.

The passive library binds caller-supplied float/binary search, query, version, parameters and results. Native Csc invocation, empty SARIF, retained assembly and portable PDB corroborate compilation. All four PDB source checksums match retained compiler inputs. No method executes, client is constructed, endpoint is contacted or schema/vector/record fixture is generated. Unsupported vector element types, dimensions, filters, scores, tenant isolation, deadlines, cancellation and recovery remain untested at runtime.

Pinned public source lacks HybridSearchAsync and a sparse-vector surface. Service-qualified protocol comparison finds 84 old and149 comparison-server declarations, with66 additions and one removal. Milvus3.0.2's published SDK pairing table omits C#. These are source compatibility risks, not wire-test failures or proof of incompatibility.

## Tool and service failure handling

The initial current-protocol tag lookup returned an empty result because the guessed tag path was wrong; the original response remains retained. Actual module origin and the correct go-api/v3.0.2 tag resolved provenance. Two inspection failures are also retained: counting bare RPC names lost service identity, and guessed implementation literals differed from pinned source. Corrected inspection and a separate repeatable read-only validator pass; original report-producing code is not rerun against an exclusively created result.

Operational source review required separate archive/content-hash checks, cumulative reservation accounting and clearing success flags when preservation fails. Python review also required actual optimized-mode and interpreter mismatch controls. These were corrected and independently reviewed before SDK dispatch. Four command tools import passively and reject assertion-disabled execution; interpreter guards are checked against the actual3.12.3 host. Historical acquisition and failure scripts remain immutable. No native attempt failed and no native retry was needed.

## Concurrency and load

Root exclusively owns SDK, writes and index; agents inspect read-only. Each packet uses a distinct cold private parent for project, outputs, SDK home, temporary files and caches. Both owned sessions settle and parents retire with byte-identical inactive source/project/runtime mappings. The original paths are absent. Graph preservation verifies492 input/snapshot pairs and313 retired files; compile verifies501 pairs and357 retired files. All336 ordinary DLL/PDB hashes and the index remain unchanged.

Two of three MP0 native reservations were used, totaling7.419300 seconds against900 seconds. Each command is below120 seconds. Actual terminal preservation takes0.100512/0.110880 seconds; retirement remains below30 seconds. Snapshot capture is below6144 files/640MiB and each input/log below16MiB. Final owned area at native retirement is103,026,637 bytes, below2GiB. This is bounded process evidence, not application load or throughput measurement. Completion does not authorize use of the remaining native reservation for broader work.

## Security and data exposure

The private profile clears inherited feeds and uses explicit mappings for all eight IDs on the sole public NuGet source. Fresh caches and retained catalog/archive/payload checks establish source selection and content identity; license ownership and production adoption remain undecided. Process launches use argument arrays and a minimal environment. No credentials, application records, generated provider inputs or paid service calls are involved. Production projects, dependencies and public APIs are unchanged by MP0.

## Verification commands and results

From the repository root, `python3 artifacts/milvus-discovery/verify-sources.py` repeatedly exits0 and authenticates97 acquired source/metadata responses, source Git blobs, archive/XML and protocol comparison. The ignored artifacts are local review evidence, not distributed tools or documentation assets.

The separately reviewed prepared graph command `python3 artifacts/milvus-discovery/run-prepared.py graphs` executed SDK identity0 / graph restore0. After actual graph review, `python3 artifacts/milvus-discovery/run-prepared.py compile` executed SDK identity0 / cold locked restore0 / forced no-restore rebuild0. These are historical dispatched commands, not invitations to rerun settled packets. Actual build output reports zero warnings/errors and SARIF has zero results. Independent observed graph and compile Oracle reports are unconditional CLEAR with empty requests.

| Evidence under `artifacts/milvus-discovery/` | SHA256 |
| :--- | :--- |
| `graphs.axrlwp1q/result.json` | `3b60218bb49fe9d1d52c97728d383a54b55fb10aa552275e5d7ea85ba96c776f` |
| `compile.bghh6sdk/result.json` | `1e4f44da1e6233239fbbce262dbbbb6ab78beac897b2a55a812a362d1cca7deb` |
| `compile.bghh6sdk/compiler-captures.json` | `b2aa50179e4d475b3fc491ab12cfdfdcc74d90ab082591dab5d8e31afb7af592` |
| `compile.bghh6sdk/parent-retirement.json` | `2bc4b8b4674c9adee41ecacd47d2674914f509d73568d5cae683635a82745abc` |
| `observed-graph-oracle.json` | `53dc9f5492cde002b95fdcea78ff1a36bc5d281a79974cb0ff4004f1130df43d` |
| `observed-compile-oracle.json` | `fab1034066712e2385cf42b4dfb1e94b5935a0a48ec66d121a4f0b8ea94c87f0` |

Pre-execution planning PASS binds plan0.115/probe0.3 and the complete496-input manifest. Those exact documents remain retained in compiled-document-epoch-0.115; observed native evidence is not rebound to this subsequent documentation update. The selected technical standard is ENG-STD-DOTNET-XPLAT1.1.0-draft.1; Python Code Standards revision2026-09-29 governs the new local stdlib supervisors in their documented scope. Whole-project compliance is unclaimed.

## Remaining work

MP0.1–3 pass in source/compiler scope. Real server/client interoperability requires a separately expanded packet with an owner-selected pair, endpoint/deployment/cost authorization and authentic schema/data/corpus. Quality, score semantics, filtering/isolation, cancellation/recovery, runtime dependency compatibility and platform support remain unverified. Production M4 still depends on M1 and accepted workload/public contracts. No backend, fork, alternate SDK or public package commitment is selected here.

MariaDB compatibility and vector abstraction interoperability remain required discovery. AP0 and V1aj/V1ai stopped budgets, finalprofile0.110 metadata, broader coverage and all unanswered owner/data/public/provider decisions are unchanged. The full roadmap and M0 remain incomplete; no staging, publication or release follows from this scoped result.
