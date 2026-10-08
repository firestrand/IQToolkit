---
title: Proposed modernization fact and acceptance inventory
version: 0.1
last_updated: 2026-10-05
status: Proposed; no fact activated or implementation certified
owner: IQToolkit project owner
tags: [modernization, requirements, facts, planning]
---

# Proposed modernization fact and acceptance inventory

This is the complete future obligation register for the rolling-wave [implementation plan](modernization-implementation-plan.md), derived from roadmap0.3. It supplements the34 historical rows without changing their IDs, claims, lifecycle or evidence. The native34-fact/95-method registration remains historical; these rows are not silently registered or classified by it.

## Record semantics, ownership and evidence gates

Every product row below has **Tier1**, **Kind: Behavior**, **Lifecycle: Proposed**, **Owner: IQToolkit project owner**, a versioned ID, requirement trace, scoped Given/When/Then claim and delivery phase. Proposed means required or conditional planning obligation, never accepted architecture, a new public API, active evidence or completed implementation. The owner capacity is product/public-contract selection; security, platform, data and support decisions remain explicit prerequisites, with no fictitious approval team.

Scope **R** is an adopted required roadmap outcome. Scope **S** is a shared feature requiring an owner-selected slice or an explicit reasoned deferral. Scope **C** applies only if its workload/model/follow-on is selected. Scope never settles which contract, engine version, payload representation, workload, policy or platform the owner will accept. The claim is conditional on the accepted scope named in its Given clause; expansion must make its preconditions measurable before dispatch. A deferral is a decision record, not a passed behavior fact.

Each row's phase binds to the [deferred phase contract](modernization-deferred-phase-contracts.md): dependencies, protected facts, actual-engine/public-consumer evidence, data authorization, security/policy mutation checks, rollback and integrated closure. These are **planned evidence gates**, not evidence bindings or test results. Before activation, the expanded packet must assign a distinct EV-ID, exact public test/oracle/fixture/configuration paths and literal command; meaningful semantic RED precedes implementation, or an explicit Verify task handles already-correct behavior. Proposed rows require no invented runnable test command now. Every Active fact must have a genuine binding before dispatch; semantic changes require owner-approved Fact Change, while mechanical rebinding requires demonstrated equivalence.

The common closure command is `bash eng/verify-final.sh` after the complete selected register and profile are expanded/reviewed. Engine-specific acceptance and demo commands are mandatory prerequisite outputs, not inferred from that foundation gate. Data is authentic approved capture/provider input; generated evidence needs named human authorization. Performance, memory, relevance and platform claims require the accepted workload/profile and actual measurement. None is satisfied by source inspection or compilation.

## M0 release confidence

Existing M0 cache/array/null/resource/native ownership/mapping facts remain in the historical register. These Proposed rows make remaining cross-cutting acceptance explicit; they do not replace protecting regressions.

| Fact ID | Phase / scope / trace | Given / When / Then |
| :--- | :--- | :--- |
| MOD.M0.BUILD.v1 | M0 / R / LOCAL-RM-M0 | Given the declared SDK/restore profile, when a clean Release verification runs, then restore/build/analyzer warnings and errors fail the gate and successful evidence contains none. |
| MOD.M0.API.v1 | M0 / R / LOCAL-RM-M0, LOCAL-RM-SUPPORT | Given the approved1.x API baseline, when an updated package is consumed, then retained public signatures and supported semantics remain compatible unless an explicit approved major change applies. |
| MOD.M0.VERSION.v1 | M0 / R / LOCAL-RM-M0 | Given one approved release metadata source, when packages and assemblies are built, then their declared version identities agree with that source without duplicate competing metadata. |
| MOD.M0.CAPTURE.v1 | M0 / R / LOCAL-RM-M0 | Given a reused ordinary query and changing captured values, when uncached/cached execution repeats, then results reflect each current capture without another root's mapping/policy substitution. |
| MOD.M0.MAPPING.v1 | M0 / R / LOCAL-RM-M0 | Given distinct mapped types/table identities, when their queries share caches, then each result and translation retain that mapping identity. |
| MOD.M0.TRANSACTION.v1 | M0 / R / LOCAL-RM-M0 | Given a caller-supplied transaction and supported raw operation, when the operation executes and rollback occurs, then it uses that transaction and preserves its caller-owned lifetime. |
| MOD.M0.PACKAGE.v1 | M0 / R / LOCAL-RM-M0, LOCAL-RM-SUPPORT | Given each declared independent package, when a cold consuming application restores and uses it, then declared dependencies/public operations work without repository project references or undeclared copied binaries. |
| MOD.M0.EVIDENCE.v1 | M0 / R / LOCAL-RM-M0, LOCAL-RM-SUPPORT | Given real executed test/provider checks, when verification reports results, then empty execution, skipped integration, failures and historical evidence are distinguished from current acceptance. |

