---
title: PostgreSQL capability and adapter contract proposal
version: 0.1
last_updated: 2026-10-03
status: Proposed for owner review; implementation and selections not approved
owner: IQToolkit project owner
tags: [modernization, postgresql, npgsql, capabilities, mapping, contract]
---

# PostgreSQL capability and adapter contract proposal

D2 independently prepares the SQL capability gate for LOCAL-RM-M2 against [roadmap 0.3](modernization-roadmap.md) and [implementation-plan inventory](modernization-implementation-plan.md). It covers queries, CRUD, generated keys, transactions, pagination, parameters and selected types, retaining every LOCAL-RM-MAPPING and LOCAL-RM-QUERY item as a proposed selection or explicit proposed deferral. Nothing here approves a public API, dependency change, production adapter, data fixture, coverage exception or milestone completion. Production M1/M2 still require complete M0, accepted [D1 execution contracts](modernization-execution-contract.md), owner-reviewed workload/SQL capabilities and actual integration evidence.

## Authority, execution contract and GOTCHA

Technical authority is the owner-selected ENG-STD-DOTNET-XPLAT 1.1.0-draft.1, [retained source](standards/source/Cross-Platform-DotNet-Engineering-Standard-1.1.0-draft.1.md), SHA256 `8c56930ec1cd26e8b58a273d47cb0b9713f55e6d4961b6d29a636ffaf4f0ebe2`. Applicable obligations include DOTNET-DEP-001/002/003/004 dependency profiles, DOTNET-API-001/002/003 compatibility/versioning, DOTNET-TEST-001 integration/coverage, DOTNET-PLAT-002 boundaries, DOTNET-SEC-002/OBS-001 sensitive diagnostics and DOTNET-GOV-005 scoped adoption. Development Plan Creation Guide 2.7 and the autonomous plan guide supply evidence/disposition discipline. This proposal makes no whole-standard compliance claim.

**Role/type:** Documentation/design preparation. **Dependencies:** none for independent source preparation; production remains gated above. **Ownership:** version_metadata owns this document only; root owns plan/state, SDK, services, dependency/public implementation and integration. **Facts introduced:** none in production. **Facts protected:** existing synchronous/provider behavior, exact source/package/native records and prior scoped evidence. **Rollback:** remove or revise only this new proposal; no implementation exists to revert.

**Goals/objectives:** define a bounded additive PostgreSQL adapter and identify the actual missing common seams. **Trigger/termination:** roadmap M2 preparation; stop at a reviewable proposal, source/link checks and ownership handoff. **Capabilities/off-limits:** read current source and primary documentation; no SDK, service, package installation, authored rows/schema/errors, paid resources or production edits. **Health:** root-owned future probes require explicit deadlines, terminal outcomes, source/lock/native identities and owned cleanup; no passing skip or weakened gate. **Attributes:** proposed selections differ from accepted contracts, source predictions and runtime observations. **Users/runtime:** maintainers reviewing a future optional package; no deployed runtime. **Beliefs/intentions:** existing SQL abstractions are reusable, but PostgreSQL RETURNING, type semantics and lifecycle need deliberate extension and real evidence.

## Exact dependency and environment context

