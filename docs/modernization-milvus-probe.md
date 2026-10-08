---
title: Milvus client feasibility probe
version: 0.4
last_updated: 2026-10-05
status: Source and private compile verified; server compatibility unverified
owner: IQToolkit project owner
tags: [modernization, milvus, dotnet, feasibility]
---

# Milvus client feasibility probe

This required `LOCAL-RM-DISCOVERY` probe asks whether the published C# client provides the capabilities needed for a future retrieval adapter and whether its public API can compile under an isolated strict .NET 10 dependency profile. Source inspection and private strict compilation pass. Server interoperability and retrieval quality are unverified. No IQToolkit provider or support promise follows from this document.

## Acquired versions and provenance

Acquisition on 2026-10-04 retained 97 original HTTPS source/metadata responses. SHA-256, length, package archive SHA-512 and selected Git blob checks bind the observations in `artifacts/milvus-discovery/source-inspection.json`. The ignored area retains original bodies, normalized metadata, source inspection code and failures. Repeatable read-only validation is `python3 artifacts/milvus-discovery/verify-sources.py` from the repository root; the original acquisition/inspection producer is retained and is not rerun against its existing result. These are local review artifacts, not published documentation assets.

| Component | Exact inspected identity | Evidence and interpretation |
| :--- | :--- | :--- |
| Published C# package | `Milvus.Client 2.3.0-preview.1`, published 2024-03-20 | [NuGet package](https://www.nuget.org/packages/Milvus.Client/2.3.0-preview.1); official index contains six prerelease versions and no stable version at acquisition. |
| Package source | `2ccee694092952ca86c2648e9653863951ea8dc5` | Nuspec repository commit matches the [SDK tag source](https://github.com/milvus-io/milvus-sdk-csharp/tree/2ccee694092952ca86c2648e9653863951ea8dc5); 63 acquired files match Git blob identities. |
| Package targets | `net462`, `net6.0`, `netstandard2.0` | Actual archive contains DLL/XML pairs for these targets. The private .NET 10 compile consumed its `net6.0` assembly. |
| Embedded client protocol | `9ed8fb405005c2a2cdb10d30276d8bec9334b023` | SDK proto submodule is pinned to this commit; five proto files acquired. |
| Declared SDK CI server | `v2.3.10` | Pinned Build.yml names this server. CI execution and compatibility are not observed. |
| Current comparison server | Milvus `v3.0.2`, commit `3c4448a2aee506ccd14851e742aba85f1c83188a` | [Official release](https://github.com/milvus-io/milvus/releases/tag/v3.0.2), published 2026-09-20. Its SDK pairing table does not include C#. |
| Current server protocol | `9e4f0ebc92af3ecf9e13c12350c6fce13d0893fa` | Server go.mod pins `go-api/v3 v3.0.2`; official Go module origin and GitHub tag `go-api/v3.0.2` identify this commit. |

The selected archive is 994,842 bytes; its downloaded SHA-512 equals the NuGet catalog hash. This checks archive identity, not publisher authenticity. Pinned source LICENSE is Apache-2.0; catalog license fields are absent. Legal disposition and ownership for production adoption remain future gates. Mutable SDK main declares 2.4.0 separately; it is not the source/API of this published package.

## Source capability findings

| Area | What pinned public source establishes | What still needs evidence or a decision |
| :--- | :--- | :--- |
| Vector search | `SearchAsync<T>` accepts a vector field, vector list, metric, limit, parameters and cancellation token. Its implementation supports only float and byte vectors. | Dimensions, invalid input, metric availability, exact/approximate behavior and performance require real server evidence. Generic syntax does not promise other element types. |
| Filters and projection | Search parameters forward expression, output fields and partition names; query accepts expression plus projection/partition/limit/offset parameters. | Filter grammar, filter-before-ranking semantics, tenant isolation and pagination correctness require a reviewed contract and server results. |
| Results | Search exposes IDs, scores, fields, query count and per-query limits. | Score direction, normalization, ordering, tie behavior and retrieval quality remain unverified. |
| Cancellation | Public calls forward the caller token through `_callOptions.WithCancellationToken`. | Server cancellation acknowledgement, resource cleanup and recovery are not measured. |
| Lifecycle | Client supports disposal; ownership determines whether it disposes its gRPC channel. | Shared-client/channel ownership, deadline policy and production dependency injection require design. |
| New protocol capabilities | Compared protocol declarations include hybrid search and current sparse/int8/BM25 schema elements. | Pinned public C# source has no `HybridSearchAsync` or sparse-vector surface. Transport-generated messages do not establish public adapter support. |

Service-qualified comparison finds 84 client and 149 current-server RPC declarations, with 66 added pairs and one removed pair (`MilvusService.SearchV2`). `RegisterLink` occurs in multiple services; comparing names alone loses that distinction. This is an API gap indicator, not a wire compatibility test or a count of features IQToolkit needs.

## Private compile contract

The observed experiment compiled a library containing forwarding methods only. Methods accept caller-supplied `MilvusCollection`, vectors, field/expression names, parameters, metrics, limits and tokens; they bind float search, binary search, query and `GetVersionAsync`, plus existing parameter/result properties. No method executes, client is constructed, endpoint is contacted, collection/schema is created, or vector/record fixture is generated. This therefore does not consume pending generated-data authorization.

Use SDK `10.0.401`, target `net10.0`, Release, portable framework-dependent IL, no RID, no app host, no trimming/AOT, `RestoreEnablePackagePruning=false`, CI properties, nullable analysis, recommended analyzers and warnings as errors. Compile-only checks are verification of existing APIs; they need no fabricated RED assertion. No IQToolkit source/reference or production dependency changes are permitted.

Private Central Package Management pins exactly these five direct dependencies:

| Package | Exact version | Profile rationale |
| :--- | :--- | :--- |
| Milvus.Client | `[2.3.0-preview.1]` | Published archive/source being evaluated. |
| Google.Protobuf | `[3.36.2]` | Current stable 3.x metadata candidate at acquisition; deliberate upgrade from declared minimum 3.25.3. |
| Grpc.Net.Client | `[2.84.0]` | Current stable 2.x metadata candidate; deliberate upgrade from minimum 2.61.0. |
| Microsoft.Extensions.Logging.Abstractions | `[10.0.12]` | Align with installed .NET patch; deliberate upgrade from minimum 8.0.0. |
| System.Text.Json | `[10.0.12]` | Align with installed .NET patch; deliberate upgrade from minimum 8.0.2. |

Package pruning is deliberately disabled so this experiment consumes all five declared references, including libraries also available through the target framework. The evaluated setting remained false during generation, cold locked restore and no-restore rebuild; actual graph and command reviews verify it. This is a named private discovery profile, not a production pruning policy.

This evaluates a deliberately modernized five-direct-dependency profile, not an untouched minimum graph or sole-package profile. Catalog vulnerability fields are empty for these four upgrades. Actual cold restore also passed with NuGet audit enabled, mode all and level high; warnings were errors. The eight-node graph and archive/license/asset identities were independently reviewed. This compile success cannot establish runtime compatibility of the older client with those dependencies.

Isolate all configuration, source, obj/bin, SDK home, temporary files, HTTP cache and package cache under a uniquely owned private parent. Disable inherited build props/targets, clear feeds and use the sole public NuGet feed with explicit package mappings (including reviewed transitive IDs). Deliberately generate this new profile lock once outside locked mode; save actual graph/assets/audit output, then retire the preparation parent. An independent graph review must approve that exact graph before a new cold locked restore and forced `Rebuild --no-restore` under the same profile. Never regenerate a lock in response to a failed locked restore.

**Acceptance:** SDK identity matches; graph generation and actual all-dependency high/critical vulnerability audit pass; cold locked restore preserves lock identity; forced build produces actual Csc invocation, compiler SARIF, library/PDB and zero warning/error records. The invocation must bind the reviewed forwarding source, correct Milvus reference asset and strict flags. Audit/network/setup/analyzer/compiler/timeout/preservation failure remains failed and unqualified; no suppression, audit weakening, substitution or mock server is allowed. Only successfully observed source/compiler capabilities may be marked verified.

**Bounds:** Each SDK command at most 120 seconds; cumulative native work across preparation, compile and repairs at most 900 seconds; whole owned area plus live private parent at most 2 GiB; each input/log at most 16 MiB; snapshot admission at most 6,144 files / 640 MiB. Terminal preservation and retirement each at most 30 seconds. Three evidence-driven native executable attempts total for MP0; preparation and compile each consume one. Original failures are retained and never promoted. Earlier AP0 and V1aj budgets remain exhausted.

Root alone owns project/doc/controller writes, SDK, integration and index. Source/version agents and Oracle are read-only. Freeze source/controller preparation before review, exact allocation/commands/copies before dispatch, and actual graph before compile. Retain native exits, logs, source/config/locks/assets/archive/reference/compiler identities and process-session settlement. Source/index and all 336 ordinary DLL/PDB identities must stay unchanged during native dispatch. After sessions terminate, retain the owned parent inactive with exact rename/hash mapping and verify its original path absent. No staging or full-phase closeout while the broader gate remains open.

## Observed verification on 2026-10-05

The separately reviewed graph packet `graphs.axrlwp1q` completed SDK identity and cold graph-generation restore with exits 0/0. It retained five direct dependencies and eight total nodes. The additional nodes are Grpc.Net.Common 2.84.0, Grpc.Core.Api 2.84.0 and Microsoft.Extensions.DependencyInjection.Abstractions 10.0.12. Actual evaluated audit properties are enabled/all/high; strict restore emitted no warnings or errors. Archive identity and signature-excluded lock content hashes were checked separately.

After independent graph review, `compile.bghh6sdk` used a fresh private parent and cold cache. SDK identity, locked restore and forced no-restore rebuild exited 0/0/0. The lock was unchanged; all eight downloaded archives matched the graph packet. Actual Csc invocation referenced the published `lib/net6.0/Milvus.Client.dll` with warnings as errors. Native SARIF contained zero diagnostics, and build output reported zero warnings/errors. The passive caller DLL/PDB and all four PDB source checksums were independently verified. No caller method or client API executed.

These two attempts used 7.419300 seconds of the cumulative 900-second budget. Each command stayed below 120 seconds, terminal preservation and retirement below 30 seconds, and owned storage below 2 GiB. Both parents are absent and all owned sessions settled; 492 graph and 501 compile input/snapshot pairs were verified. Checkout sources, index and all 336 ordinary DLL/PDB identities remained unchanged. Two of three native reservations were used; this completed probe authorizes no additional SDK execution.

Independent operational Oracle reviews cleared the actual graph and compile results with no outstanding requests. Pre-execution planning PASS binds plan0.115/probe0.3; those exact documents are preserved under `artifacts/milvus-discovery/compiled-document-epoch-0.115/`. This outcome update does not relabel native receipts as verification of current documentation or certify full M0/final readiness. The [Milvus ATLAS report](modernization-milvus-hardness.md) records failures, bounds, retained identities and remaining work. Ignored local receipts are available in this workspace; published documentation does not depend on their being distributed.

## Local verification-tool scope

The Python Code Standards revision 2026-09-29 also governs the new local stdlib verification scripts. Its SHA-256 is `05269f75a22ab7a66712bdfb0aeb628f3b4f22e57fe2981f815f9f72f89445b5`; source is `/home/firestrand/Dropbox/Projects/Software-Standards/Python Code Standards.md`. The observed local interpreter is `/usr/bin/python3.12`, Python 3.12.3; exact executable identity is retained in `artifacts/milvus-discovery/python-interpreter.json`. This is a single-host command-tool profile, with no Python library/service/distribution or broader interpreter compatibility promise.

Apply correctness, bounded I/O and concurrency, pathlib, subprocess argument arrays, input/path/size validation, TLS, cause-preserving terminal handling, cleanup and actual verification rules. Broad catches are restricted to deliberate supervisor/retirement boundaries; failures and shutdown must remain visible. New scripts have guarded entry points, reject optimized assertion-disabled execution, and import without launching processes or writing bytecode. Actual passive-import and repeated source-verification checks are retained. Native controllers additionally verify the recorded interpreter before SDK dispatch.

Operational review must assess these rules as well as the .NET profile: budgets include every successful or failed native reservation, originals remain sealed, subprocess sessions settle, and failure/publication cannot leave success flags or active parents. Correctness-critical controller failure paths need evidence proportionate to this local supervisor, without fabricated database inputs. Existing reviewed AP0 mechanics are supporting evidence only; they do not automatically qualify changed scripts. Exact packet and observed reviews remain mandatory after the new source review.

These are ignored, non-distributed, stdlib-only local command tools alongside an existing .NET project. Python package/application metadata, uv dependency migration, wheels, Python service telemetry and a Python CI platform matrix are not introduced. The guide permits simpler layouts for small scripts when packaging adds no value and warns against automatic unrelated toolchain migrations. This scoped applicability is not an exception to security, data integrity or failure verification, and it does not claim whole-project Python compliance. There are no new Python dependencies or performance hot-path changes.

## Decision gates and GOTCHA spec

**Goal:** Resolve published C# capability and strict modern-profile compile feasibility for required Milvus discovery. **Objectives:** Verify package/source/API provenance; distinguish available public operations from current-server gaps; obtain an actual strict compile result. **Trigger:** Required discovery has no qualified C# probe. **Tasks:** Source inspection → independent planning/source review → exact private preparation review → graph generation/review → cold locked compile → preservation, ATLAS and observed review. **Termination:** Accepted scoped evidence or truthful retained failure at the attempt cap.

**Capabilities:** Official primary sources and installed .NET SDK; compile-only local library. **Health:** Fixed budgets above, strict warnings/audit, source/index/binary fences. **Attributes:** Every capability claim names its evidence tier; no service/fixture/public API/support inference. **Constraints:** Linux ARM64 host, portable compile only; no production architecture, package adoption, quality or latency certification. **Users:** Maintainers assessing a future retrieval adapter. **Runtime:** Read-only external source acquisition and serial private SDK commands; persisted evidence lives in the owned ignored area. **Beliefs and intent:** The client is older than the comparison server and has visible capability gaps; measure compile feasibility without converting that inference into an unsupported compatibility claim.

Real server interoperability requires a separately expanded packet, explicitly selected server/client pair, approved endpoint/deployment/cost and authentic schema/data/corpus. Quality, tenant isolation, score semantics, cancellation/recovery and cleanup require actual engine evidence. Production M4 still depends on M1 and accepted retrieval/workload/public/platform decisions. REST, another SDK, maintained fork or deferral are future choices after those gates, not changes authorized by this probe.

## Standards coverage for this scope

| Authority / rule | Stage and applicability | Evidence or gate |
| :--- | :--- | :--- |
| ENG-STD-DOTNET-XPLAT 1.1.0-draft.1, DOTNET-GOV-005 | Legacy scoped adoption | Owner-selected exact standard; no whole-project compliance claim. |
| DOTNET-DEP-001 / DEP-002 | Private CPM and profile resolution | Five exact pins; deliberate graph generation followed by reviewed cold locked restore and same-profile no-restore rebuild. |
| DOTNET-DEP-003 / DEP-004 | Feed/cache trust and vulnerability evaluation | Cleared sole feed, explicit mappings, private cold cache, retained hashes and actual audit; no publisher inference from content hash. |
| DOTNET-API-003 | Prerelease/compatibility scope | Exact prerelease identified; no production package version or public signature change. |
| Build/Test/Packaging/Deployment section; DOTNET-TEST-001 | Compiler verification only | Actual strict forced compile/SARIF; no test/runtime/publish/coverage improvement claim. Existing project tests and coverage remain unchanged, with broader targets still open. |
| Development Plan Creation Guide 2.7, Task Writing Standards / Data Fidelity Policy | Discovery and task sequencing | MP0 separates acquisition, graph verification and compile verification; official source inputs only, no generated fixture. |
| Autonomous Development Plan Guide, failure/resumption/dispatch clauses | Bounded independent work | Separate root-owned packet, frozen inputs, exact command reviews, preserved failures, caps and safe inactive disposition. |
| Python Code Standards, 2026-09-29, Operating Principles / I/O / Error Handling / process boundaries / Testing / Security / Packaging and Project Boundaries / Exceptions and Adoption | Local tool scope | Recorded Python3.12.3 and executable identity; guarded passive imports and read-only source verification now; independently reviewed bounded/native failure and cleanup gates before qualifying SDK results. No distributed Python package/service or dependency migration. |
| Technical Markdown Style Guide; agent-tooling.md; AGENTS_TEMPLATES.md | Documentation, routing and reporting | Source-linked findings, fallback official sources, pre-execution GOTCHA and post-execution ATLAS. |

Selected technical standard SHA-256: `8c56930ec1cd26e8b58a273d47cb0b9713f55e6d4961b6d29a636ffaf4f0ebe2`. Planning acceptance and operational source/native review are distinct; neither closes full M0 or remaining owner decisions.