## M1 modern execution and hosting

| Fact ID | Phase / scope / trace | Given / When / Then |
| :--- | :--- | :--- |
| MOD.M1.QUERY.IO.v1 | M1 / R / LOCAL-RM-M1 | Given a supported genuine-async driver operation, when an async query runs, then driver I/O is asynchronous rather than synchronous work hidden by Task.Run. |
| MOD.M1.COMMAND.IO.v1 | M1 / R / LOCAL-RM-M1 | Given a supported genuine-async command, when it executes, then command I/O reaches the driver async path and reports its actual outcome. |
| MOD.M1.CANCEL.v1 | M1 / R / LOCAL-RM-M1 | Given an active operation and caller cancellation, when cancellation is requested, then it reaches the driver and terminates/releases resources under the accepted recovery contract. |
| MOD.M1.STREAM.v1 | M1 / R / LOCAL-RM-M1 | Given an approved large-result workload, when async streaming consumes results, then it does not buffer the whole result and stays within the accepted memory bounds. |
| MOD.M1.EARLY.DISPOSE.v1 | M1 / R / LOCAL-RM-M1 | Given an active async stream, when consumption stops early or faults, then owned readers/commands/leases release under the accepted async ownership contract. |
| MOD.M1.TRANSACTION.v1 | M1 / R / LOCAL-RM-M1 | Given declared async transaction capabilities, when begin/commit/rollback runs, then supported operations use genuine driver async I/O and unsupported operations are identified explicitly. |
| MOD.M1.ASYNC.DISPOSE.v1 | M1 / R / LOCAL-RM-M1 | Given a declared async-disposable resource, when cleanup runs, then supported driver async cleanup is used and synchronous fallback limits remain explicit. |
| MOD.M1.BORROWED.v1 | M1 / R / LOCAL-RM-M1 | Given caller-owned connections/transactions, when toolkit execution finishes, fails or cancels, then it preserves caller ownership while releasing only toolkit-owned resources. |
| MOD.M1.SCOPES.v1 | M1 / R / LOCAL-RM-M1 | Given independent application scopes executing concurrently, when queries run, then mappings, tenant policy, connections and transactions do not leak across scopes. |
| MOD.M1.SYNC.v1 | M1 / R / LOCAL-RM-M1 | Given an existing supported synchronous consumer, when async features are added, then its supported query/update/ownership behavior remains available and compatible. |
| MOD.M1.HOSTING.v1 | M1 / R / LOCAL-RM-M1 | Given application-opted-in DI/hosting integration, when scopes resolve and end, then configured provider lifetimes/ownership behave as documented without making hosting dependencies mandatory in core. |
| MOD.M1.LOGGING.v1 | M1 / R / LOCAL-RM-M1 | Given application-opted-in structured logging, when operations succeed, fail or cancel, then structured events report actual operation outcomes under the redaction contract without sensitive payloads. |
| MOD.M1.REDACTION.v1 | M1 / R / LOCAL-RM-M1 | Given default diagnostics, when operations succeed or fail, then credentials, parameter values, document contents and vectors are omitted. |
| MOD.M1.TRACING.v1 | M1 / R / LOCAL-RM-M1 | Given application-opted-in tracing/metrics integration, when operations execute, then activities and configured duration/retry/item/failure metrics describe actual outcomes without sensitive payloads. |
| MOD.M1.ERRORS.v1 | M1 / R / LOCAL-RM-M1 | Given cancellation, timeout, conflict, unsupported-operation or backend failure, when reported publicly, then those categories remain distinct and useful permitted native details are retained. |

## M1q mapping and query features

Each S row requires a selected bounded slice or an approved explicit deferral, before a consuming provider depends on it. PostgreSQL/DuckDB design proposals are not owner selections.