The observed P0-P3 profile uses SDK10.0.401/runtime10.0.12, net10.0, Ubuntu24.04.5/Linux ARM64, privately compiled PostgreSQL18.6 and Npgsql10.0.3. PostgreSQL source archive SHA256 is `555610c24d53e4316da5b7d3fc25c279d96856d5e0e23ee308c328c5fa881d9f`; [official archive](https://ftp.postgresql.org/pub/source/v18.6/postgresql-18.6.tar.bz2) and [checksum](https://ftp.postgresql.org/pub/source/v18.6/postgresql-18.6.tar.bz2.sha256) establish provenance. ICU/readline/TLS and other platforms are not qualified by that private build.

| Exact private probe pin | net10 dependency declaration | License/repository commit |
| :--- | :--- | :--- |
| Npgsql10.0.3 | Logging.Abstractions >=10.0.0 | PostgreSQL; npgsql/npgsql `d3768398c17877b3a916c3c4d87e8e11698991fc` |
| Logging.Abstractions10.0.12 | DI.Abstractions >=10.0.12 | MIT; dotnet/dotnet `95017c711e6afc1085133d440e42b4bd78155701` |
| DI.Abstractions10.0.12 | None | MIT; same dotnet/dotnet commit |

Primary declarations: [Npgsql catalog](https://api.nuget.org/v3/catalog0/data/2026.05.27.11.36.39/npgsql.10.0.3.json), [Logging catalog](https://api.nuget.org/v3/catalog0/data/2026.09.08.19.40.26/microsoft.extensions.logging.abstractions.10.0.12.json), [DI catalog](https://api.nuget.org/v3/catalog0/data/2026.09.08.19.40.26/microsoft.extensions.dependencyinjection.abstractions.10.0.12.json).

Exact retained catalog bodies are `artifacts/source._1ngal9c/npgsql-10.0.3-catalog.json`, `logging-10.0.12-catalog.json` and `di-10.0.12-catalog.json`. Their SHA256 values are respectively `aafee3f2c83215288d72f2b4316f02c35eaa51ffb6f9bb2e6c9dfbaa097d0972`, `2771272dec3ae60f97edd0c1bbbc57cad8c473ab9e01de4ca6070d8b84461d94` and `53ce4ffca067583be25ed649ab3b51954fa1ae5f1c315a00280c20952e69b0e2`. Approved signed-archive SHA512 pins are in `artifacts/postgresql-probe/npgsql.t_quc1gq/approved-pins.json`; its `packages.lock.json.reviewed` records the separate NuGet logical-content hashes. The probe explicitly pins all three packages directly. A future sole-direct toolkit-adapter consumer has a different, complete transitive graph; it must not inherit a three-package certification claim.

All seven retained Npgsql files in `artifacts/modern-execution/source-audit.b9crcp7b/manifest-npgsql.json` match their recorded SHA256 and published commit. New source qualification must retain exact version/configuration evidence rather than cite an unversioned driver feature as proof of this profile.

## Current source seams and missing extension

| Confirmed repository anchor | PostgreSQL consequence |
| :--- | :--- |
| [MySqlLanguage](../IQToolkit.Data.MySqlClient/MySqlLanguage.cs), [TSqlLanguage](../IQToolkit.Data.SqlClient/TSqlLanguage.cs), [SQLiteLanguage](../IQToolkit.Data.SQLite/SQLiteLanguage.cs) | Generated-ID expressions use LAST_INSERT_ID(), SCOPE_IDENTITY() and last_insert_rowid(). Quote/type/paging differences are already provider seams. |
| [BasicMapper](../IQToolkit.Data/Common/Mapping/BasicMapper.cs): GetInsertExpression336, GetInsertResult367, GetGeneratedIdCommand436 | Insert selectors become insert plus identity lookup and sometimes row requery. Generated retrieval concentrates on generated primary-key members; a replacement identity function cannot supply PostgreSQL RETURNING semantics. |
| BasicMapper.GetUpdateExpression482/GetInsertOrUpdateExpression541 | Update selectors can requery after an affected-row check; legacy insert-or-update is a conditional multi-operation algorithm, not an atomic upsert contract. |
| [AdvancedMapping](../IQToolkit.Data/Common/Mapping/AdvancedMapping.cs): GetInsertExpression217, GetInsertResult483 | Multi-table entity changes and generated-key propagation require explicit selection/exclusion; they do not become one RETURNING command automatically. |
| [QueryTranslator](../IQToolkit.Data/Common/QueryTranslator.cs), [QueryMapping](../IQToolkit.Data/Common/Mapping/QueryMapping.cs), [QueryPolice](../IQToolkit.Data/Common/QueryPolice.cs): BuildExecutionPlan89 | Mapping selects its mapper and policy owns plan construction. Adapter installation must preserve arbitrary caller mapping/policy behavior. |
| [ExecutionBuilder](../IQToolkit.Data/Common/ExecutionBuilder.cs): constructor32, VisitCommand337, BuildExecuteCommand468 | Constructor/build orchestration are private; protected visitors do not currently provide a usable subclass extension. Commands without a found projection dispatch ExecuteCommand. |
| [DbEntityProvider](../IQToolkit.Data/DbEntityProvider.cs): GetCommand628, AddParameter662, Convert324 | Executor overrides can bind vendor types; GetCommand explicitly attaches the current transaction. Existing eager/deferred ownership facts must remain protected. |
| [QueryLanguage](../IQToolkit.Data/Common/Language/QueryLanguage.cs): IsScalar180; [DbTypeSystem](../IQToolkit.Data/DbTypeSystem.cs) | Scalar recognition excludes DateOnly/TimeOnly/JSON/general arrays; common DbTypeSystem uses SqlDbType rather than PostgreSQL type metadata. |
| [FieldReader](../IQToolkit.Data/Common/FieldReader.cs): ReadValue45; DbFieldReader.GetValue; [TypeHelper](../IQToolkit/TypeHelper.cs): Convert148 | SQL NULL can become CLR defaults; generic reads use GetValue/general conversion. Typed/converter/null behavior needs explicit selection. |
| [SqlFormatter](../IQToolkit.Data/Common/Language/SqlFormatter.cs): VisitPredicate722, WriteValue762, VisitSelect831, VisitInsert1130 | Common formatting contains integer Boolean conventions, TOP, and insert syntax that cannot simply be inherited for PostgreSQL. |

Proposed optional package name is `IQToolkit.Data.Npgsql`; proposed classes are `PostgreSqlQueryProvider`, `PostgreSqlLanguage`, `PostgreSqlFormatter`, `PostgreSqlTypeSystem` and its executor. These names are reviewable, not existing public APIs. Vendor types stay in the optional package; core/Data do not acquire mandatory Npgsql or hosting dependencies. Preserve existing synchronous consumers and external subclasses without new abstract obligations.

The proposed common addition is a vendor-free **result-producing DML representation**, carrying its command, returned-column metadata, projector and cardinality. A default-disabled language hook must be used by BasicMapper before entering the identity-function branch; PostgreSQL can produce a RETURNING result while old providers retain current lowering. Execution dispatch must consume that result as a reader projection, and expression visitors, comparison/cache identity, parameterization, preview and diagnostics must traverse it correctly. A language-only GetGeneratedIdExpression override is insufficient because mapping invokes it before the linguist can repair the insert block.

Exact hook names/signatures require a reviewed source spike before implementation. Do not claim that the existing builder can be subclassed, fork the entire mapper/builder, or replace a caller's policy to force a PostgreSQL plan. A default-preserving FieldReader factory is a separate narrow candidate only where selected typed reads/converters require it. D1's separate async runner must understand the same DML result metadata without invoking a synchronous execution plan/executor, client-join/aggregate subquery, or field getter that can perform synchronous provider I/O. CPU-only construction and conversion over already acquired values may remain synchronous; preserve original errors through cleanup.

Keep AllowsMultipleCommands false for the initial PostgreSQL lowering: common IF/declaration/block handling remains explicit composition, not TSQL text or semicolon batching. Use a rows-affected marker recognized by the execution builder, never a fabricated PostgreSQL @@ROWCOUNT/ROW_COUNT function. Do not confuse this initial toolkit restriction with a claim that PostgreSQL cannot execute multiple commands.

## Proposed M2 behavior

### Queries, expressions and capability failures

Select a declared useful query slice: parameterized filtering/projection/ordering, scalar/cardinality operations, aggregates and explicitly selected join shapes. Owner workload review fixes its concrete acceptance surface before production code. PostgreSQL Boolean predicates/literals, CASE, casts, string concatenation and selected date/string functions need dedicated formatting; unsupported functions and apply/association shapes reject before connection acquisition. No unsupported predicate may silently become an unfiltered query or hidden client filtering. PostgreSQL's planner may legitimately choose a sequential scan for supported SQL; this restriction concerns toolkit fallback, not native plan selection. Intentional existing client projection is separately declared; it does not authorize moving unsupported server filtering to the client.

PostgreSQL count returns bigint, integer sums widen, and most empty aggregates return NULL. Preserve LINQ cardinality, checked numeric/result conversion and declared empty/null semantics; do not inherit unchecked FieldReader narrowing or infer aggregate correctness from valid SQL. [Aggregate semantics](https://www.postgresql.org/docs/18/functions-aggregate.html), [comparisons](https://www.postgresql.org/docs/18/functions-comparison.html), [strings](https://www.postgresql.org/docs/18/functions-string.html).

Preview returns SQL and parameter names/types, supported shape or a stable capability reason without opening a connection. Pre-I/O validation covers known unsupported plan/type/converter shapes, malformed parameter metadata and invalid paging configuration. Backend-dependent permissions, schema changes, server failures and conversion facts unavailable until values arrive remain runtime errors, not promised static detection.

### CRUD and generated results

Select parameterized single-table insert/update/delete, generated-column omission and all-generated inserts using DEFAULT VALUES. Use RETURNING for selected generated keys/defaults and write-result projections, with explicit no-row/one-row/multiple-row outcomes. Preserve the projection's intended CLR type rather than special-casing integer identities. Do not infer generated keys through lastval/currval or perform an unsafe update-result requery. [RETURNING](https://www.postgresql.org/docs/18/dml-returning.html), [insert defaults](https://www.postgresql.org/docs/18/dml-insert.html).

RETURNING exposes inserted/new/deleted target rows and trigger-modified values. Identity generation alone does not establish uniqueness. Mapping must identify actual generated/identity behavior and key constraints; explicit identity override, composite/generated key combinations, generated non-key members and trigger/default values each require selected evidence or a precise pre-I/O exclusion. [Identity columns](https://www.postgresql.org/docs/18/ddl-identity-columns.html).

Multi-table mappings and cross-table write selectors are proposed initial deferrals. Legacy insert-or-update must not silently become ON CONFLICT with different conflict-target/concurrency semantics. Atomic upsert, MERGE and richer DML require later explicit capability contracts. Required M2 CRUD/generated-key evidence remains open even if these additional shapes are deferred.

### Parameters, identifiers and pagination

Bind data separately with declared PostgreSQL type metadata, including NULL, precision/range, JSON and array element types. Minimum compatibility proposal retains generated named placeholders: Npgsql supports these by parsing/rewrite. Native positional $n is preferred for a separately qualified path and needs one deterministic ordinal contract shared by formatting/binding. [Parameter behavior](https://www.npgsql.org/doc/basic-usage.html#parameters). [NamedValueGatherer](../IQToolkit.Data/Common/Translation/NamedValueGatherer.cs) currently uses a HashSet; its enumeration is not an ordinal contract. Repeated names must agree on metadata/value identity. Identifiers cannot be parameters, and values cannot be concatenated into SQL.

Quote identifier components with double quotes and doubled embedded quotes, preserving case. Distinguish schema qualification from literal dots inside names; reject unsupported ambiguous declarations rather than copy bracket/backtick logic or split every name blindly. Schema/search-path policy must be explicit. [Identifier rules](https://www.postgresql.org/docs/18/sql-syntax-lexical.html#SQL-SYNTAX-IDENTIFIERS), [schemas](https://www.postgresql.org/docs/18/ddl-schemas.html).

Emit LIMIT count OFFSET skip, supporting either independently. Do not copy MySQL/SQLite comma-limit syntax or SQL Server row-number lowering. Validate finite page bounds and nonnegative inputs; deterministic paging requires a unique order. Proposed initial keyset selection uses nonnullable sort keys, fixed directions and a unique tie-breaker. The cursor carries last sort values and query/policy identity; it does not promise a database snapshot across pages. Nullable sort keys, collation changes and snapshot pagination remain explicit proposed deferrals. [LIMIT/OFFSET](https://www.postgresql.org/docs/18/queries-limit.html), [NULL ordering](https://www.postgresql.org/docs/18/queries-order.html).

### Transactions, conflicts and uncertain writes

Use the same owned connection, explicit command transaction, sequential operations and defined borrowed/owned cleanup. Preserve sync compatibility. Proposed async scopes follow accepted D1 without Task.Run, blocking waits or synchronous executor fallback. Select explicit Read Committed support initially; stronger isolation requires its own declared evidence. Borrowed transaction participation does not authorize toolkit commit/rollback/disposal, and nested sync behavior must not be silently reinterpreted as the proposed async scope rules.

Conditional writes bind key and explicit expected-state/token predicates atomically; zero affected rows means no matching write, not automatically missing versus version conflict. Owner fixes that distinction and whether any authorized follow-up read is permitted. Preserve SQLSTATE/native exceptions separately from public redacted diagnostics. A failed transaction requires rollback or established savepoint recovery. Serialization/deadlock replay requires an explicitly replayable whole transaction; transport interruption after dispatch/commit may leave the write outcome unknown, so do not retry automatically or label it definitely rolled back. [Isolation](https://www.postgresql.org/docs/18/transaction-iso.html), [savepoint recovery](https://www.postgresql.org/docs/18/sql-rollback-to.html), [error codes](https://www.postgresql.org/docs/18/errcodes-appendix.html).

Savepoints, ambient/distributed transactions, automatic retries and native batch/COPY behavior are proposed initial exclusions, not evidence that PostgreSQL lacks those capabilities. A proposed budget cancels further dispatch and requests cleanup; it cannot establish the final outcome of an already dispatched write without backend evidence.

### Types, NULL and converters

Proposed scalar selection: Boolean, supported signed integers, floating point, bounded decimal/numeric, text, UUID, bytea, DateOnly, TimeOnly and explicitly distinguished timestamp/timestamptz. Define numeric overflow/precision and date/time range/resolution behavior. Npgsql10 defaults date/time GetValue results to DateOnly/TimeOnly. The generic basic-type table remains stale on those two default rows; prefer [versioned10.0 release notes](https://www.npgsql.org/doc/release-notes/10.0.html#date-and-time-are-now-mapped-to-dateonly-and-timeonly) and [date/time documentation](https://www.npgsql.org/doc/types/datetime.html), then qualify actual10.0.3 materialization.

UTC DateTime versus local/unspecified timestamp, zero-offset DateTimeOffset, microsecond precision, infinity conversions and out-of-range values need explicit mappings/exclusions. A timestamptz does not preserve a timezone identifier or arbitrary original offset. Do not install a global legacy timestamp switch or new NodaTime dependency. DateTimeOffset/interval/timezone-preserving models are proposed deferred until a complete typed-read/write contract is selected.

Proposed bounded array slice is one-dimensional arrays of selected scalar element types. Distinguish SQL NULL array, empty array and NULL elements; define bounds/rank exclusions. Proposed JSON slice is declared json/jsonb as string, with explicit parameter type and application-owned serialization. SQL NULL, JSON null and absent properties remain distinct; text extraction alone cannot preserve all distinctions. Dynamic POCO JSON, arbitrary nested LINQ, JSON mutation and multidimensional/nonstandard-lower-bound arrays are proposed deferrals. [Array/type mapping](https://www.npgsql.org/doc/types/basic.html#write-mappings), [JSON mapping](https://www.npgsql.org/doc/types/json.html), [JSON operators](https://www.postgresql.org/docs/18/functions-json.html).

Converters require model/storage types, bidirectional behavior, NULL policy, determinism, failure semantics and parameter/result integration. Do not assume arbitrary CLR conversion has a server translation. Known parameter conversion/validation failures precede I/O; returned-data conversion failure disposes resources while preserving the original failure. Proposed nullable metadata enforces declared nonnullable violations rather than defaulting them silently, while preserving existing-provider defaults outside the new selected contract. Immutable/record mapping must select constructors deterministically or reject ambiguity: BasicMapper currently chooses the first applicable constructor.

## Complete proposed M1q selection and deferral ledger

Every entry below remains proposed, not owner-approved. An accepted provider packet must record the actual workload, public semantics and reason for each deferral. A deferred shared feature remains in the roadmap and cannot be counted complete.

| LOCAL-RM inventory item | Proposed bounded selection or explicit deferral |
| :--- | :--- |
| MAPPING records/immutable constructors | Select deterministic constructor binding/projection and ambiguity failures. |
| MAPPING nullable metadata | Select SQL NULL mapping and explicit nonnullable violations in the new contract. |
| MAPPING value converters | Select declared storage/model conversion, NULL policy and failure/resource handling. |
| MAPPING modern date/time | Select DateOnly/TimeOnly and explicit DateTime modes; defer unselected ranges, intervals, DateTimeOffset and timezone-preserving models until typed semantics are fixed. |
| MAPPING JSON/arrays/missing/null | Select bounded string JSON and one-dimensional arrays; retain missing/null distinctions, defer nested query/mutation/dynamic mapping. |
| MAPPING tenant/soft-delete policies and bypass | Explicit proposed deferral pending owner isolation/policy selection; PostgreSQL support must not imply automatic enforcement. Any later selection includes raw/bypass and write-path isolation checks. |
| QUERY deterministic keyset/cursor pagination | Select bounded nonnullable keyset; defer nullable/collation/snapshot cursor extensions explicitly. |
| QUERY parameterized native escape hatches | Select typed command metadata/binding/ownership; raw/native access is an explicit policy-bypass surface. |
| QUERY previews/capability diagnostics | Select SQL plus parameter names/types and pre-I/O failure reasons; omit values. |
| QUERY conditional writes/optimistic concurrency | Select explicit predicate/token and affected-row outcome; owner fixes missing-versus-conflict semantics. |
| QUERY partial-failure batches/bulk | Explicit proposed initial deferral; retain per-item outcomes, failure/stop/retry/atomicity contracts for later selection. |

Existing sequential toolkit batch loops and NpgsqlBatch are not interchangeable. By default, NpgsqlBatch without an explicit transaction uses an implicit transaction; failure skips later statements and rolls the batch back. No partial-success or atomicity promise follows from an API named batch. [Batch semantics](https://www.npgsql.org/doc/basic-usage.html#batching). CTEs, window functions, richer database functions, source generation, NativeAOT and extensions such as pgvector remain follow-on capability work, not prerequisites silently discarded or new dependencies authorized here.

## Ownership, diagnostics and async qualifications

The current executor's LogParameters writes values, and ExecuteCommand(string) logs raw text. The PostgreSQL adapter needs redacted executor overrides/typed native access rather than inheriting these as safe defaults. Omit credentials, connection strings, parameter/document contents and sensitive native details; separately report operation/capability, SQLSTATE, duration, rows and correlation. Raw SQL itself may contain secrets; preview and native-operation diagnostics require explicit disclosure policy. No unrelated existing-provider logging rewrite is authorized by D2.

Propose an application-owned NpgsqlDataSource for modern independent scopes and explicit borrowed NpgsqlConnection compatibility. Do not infer independent connections from New(mapping/policy), which shares the existing connection. Keep multiplexing, ambient enlistment and distributed transactions outside the initial declared contract. The private Unix peer profile does not qualify remote authentication/TLS; any remote production profile requires its own safe configuration and evidence. [Data-source/transaction guidance](https://www.npgsql.org/doc/basic-usage.html#data-source), [security](https://www.npgsql.org/doc/security.html).

Exact-source classifications in [driver capabilities](modernization-driver-capabilities.md) remain operation-specific. Open/command/reader async overrides select async paths; queued BEGIN is not a roundtrip proof. ReleaseAsync selects synchronous execution, and cancellation includes synchronous side-channel work. Buffered read completion is valid, not pending-I/O evidence. Retain sync compatibility; no blanket fully nonblocking lifecycle promise or driver interchangeability follows.

## Source facts versus observed readiness

[P0-P2](modernization-postgresql-probe.md) establish exact native build, peer/Unix read-only catalogs, three-package cold locked build, typed metadata/acquired-OID parameter and loaded-driver comparisons. [P3](modernization-postgresql-cancellation-probe.md) separately observes active ExecuteReaderAsync acquisition cancellation, original token/native-user57014 error, same-backend catalog recovery and cleanup. P3's documented post-await deadline-check limitation remains historical; it does not certify pending ReadAsync, memory, performance or all async lifecycle operations.

At D2 save time, [P4](modernization-postgresql-row-read-probe.md) has **terminal native acquisition PASS only**. Read-back of `artifacts/postgresql-row-read/native.qzwg05c9/result.json` records 2519 actual catalog rows, unchanged checkout, server terminal exit 0, no remaining owned native processes and socket removal. Actual native metadata identifies 18.6/aarch64, peer/Unix/read-only transport and statement_timeout 10s. This acquired native row/plan context is not a driver ReadAsync result. P4 SDK/live pending-row cancellation remains unobserved here; subsequent results belong in root's reviewed packet before this boundary changes.

Read-only empirical probes may settle translation/type/ownership questions using acquired actual schemas/rows and native-versus-driver comparisons. **CRUD, generated values, conditional conflicts and write transaction durability cannot be established by catalog reads.** They require an owner-authorized data/acquisition packet. Rollback does not make invented fixture/schema creation implicitly authorized. No current P0-P4 result demonstrates a production IQToolkit PostgreSQL provider.

## Owner decisions and empirical gates

Record decisions once in root's plan; do not re-ask resolved questions. Still open are D1/public execution acceptance, the useful query/mapping/type slice and explicit deferrals, nullable/converter/cursor/conflict semantics, authorized actual CRUD data/fixture policy, coverage requirements/any properly scoped exception, supported environment/authentication profiles and future public package/version policy. Proposed class organization and internal code choices remain reversible engineering decisions after those contracts are fixed.

Empirical gates independently settle SQL translation/result types, typed reads/conversions, generated-result shape, native conflict/transaction recovery, paging/null ordering, pending row reads, resource ownership and complete consumer provenance. Probe outcomes cannot substitute for consequential owner/data decisions. A new optional public capability calls for a reviewed minor-version policy under DOTNET-API-003; released versions stay immutable, and no package version/tag/publication is selected here.

## Future implementation acceptance and consumer proof

1. Accepted D1/SQL contract, complete M0/M1 prerequisites, explicit workload/type selections/deferrals and authorized integration data before production dispatch.
2. Actual PostgreSQL dialect/query, CRUD, generated-key/default, transaction commit/rollback/error, paging, parameter/type/null/converter and unsupported-before-I/O evidence. Selected tests assert meaningful native outcomes, not formatter implementation mirrors.
3. Preserved synchronous behavior and separate operation-specific async ownership/cancellation evidence, including genuinely pending row reads, early exit/error cleanup and explicit materialization versus streaming. Complete M1 inventory remains required beyond any first slice.
4. A fresh outside-checkout consumer with **one sole direct actual adapter-package reference**, no ProjectRefs/repository-bin DLLs/new runtime helpers. Compile actual proposed public provider/language symbols and transitive Npgsql symbols, then run selected real native operations independently of production projects.
5. Deliberate lock preparation/review followed by unchanged locked fresh-cold restore and strict --no-restore build. Validate the full toolkit+Npgsql+configuration/transitive graph, dependency edges, archive/cache/feed/lock identities, source mapping/audit and original-input stability. Do not reuse the probe's three-node graph as the adapter graph or retry a locked failure unlocked. Use nondiscoverable project templates/snapshots and outside-checkout .csproj files to avoid ambient IDE restoration and inherited CPM/imports.
6. Existing-provider API/regression gates, strict build/package checks, scoped coverage obligations, exact archive/source/native/runtime provenance, ATLAS/disposition and independent integrated review. Emulator/protocol similarity or Linux package presence cannot certify another engine/platform.

## Revision history

| Version | Date | Change |
| :--- | :--- | :--- |
| 0.1 | 2026-10-03 | Initial source-grounded D2 proposal; complete M2/M1q inventory, exact10.0.3/18.6 context and scoped P3/P4-native evidence; no implementation or owner acceptance. |
