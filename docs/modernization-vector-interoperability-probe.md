---
title: Vector abstraction interoperability discovery
version: 0.1
last_updated: 2026-10-05
status: Source inspected; compilation and engine behavior unverified
owner: IQToolkit project owner
tags: [modernization, vectors, interoperability, discovery]
---

# Vector abstraction interoperability discovery

This required `LOCAL-RM-DISCOVERY` investigation evaluates `Microsoft.Extensions.VectorData` before IQToolkit invents overlapping retrieval contracts. Published abstractions and existing PgVector/Qdrant implementations already represent typed collection, CRUD and vector search operations. They leave concrete gaps in the roadmap's query controls, validation, score metadata and tenant guarantees. This is source/API discovery, not a supported adapter, a runtime interoperability result or an adopted public contract.

## Exact package and source identities

Acquisition on 2026-10-05 retained three actual package archives, catalogs, nuspecs, public XML documentation and 88 Git-blob-verified source/metadata files. Downloaded archive SHA512 values match their NuGet catalogs. This establishes content identity, not publisher authenticity. Sources are bound to package nuspec commits rather than mutable repository branches. The ignored local evidence is under `artifacts/vector-interoperability/`; it is not a distributed dependency or documentation asset.

| Package | Version, publication and license | Package source commit |
| :--- | :--- | :--- |
| [Microsoft.Extensions.VectorData.Abstractions](https://www.nuget.org/packages/Microsoft.Extensions.VectorData.Abstractions/10.10.0) | Stable 10.10.0; 2026-09-09; MIT | dotnet/extensions `02107c65bab30aad9e35b5133ed643eaa77bccd8` |
| [CommunityToolkit.VectorData.PgVector](https://www.nuget.org/packages/CommunityToolkit.VectorData.PgVector/1.0.1) | Stable 1.0.1; 2026-08-31; MIT | CommunityToolkit/AI `126f5ae78aef91192d161088737e3c00acc3a23c` |
| [CommunityToolkit.VectorData.Qdrant](https://www.nuget.org/packages/CommunityToolkit.VectorData.Qdrant/1.0.0) | Stable 1.0.0; 2026-07-22; MIT | CommunityToolkit/AI `433fda5e2a6f6d96260057e531bff396c320205e` |

All three actual archives provide net10.0, net8.0, net462 and netstandard2.0 assets. The net10.0 PgVector dependency minima include VectorData10.8.2, AI abstractions10.8.3, DI abstractions10.0.10, Npgsql10.0.3 and Pgvector0.3.2. Qdrant minima include VectorData/AI abstractions10.7.0, DI abstractions10.0.9 and Qdrant.Client1.18.1. These are declared ranges, not an observed resolved graph. Combining them with abstraction10.10.0 needs a separately reviewed graph and compile experiment; no production dependency has changed.

Old Semantic Kernel PgVector/Qdrant1.74.0-preview catalog entries explicitly record deprecation and replacement with the stable CommunityToolkit packages. Their old preview status does not describe these replacements. [PgVector deprecation](https://www.nuget.org/packages/Microsoft.SemanticKernel.Connectors.PgVector/1.74.0-preview), [Qdrant deprecation](https://www.nuget.org/packages/Microsoft.SemanticKernel.Connectors.Qdrant/1.74.0-preview).

## Roadmap capability mapping

`Represented` means the inspected API or dispatch source contains the stated shape. `Requires extension` means the roadmap asks for a contract beyond that common surface. `Unverified` means source cannot prove the required behavior. None of these rows certifies a real engine.

| Roadmap requirement | Source finding | Disposition and remaining evidence |
| :--- | :--- | :--- |
| Typed get/upsert/delete and top-k | VectorStoreCollection has typed CRUD and SearchAsync; both connectors implement them. | Represented. Conflict/partial failure, consistency and real records still need backend acceptance. |
| Collection/key identities | Collection definitions and typed keys exist. PgVector accepts short/int/long/string/Guid; Qdrant accepts ulong/Guid. | Represented with incompatible key subsets. A portable key policy needs an explicit decision; conversion must not silently change identity. |
| Vector dimensions and element types | Definitions require positive dimensions. PgVector supports float, Half on modern targets, binary and sparse representations; Qdrant model supports float representations and multiple vectors through named mode. | Represented configuration, backend dependent. Actual extension/server/version/type combinations remain unverified. |
| Dimension and nonfinite input validation | Examined connector search conversion paths do not explicitly check vector length, NaN or infinity. | Requires extension for a shared pre-dispatch guarantee. Neither schema configuration nor generic TInput syntax proves value validation. |
| Index and metric configuration | Definitions carry IndexKind/DistanceFunction. PgVector defaults to Flat and has Hnsw creation limits; Qdrant supports Hnsw creation. | Represented schema controls. Index provisioning and supported distance semantics need engine evidence. |
| Per-query exact/approximate controls | Common VectorSearchOptions has no selector; Qdrant native SearchParams exists but this collection path does not pass it. | Requires extension or a declared exclusion. Index kind alone is not a per-query exactness contract. |
| Metadata filter translation | Common options accept an expression filter. PgVector builds server WHERE before nearest-result ordering/limit; Qdrant passes translated filter to its server query. Unsupported expression shapes throw. | Represented bounded translation. Arbitrary LINQ is unsupported; actual engine preselection, null/type behavior and failures remain unverified. |
| Tenant isolation and policy bypass | Filter arguments exist, but no required IQToolkit tenant policy or protected bypass boundary is established. | Unverified security behavior. Common typing and source placement cannot certify authorization; require accepted policy plus real-engine adversarial evidence. |
| Metadata projection | Common options expose IncludeVectors, not an arbitrary field projection selector. | Requires extension or documented limitation. PgVector nearest-match column selection uses model properties. |
| Scores, rank and metric direction | Results expose Record and nullable Score. PgVector converts cosine/dot-product scores; Qdrant returns native scores. Metric/direction/rank are not result fields. | Requires extension for the roadmap's metadata promises. Do not normalize unrelated metrics or equate scores across engines without a contract. |
| Threshold and hybrid retrieval | Common options carry ScoreThreshold; both connectors expose keyword/vector hybrid search with fusion scores. PgVector applies threshold outside its bounded nearest-match query. | Represented with different score/result-count implications. Threshold placement, RRF interpretation and text relevance need separate engine/quality evidence. |
| Embedding model/version metadata | Application-supplied vectors are accepted; embedding generators are optional. No common model/version field is found. | Requires an application metadata contract. IQToolkit must not make embedding generation/model hosting mandatory. |
| Cancellation | Search forwards tokens into backend work. PgVector collection deletion accepts a token but calls ExecuteNonQueryAsync without it. | Represented for inspected search dispatch; universal lifecycle cancellation is unsupported by this evidence. Active cancellation/cleanup/recovery remain unverified. |
| Streaming, disposal and client ownership | Search returns IAsyncEnumerable. Both connectors have explicit supplied-resource ownership choices and reference-counted owned resources; disposal is synchronous at the abstraction boundary. | Represented source shape. PgVector reader iteration differs from Qdrant's awaited query response followed by result enumeration; neither implies identical buffering or async disposal. Runtime early-break/error/concurrency behavior remains unverified. |
| Native extension boundary | GetService can expose NpgsqlDataSource or QdrantClient plus metadata/self; non-null service keys return null in inspected implementations. | Represented. Keep native types at optional package boundaries and capabilities explicit. |
| Trimming/NativeAOT | Typed GetCollection overloads carry RequiresDynamicCode/RequiresUnreferencedCode; dictionary alternatives are documented. | Unverified runtime support. Do not infer AOT compatibility from ordinary API availability or dictionary syntax. |

The mapping is grounded in [abstraction collection](https://github.com/dotnet/extensions/blob/02107c65bab30aad9e35b5133ed643eaa77bccd8/src/Libraries/Microsoft.Extensions.VectorData.Abstractions/VectorStoreCollection.cs), [search options](https://github.com/dotnet/extensions/blob/02107c65bab30aad9e35b5133ed643eaa77bccd8/src/Libraries/Microsoft.Extensions.VectorData.Abstractions/VectorSearchOptions.cs), [result](https://github.com/dotnet/extensions/blob/02107c65bab30aad9e35b5133ed643eaa77bccd8/src/Libraries/Microsoft.Extensions.VectorData.Abstractions/VectorSearchResult.cs), [PgVector model](https://github.com/CommunityToolkit/AI/blob/126f5ae78aef91192d161088737e3c00acc3a23c/MEVD/src/PgVector/PostgresModelBuilder.cs), [SQL builder](https://github.com/CommunityToolkit/AI/blob/126f5ae78aef91192d161088737e3c00acc3a23c/MEVD/src/PgVector/PostgresSqlBuilder.cs), [PgVector collection](https://github.com/CommunityToolkit/AI/blob/126f5ae78aef91192d161088737e3c00acc3a23c/MEVD/src/PgVector/PostgresCollection.cs), [Qdrant model](https://github.com/CommunityToolkit/AI/blob/433fda5e2a6f6d96260057e531bff396c320205e/MEVD/src/Qdrant/QdrantModelBuilder.cs) and [Qdrant collection](https://github.com/CommunityToolkit/AI/blob/433fda5e2a6f6d96260057e531bff396c320205e/MEVD/src/Qdrant/QdrantCollection.cs). Exact local snapshots and origins support review of each row.

## Milvus and search backend boundaries

The inspected Semantic Kernel Milvus1.74.0-alpha release source implements IMemoryStore with MemoryRecord semantics, not VectorStoreCollection. Its nearest-match surface is not an existing generic VectorData adapter. The release tag is annotated; its peeled commit is `14ea2fcabb02910e6fccfc939d217567caa4a0be`. This is a release-source observation, not authenticated package/source commit equivalence. [Milvus memory store source](https://github.com/microsoft/semantic-kernel/blob/14ea2fcabb02910e6fccfc939d217567caa4a0be/dotnet/src/VectorData/Milvus/MilvusMemoryStore.cs).

OpenSearch remains its own text/search capability. Its [official .NET clients](https://docs.opensearch.org/latest/clients/dot-net/) provide transport and query APIs; that documentation does not establish a VectorData adapter or Elasticsearch interoperability. No Milvus/OpenSearch adapter is adopted or globally ruled out by this probe. The separate [Milvus feasibility result](modernization-milvus-probe.md) remains source/compile only.

## Recommendation and decision gates

Evaluate an optional VectorData integration package that reuses these collection/search abstractions and existing connectors. Keep IQToolkit relational LINQ intact. A bridge is worthwhile only if accepted workloads need IQToolkit mapping, capability diagnostics, tenant policy, observability or explicit result metadata. A transport wrapper without such value does not justify another provider.

Before adopting that recommendation, the project owner must select the workload and public contract: direct connector use versus an optional IQToolkit bridge; portable key/vector subsets; exactness/projection/score/model metadata extensions; security policy and bypass behavior; and supported engine/platform versions. These are existing M4 workload/public-policy gates, not decisions this source probe can settle. No new public type, backend commitment or package is implemented.

A next private compile packet may bind caller-supplied generic collections, search inputs, filters/options, tokens, records and result types against the exact abstraction and both connectors. It must not construct clients, enumerate results, execute CRUD/search or generate vectors/records. Including the unchanged IQToolkit assembly would establish coexistence only, not a working mapping bridge. Exact CPM pins, complete transitive/source/license graph, controlled cold caches, matching locked restore/rebuild profile, literal bounded command arrays, protected source/index/binaries, compiler/PDB evidence and cleanup require separate plan expansion and independent planning/source/packet reviews before any SDK dispatch. No such packet ran here.

## GOTCHA scope and verification

**Goal:** Resolve required source/API interoperability before duplicate vector contracts. **Objectives:** Authenticate published abstraction/two connectors, map all retrieval requirements and document remaining gates. **Tasks:** Read-only phase/source discovery, actual source/catalog/archive acquisition, source validation, matrix/plan/state/ATLAS and independent review. **Trigger:** Required interoperability discovery lacks evidence. **Termination:** Reviewed source findings with compilation/runtime explicitly unverified.

**Capabilities:** Official HTTPS sources and local read-only validation. **Off-limits:** SDK/build/restore, services, schema/data/vector fixtures, embeddings, production dependencies/public APIs, provider promotion and previous stopped packet retries. **Health:** Each acquisition stage at most240 seconds, HTTPS request30 seconds, response/input16MiB, at most180 selected Git blobs per stage, archive expanded64MiB/128 entries, whole owned area256MiB; three evidence-driven repairs per distinct failure. **Attributes:** Preserve originals and classify source/compile/runtime separately. **Constraints:** No performance/quality/compatibility certification. **Users:** Maintainers defining retrieval contracts. **Runtime:** Root owns writes, shared docs/index; independent agents are read-only, no recursive delegation. **Beliefs and intent:** Existing abstractions reduce duplication but cannot supply all promised semantics; use evidence to expand the next gate.

From repository root, `python3 artifacts/vector-interoperability/verify-sources.py` authenticates three archives, retained payloads and88 Git blobs. Repeated validation passes; optimized Python execution retains explicit validation rather than assertion-dependent checks. Passive import writes nothing and a real out-of-area file is rejected before reading. The [ATLAS report](modernization-vector-interoperability-hardness.md) records acquisition limitations and outcomes. Python3.12.3 local stdlib tooling uses Python Code Standards revision2026-09-29 under the same non-distributed script scope as MP0; no Python project/uv/package/CI migration is introduced.

## Standards coverage and limits

| Authority | Application to this source-only scope |
| :--- | :--- |
| ENG-STD-DOTNET-XPLAT1.1.0-draft.1, DOTNET-GOV-005 | Owner-selected legacy adoption scope; no full compliance claim. |
| DOTNET-DEP-001–004 | Actual declared package/source/license identities recorded; CPM, locked profile, source mapping, cache provenance and live audit are mandatory future compile gates, not claimed executed. |
| DOTNET-API-001–003 | No public/package version changes; stable versus deprecated/prerelease identities distinguished; future adoption needs reviewed compatibility policy. |
| DOTNET-TEST-001 and runtime/platform clauses | Source findings only; no invented engine data, runtime, coverage, performance or supported-platform claim. |
| Development Plan Creation Guide2.7, DP-05/06/07/09/12 | Official source inputs, true optional package boundaries, requirement mapping, owner gates and independent review. No Tier1 product fact activated. |
| Autonomous guide, failure/ownership/resumption clauses | Bounded root-owned discovery; immutable failures, no stopped-budget reset, source/index/336 binaries protected. |
| Python Code Standards2026-09-29, I/O/security/error handling/testing/project boundaries | Stdlib read-only validator uses owned paths, explicit checks, bounded archive/response handling and actual import/path validation. No distributed Python application. |
| Markdown style, agent-tooling and AGENTS_TEMPLATES | Source links, versioned metadata, unavailable Graphify/Context7 fallback, GOTCHA and ATLAS. |

Technical standard SHA256 remains `8c56930ec1cd26e8b58a273d47cb0b9713f55e6d4961b6d29a636ffaf4f0ebe2`. Full M0, M1/M4 implementation, MariaDB compatibility, engine/quality/corpus/public/workload/platform decisions and coverage/final-readiness gates remain open.