| Fact ID | Phase / scope / trace | Given / When / Then |
| :--- | :--- | :--- |
| MOD.MAP.CONSTRUCTION.v1 | M1q / S / LOCAL-RM-MAPPING | Given an accepted record/immutable constructor mapping, when results materialize, then the selected constructor produces the required object values without unintended mutation. |
| MOD.MAP.NULLABILITY.v1 | M1q / S / LOCAL-RM-MAPPING | Given accepted nullable-reference metadata, when null values materialize or write, then the documented nullable/nonnullable contract is enforced. |
| MOD.MAP.CONVERTER.v1 | M1q / S / LOCAL-RM-MAPPING | Given an explicitly selected value converter, when values round-trip through a supported backend, then the declared conversion and failure semantics are preserved. |
| MOD.MAP.TIME.v1 | M1q / S / LOCAL-RM-MAPPING | Given selected modern date/time types, when values round-trip, then accepted precision/offset/time-zone semantics remain intact for that backend. |
| MOD.MAP.JSON.v1 | M1q / S / LOCAL-RM-MAPPING | Given a selected JSON mapping, when documents query/round-trip, then supported paths and values follow the accepted representation and failure contract. |
| MOD.MAP.ARRAY.v1 | M1q / S / LOCAL-RM-MAPPING | Given selected array mappings, when arrays query/round-trip, then supported element/null/empty behavior matches the accepted backend contract. |
| MOD.MAP.MISSING.v1 | M1q / S / LOCAL-RM-MAPPING | Given a document with missing versus explicit-null fields, when read/updated, then the accepted mapping keeps those states distinct. |
| MOD.QUERY.KEYSET.v1 | M1q / S / LOCAL-RM-QUERY | Given a supported deterministic ordering and keyset cursor, when pages are fetched, then continuation follows the accepted ordering/tie rules without offset substitution. |
| MOD.QUERY.CURSOR.v1 | M1q / S / LOCAL-RM-QUERY | Given a backend continuation token, when later pages are requested, then the accepted opaque/versioned cursor contract preserves continuation and rejects invalid context. |
| MOD.QUERY.RAW.v1 | M1q / S / LOCAL-RM-QUERY | Given a selected raw-command escape hatch with parameters, when executed, then values are bound through the backend parameter contract rather than interpolated as command text. |
| MOD.QUERY.PREVIEW.v1 | M1q / S / LOCAL-RM-QUERY | Given a query and declared capabilities, when previewed/validated, then its inspectable form and unsupported-operation diagnostics reflect the actual backend plan. |
| MOD.QUERY.CONFLICT.v1 | M1q / S / LOCAL-RM-QUERY | Given an optimistic/conditional update conflict, when an update runs, then it returns the accepted distinct conflict outcome instead of reporting an unconditional success. |
| MOD.QUERY.BATCH.v1 | M1q / S / LOCAL-RM-QUERY | Given selected batch/bulk operations, when some items fail, then successes and failures are reported explicitly without implying atomicity. |
| MOD.POLICY.TENANT.v1 | M1q / S / LOCAL-RM-MAPPING | Given an accepted tenant scope, when ordinary supported operations execute, then another tenant's data is neither read nor changed through policy/cache leakage. |
| MOD.POLICY.SOFTDELETE.v1 | M1q / S / LOCAL-RM-MAPPING | Given selected soft-delete policy, when ordinary operations run, then visibility and mutation follow that policy's accepted contract. |
| MOD.POLICY.BYPASS.v1 | M1q / S / LOCAL-RM-MAPPING | Given an authorized explicit policy bypass, when invoked, then it is distinguished and tested according to the accepted security contract rather than implicit query behavior. |

## M2 PostgreSQL

| Fact ID | Phase / scope / trace | Given / When / Then |
| :--- | :--- | :--- |
| MOD.PG.QUERY.v1 | M2 / R / LOCAL-RM-M2 | Given the accepted PostgreSQL dialect/workload, when supported queries run, then public results match the independent real-engine oracle. |
| MOD.PG.CRUD.v1 | M2 / R / LOCAL-RM-M2 | Given approved real entities, when create/read/update/delete runs through the provider, then persisted state matches the accepted operation contract. |
| MOD.PG.KEY.v1 | M2 / R / LOCAL-RM-M2 | Given accepted generated-key mapping, when creation succeeds, then the returned identity matches the actual created entity. |
| MOD.PG.TX.v1 | M2 / R / LOCAL-RM-M2 | Given owned or borrowed supported transactions, when commit/rollback runs, then real persisted state and resource ownership match the accepted contract. |
| MOD.PG.PAGE.v1 | M2 / R / LOCAL-RM-M2 | Given accepted deterministic pagination, when pages are queried, then actual engine results preserve the selected ordering/continuation semantics. |
| MOD.PG.PARAM.v1 | M2 / R / LOCAL-RM-M2 | Given supported parameter values, when a query/update runs, then values retain their type/value semantics without command-text interpolation. |
| MOD.PG.TYPE.NULL.v1 | M2 / R / LOCAL-RM-M2 | Given selected PostgreSQL types and nullable values, when mapped values round-trip/query, then accepted type/precision/null semantics match native behavior. |
| MOD.PG.UNSUPPORTED.v1 | M2 / R / LOCAL-RM-M2 | Given an unsupported operation/type, when requested, then the provider reports its declared capability failure predictably rather than silently approximating behavior. |

## M3 DynamoDB

| Fact ID | Phase / scope / trace | Given / When / Then |
| :--- | :--- | :--- |
| MOD.DDB.KEYED.v1 | M3 / R / LOCAL-RM-M3 | Given an accepted table/key mapping and approved items, when typed get/put/update/delete executes, then keyed state matches the service operation contract. |
| MOD.DDB.CONDITION.v1 | M3 / R / LOCAL-RM-M3 | Given a failed conditional write, when requested, then a distinct conflict result preserves the accepted native condition semantics. |
| MOD.DDB.PARTITION.v1 | M3 / R / LOCAL-RM-M3 | Given an explicit partition equality and supported sort condition, when queried, then only that declared key access pattern is dispatched. |
| MOD.DDB.FILTER.v1 | M3 / R / LOCAL-RM-M3 | Given supported attribute filters/projection, when queried, then results and diagnostics preserve service-side post-read filtering and projected attributes. |
| MOD.DDB.INDEX.v1 | M3 / R / LOCAL-RM-M3 | Given an explicit table/index choice, when queried, then the selected mapping and index access are used without implicit substitution. |
| MOD.DDB.CONSISTENCY.v1 | M3 / R / LOCAL-RM-M3 | Given a read consistency unsupported by its selected index, when requested, then it is rejected rather than silently downgraded. |
| MOD.DDB.PRESENCE.v1 | M3 / R / LOCAL-RM-M3 | Given missing and explicit-null attributes, when items round-trip, then the accepted converter preserves that distinction. |
| MOD.DDB.SERIALIZE.v1 | M3 / R / LOCAL-RM-M3 | Given selected number/binary/collection/date representations, when items round-trip, then accepted conversion and failure semantics are retained. |
| MOD.DDB.EMPTY.PAGE.v1 | M3 / R / LOCAL-RM-M3 | Given an empty filtered page with continuation, when enumeration continues, then it follows that continuation rather than treating the empty page as completion. |
| MOD.DDB.CURSOR.v1 | M3 / R / LOCAL-RM-M3 | Given a page request/result, when continuation is returned/reused, then items and opaque context-bound token follow the accepted paging contract. |
| MOD.DDB.BUDGET.v1 | M3 / R / LOCAL-RM-M3 | Given accepted item/request/capacity limits and Take semantics, when pages are read, then dispatch and returned items obey those limits with explicit capacity diagnostics when requested. |
| MOD.DDB.SCAN.v1 | M3 / R / LOCAL-RM-M3 | Given unsupported key-query translation, when requested, then it cannot become a scan; only explicit opted-in scans with declared budgets execute. |
| MOD.DDB.CREDENTIAL.v1 | M3 / R / LOCAL-RM-M3 | Given regional SDK credential-chain configuration, when calls run, then credentials are obtained through that chain and absent from mappings/diagnostics. |
| MOD.DDB.RETRY.v1 | M3 / R / LOCAL-RM-M3 | Given an accepted single-owner retry/uncertain-write policy, when failures occur, then retry counts are bounded and unsafe writes are not silently replayed or misreported. |

## M4a vector retrieval

Each R row requires independent real pgvector and Qdrant experiment evidence under the reviewed contract, before stability. Milvus is not selected implicitly.

| Fact ID | Phase / scope / trace | Given / When / Then |
| :--- | :--- | :--- |
| MOD.VECTOR.ROUNDTRIP.v1 | M4a / R / LOCAL-RM-M4A | Given accepted collection/index/key/type identity and real records/vectors, when upsert/get/delete/search runs, then actual engine state/results follow the declared capability contract. |
| MOD.VECTOR.INPUT.v1 | M4a / R / LOCAL-RM-M4A | Given incompatible dimensions or nonfinite vector values, when a retrieval request is made, then it is rejected before dispatch. |
| MOD.VECTOR.CONTROLS.v1 | M4a / R / LOCAL-RM-M4A | Given selected metric/index and available exact/approximate controls, when queried, then the declared controls are applied or unsupported choices explicitly rejected. |
| MOD.VECTOR.MODEL.v1 | M4a / R / LOCAL-RM-M4A | Given accepted model/version metadata and application-supplied vectors, when retrieval runs, then metadata remains explicit without making embedding generation/model hosting a core dependency. |
| MOD.VECTOR.FILTER.v1 | M4a / R / LOCAL-RM-M4A | Given a required candidate filter, when top-k retrieval runs, then preselection versus returned-set filtering remains explicit and cannot be silently substituted. |
| MOD.VECTOR.ISOLATION.v1 | M4a / R / LOCAL-RM-M4A | Given a required tenant/authorization filter the backend cannot express, when requested, then the request is rejected rather than filtering unauthorized results client-side. |
| MOD.VECTOR.PROJECTION.v1 | M4a / R / LOCAL-RM-M4A | Given an accepted result projection, when retrieval returns, then records contain the declared metadata fields with explicit unsupported projection behavior. |
| MOD.VECTOR.SCORE.v1 | M4a / R / LOCAL-RM-M4A | Given engine-specific retrieval results, when returned, then metric, score direction and rank are exposed, with any optional normalization documented and no raw cross-engine scale equivalence implied. |
| MOD.VECTOR.TOPK.v1 | M4a / R / LOCAL-RM-M4A | Given accepted top-k/filter/tie semantics, when queried, then result counts/order match that declared contract and backend exclusions remain explicit. |
| MOD.VECTOR.CANCEL.v1 | M4a / R / LOCAL-RM-M4A | Given active retrieval and caller cancellation, when cancelled, then the accepted driver/resource recovery contract holds on the actual engine. |
| MOD.VECTOR.QUALITY.v1 | M4a / R / LOCAL-RM-M4A | Given a reviewed relevance corpus and selected exact/approximate workload, when evaluated, then measured recall/relevance meets owner-approved criteria with profile/limits disclosed. |

## M4b OpenSearch

All non-hybrid rows are required if the text-search workload selects M4b; that selection remains unanswered. Hybrid rows are separately conditional.

| Fact ID | Phase / scope / trace | Given / When / Then |
| :--- | :--- | :--- |
| MOD.SEARCH.MAPPING.v1 | M4b / C / LOCAL-RM-M4B | Given selected index mappings/analyzers, when records index and queries run, then actual text/type analysis matches the accepted schema contract. |
| MOD.SEARCH.VISIBILITY.v1 | M4b / C / LOCAL-RM-M4B | Given declared refresh policy, when records index/update/delete, then search visibility matches the documented timing/consistency contract. |
| MOD.SEARCH.RELEVANCE.v1 | M4b / C / LOCAL-RM-M4B | Given an approved text corpus/workload, when text queries execute, then measured relevance meets accepted criteria. |
| MOD.SEARCH.ISOLATION.v1 | M4b / C / LOCAL-RM-M4B | Given required filters and tenant policy, when search executes, then actual engine results enforce the accepted isolation contract without hidden client filtering. |
| MOD.SEARCH.FACET.v1 | M4b / C / LOCAL-RM-M4B | Given accepted facet requests, when search runs, then facet outputs match real indexed records and documented filter scope. |
| MOD.SEARCH.HIGHLIGHT.v1 | M4b / C / LOCAL-RM-M4B | Given selected highlighting rules, when matching text returns, then highlights follow the accepted escaping/content contract. |
| MOD.SEARCH.PAGE.v1 | M4b / C / LOCAL-RM-M4B | Given declared pagination limits/order, when pages are fetched, then the accepted bounded continuation behavior holds. |
| MOD.SEARCH.PARTIAL.v1 | M4b / C / LOCAL-RM-M4B | Given partial shard or bulk failures, when operations report results, then failures/successes are explicit rather than disguised as complete success. |
| MOD.SEARCH.CANCEL.v1 | M4b / C / LOCAL-RM-M4B | Given active search and cancellation, when cancelled, then the accepted resource/driver recovery contract holds. |
| MOD.SEARCH.HYBRID.v1 | M4b / C / LOCAL-RM-M4B, LOCAL-RM-LATER | Given a selected hybrid pipeline and approved corpus, when retrieval runs, then ranking configuration and measured quality are visible without promising matching rank order across engines. |

## M5 analytics

| Fact ID | Phase / scope / trace | Given / When / Then |
| :--- | :--- | :--- |
| MOD.DUCK.QUERY.v1 | M5 / R / LOCAL-RM-M5 | Given approved analytical data, when selected projections/aggregates query DuckDB, then results reproduce the independent engine oracle. |
| MOD.DUCK.PARAM.v1 | M5 / R / LOCAL-RM-M5 | Given supported parameters, when analytical queries run, then values are bound with accepted type/value semantics. |
| MOD.DUCK.TYPE.v1 | M5 / R / LOCAL-RM-M5 | Given selected native types/nulls, when data maps/round-trips, then accepted precision/shape/null semantics match actual engine behavior. |
| MOD.DUCK.STREAM.v1 | M5 / R / LOCAL-RM-M5 | Given an approved large-result workload, when consumed, then streaming stays within accepted memory/resource bounds without whole-result buffering. |
| MOD.DUCK.IMPORT.v1 | M5 / R / LOCAL-RM-M5 | Given an explicit approved input/source and import budget, when import runs, then accepted data/result/error limits hold without implicit untrusted file selection. |
| MOD.DUCK.FILE.v1 | M5 / R / LOCAL-RM-M5 | Given explicit file/extension configuration, when query/import requests access, then only the accepted configuration applies and untrusted values cannot implicitly enable loading. |
| MOD.DUCK.NATIVE.v1 | M5 / R / LOCAL-RM-M5 | Given declared ARM64/x64 distributions, when real consuming applications run, then the actual native assets load and selected operations pass on each supported profile. |
| MOD.DUCK.WRITE.v1 | M5 / R / LOCAL-RM-M5 | Given documented analytical write/transaction limits, when a supported or unsupported operation is requested, then those limits are enforced without general OLTP promises. |
| MOD.CLICK.QUERY.v1 | M5 / C / LOCAL-RM-M5 | Given a selected remote ClickHouse workload, when analytical reads run, then accepted projections/aggregates/type/stream semantics match the actual server. |
| MOD.CLICK.APPEND.v1 | M5 / C / LOCAL-RM-M5 | Given selected append/bulk ingestion, when writes execute, then visibility and partial-failure outcomes follow the accepted contract without relational transaction assumptions. |

## M6 demand-selected models

These rows cover candidate behavior without adopting every database. Only the selected model's rows can activate; required selection/value/support outcomes are in the process register.

| Fact ID | Phase / scope / trace | Given / When / Then |
| :--- | :--- | :--- |
| MOD.DOC.ID.v1 | M6 / C / LOCAL-RM-M6 | Given a selected document-store ID contract, when keyed CRUD executes, then real document state matches that contract. |
| MOD.DOC.QUERY.v1 | M6 / C / LOCAL-RM-M6 | Given accepted nested-field filters/projection/paging, when queried, then actual document results preserve the declared query contract. |
| MOD.DOC.PRESENCE.v1 | M6 / C / LOCAL-RM-M6 | Given missing/null/array fields and accepted schema evolution, when documents map, then those representations remain distinct as specified. |
| MOD.DOC.CONFLICT.v1 | M6 / C / LOCAL-RM-M6 | Given conditional replacement/revision conflict, when written, then accepted conflict outcomes preserve real engine revision semantics. |
| MOD.GRAPH.QUERY.v1 | M6 / C / LOCAL-RM-M6 | Given selected parameterized native graph queries, when executed, then typed results match actual relationship data without parameter interpolation. |
| MOD.GRAPH.BOUNDS.v1 | M6 / C / LOCAL-RM-M6 | Given accepted depth/fan-out budgets, when traversal runs, then those bounds and the specified cycle/path materialization behavior hold. |
| MOD.KEYED.STATE.v1 | M6 / C / LOCAL-RM-M6 | Given selected keyed/conditional operations, when executed, then key scope and conflicts follow the accepted native contract. |
| MOD.KEYED.TTL.v1 | M6 / C / LOCAL-RM-M6 | Given selected expiration rules, when keyed state expires/is read, then accepted TTL/visibility behavior holds without implicit result-cache semantics. |
| MOD.CASS.PARTITION.v1 | M6 / C / LOCAL-RM-M6 | Given a selected wide-column partition access pattern, when queried, then explicit partition/paging/consistency behavior matches the supported engine contract. |
| MOD.MODEL.BOUNDARY.v1 | M6 / C / LOCAL-RM-M6 | Given a selected nonrelational model, when operations run, then they do not silently inherit relational identity-map or cross-record transaction guarantees. |

## Conditional follow-ons

| Fact ID | Phase / scope / trace | Given / When / Then |
| :--- | :--- | :--- |
| MOD.SQL.CTE.v1 | Later / C / LOCAL-RM-LATER | Given selected CTE capability, when a supported query runs, then its real backend result matches the declared query semantics or unsupported capability is rejected. |
| MOD.SQL.WINDOW.v1 | Later / C / LOCAL-RM-LATER | Given selected window/richer aggregate/database-function capability, when queried, then accepted partition/order/type/null semantics match the actual engine. |
| MOD.GENERATION.v1 | Later / C / LOCAL-RM-LATER | Given selected source-generated mapping/query scope, when consumed, then its supported public results match the accepted runtime semantics and declared trimming/AOT profile. |
| MOD.DDB.BATCH.v1 | Later / C / LOCAL-RM-LATER | Given selected DynamoDB batch operations, when items remain unprocessed or fail, then partial outcomes and bounded single-owner retries are explicit. |
| MOD.DDB.ATOMIC.v1 | Later / C / LOCAL-RM-LATER | Given a selected distinct transactional-request contract, when DynamoDB transactions execute, then actual atomic/conflict outcomes follow that service contract. |
| MOD.DDB.TTL.STREAM.v1 | Later / C / LOCAL-RM-LATER | Given selected TTL metadata/change-stream integration, when expiration/events occur, then accepted visibility, ordering and recovery semantics are explicit. |
| MOD.INDEX.SYNC.v1 | Later / C / LOCAL-RM-LATER | Given selected outbox/change-feed synchronization, when updates/deletions/replay run, then versioned idempotent propagation and checkpoints support the declared rebuild path. |
| MOD.INDEX.LAG.v1 | Later / C / LOCAL-RM-LATER | Given a selected synchronized search index, when consumers query, then lag/stale-read behavior is reported as eventual synchronization rather than a distributed transaction. |
| MOD.RESULT.CACHE.v1 | Later / C / LOCAL-RM-LATER | Given separately selected result caching, when underlying data changes, then accepted invalidation/key-scope/consistency behavior holds independently of compiled-query caching. |

## Selection, discovery and support acceptance register

These versioned records are **Kind: Decision/Probe/Support**, **Lifecycle: Proposed**, **Owner: IQToolkit project owner**. They are process/documentary obligations, not invented Tier1 runtime behavior. Their evidence is a dated, scoped outcome/decision and supporting actual source/engine/consumer artifacts. A probe result can be failed, excluded or supported only within its measured scope; it cannot silently activate a product fact.

| Record ID | Kind / trace | Given / When / Then and required evidence |
| :--- | :--- | :--- |
| PLAN.SHARED.SELECTION.v1 | Decision / LOCAL-RM-MAPPING, LOCAL-RM-QUERY | Given every listed shared feature, when a provider workload expands, then each feature is selected with bounded facts or explicitly deferred with owner/reason; design proposals alone do not count. |
| PLAN.SEARCH.SELECTION.v1 | Decision / LOCAL-RM-M4B | Given text/hybrid demand, when M4b expands, then owner-selected scope/corpus/public contract and hybrid inclusion/exclusion are recorded. |
| PLAN.CLICK.SELECTION.v1 | Decision / LOCAL-RM-M5 | Given remote analytical demand, when ClickHouse is considered, then a named workload, maintainer and explicit selection/deferral are recorded. |
| PLAN.MODEL.SELECTION.v1 | Decision / LOCAL-RM-M6 | Given candidate document/graph/keyed-cache/wide-column models, when one is selected, then its real application, maintainer, value beyond native SDK and others' disposition are recorded. |
| PLAN.LATER.SELECTION.v1 | Decision / LOCAL-RM-LATER | Given each listed follow-on, when evaluated, then explicit selected/deferred scope and reason preserve it without marking unimplemented behavior complete. |
| PROBE.MARIADB.v1 | Probe / LOCAL-RM-DISCOVERY | Given a named MariaDB/MySQL-provider pair, when compatibility is probed, then actual SQL/type/key/transaction evidence yields a scoped support/exclusion outcome; protocol similarity is insufficient. |
| PROBE.MILVUS.v1 | Probe / LOCAL-RM-DISCOVERY | Given exact client/server identities, when parity/interoperability is probed, then source/compile/server/quality evidence and gaps are distinguished before any provider commitment. |
| PROBE.VECTOR.INTEROP.v1 | Probe / LOCAL-RM-DISCOVERY | Given exact abstraction/connectors, when interoperability is probed, then source/API/compile/engine semantics and reuse/extension decisions are recorded separately. |
| PROBE.AOT.v1 | Probe / LOCAL-RM-DISCOVERY | Given explicit trimming/AOT profiles, when feasibility is evaluated, then analyzer/publication/runtime outcomes and exclusions are recorded without a blanket compatibility promise. |
| PLAN.OTHER.DISCOVERY.v1 | Decision / LOCAL-RM-DISCOVERY | Given distributed SQL/time-series/geospatial/embedded-document candidates, when demand is evaluated, then recorded selection/deferral gates prevent silent provider commitments. |
| SUPPORT.STATUS.v1 | Support / LOCAL-RM-SUPPORT | Given each package/capability, when documented/promoted, then experimental/supported/legacy status and owner decision match actual evidence; at most one new backend is promoted at a time. |
| SUPPORT.MATRIX.v1 | Support / LOCAL-RM-SUPPORT | Given implemented engines/SDKs/platforms, when support is published, then named tested versions/OS/architectures, exclusions and actual certification evidence are explicit. |
| SUPPORT.EXAMPLE.v1 | Support / LOCAL-RM-SUPPORT | Given selected public/configuration surfaces, when documented examples run, then actual package consumers reproduce the declared behavior with approved data/setup and explicit integration skips. |
| SUPPORT.VERSION.v1 | Support / LOCAL-RM-SUPPORT | Given additive1.x/preview changes or intentional breaks, when releases are prepared, then compatibility/migration/approved-major policy and immutable package identities are enforced; Git tags alone do not establish NuGet availability. |
| SUPPORT.LEGACY.v1 | Support / LOCAL-RM-SUPPORT | Given Windows-only Access and legacy SQL Server Compact, when scope is documented, then their platform/maintenance limits remain explicit rather than new cross-platform feature promises. |
| SUPPORT.LICENSE.v1 | Support / LOCAL-RM-SUPPORT | Given candidate server/client versions, when selection occurs, then server/client licensing, self-hostability, editions/features and upgrade ownership are recorded separately. |

## Requirement coverage and activation limits

| Roadmap obligation | Register coverage | Planned evidence/decision gate |
| :--- | :--- | :--- |
| Release confidence and existing semantics | Historical34 rows; MOD.M0.* | M0 real regressions, strict/current final/API/package evidence, pending data/coverage/policy and stopped-packet follow-up. |
| Modern execution/hosting/diagnostics | MOD.M1.* | M1 accepted execution/lifetime/error contract and genuine driver/pending-I/O/resource/memory/scope/diagnostic evidence. |
| Every modern mapping/query feature | MOD.MAP.*, MOD.QUERY.*, MOD.POLICY.*; PLAN.SHARED.SELECTION.v1 | M1q selection/deferral for every listed feature, data/type/policy/public contracts and consuming-provider semantics. |
| PostgreSQL | MOD.PG.* | M2 SQL/public contract plus exact real-engine/package-consumer evidence. |
| DynamoDB initial capability | MOD.DDB.* in M3 | Key/index/converter/cursor/retry contract and approved disposable AWS evidence/cost/cleanup. |
| Two-engine retrieval and quality | MOD.VECTOR.* | M4a source/compile interoperability, accepted retrieval/security/score contract and actual pgvector/Qdrant/corpus evidence. |
| Text and optional hybrid search | MOD.SEARCH.*; PLAN.SEARCH.SELECTION.v1 | M4b workload selection, actual mapping/refresh/relevance/isolation/partial-failure and optional hybrid corpus evidence. |
| DuckDB and conditional ClickHouse | MOD.DUCK.*, MOD.CLICK.*; PLAN.CLICK.SELECTION.v1 | M5 SQL/file/import/stream/native contract; real native profiles, approved files and selected remote-engine evidence. |
| Document, graph, keyed-cache, wide-column candidates | MOD.DOC.*, MOD.GRAPH.*, MOD.KEYED.*, MOD.CASS.*, MOD.MODEL.*; PLAN.MODEL.SELECTION.v1 | M6 required application/value/maintainer selection and selected model-specific authentic evidence. |
| Named compatibility/interoperability/AOT discovery | PROBE.* | Separately expanded bounded exact-source/engine profiles with measured findings and explicit support limits. |
| Conditional extensions/result caching/index sync | MOD.SQL.*, MOD.GENERATION.*, later MOD.DDB.*, MOD.INDEX.*, MOD.RESULT.CACHE.*; PLAN.LATER.SELECTION.v1 | Demand/contract/data/owner gates before independent Test→Implement slices; no implicit selection. |
| Other database-type discovery | PLAN.OTHER.DISCOVERY.v1 | Explicit demand-led decision/probe, not commitment to deploy every candidate. |
| Support/packaging/compatibility/licensing | SUPPORT.* plus MOD.M0.API/PACKAGE/EVIDENCE | Complete actual engine/platform/consumer evidence, owner promotion/selection and immutable artifact/migration policy. |

M0, M1 and later implementations remain incomplete. This register supplies planning traceability; it proves no behavior, resolves no pending owner choice and resets no native or repair budget.
