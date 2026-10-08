---
title: DuckDB analytical capability contract proposal
version: 0.1
last_updated: 2026-10-03
status: Proposed; owner decisions and production gates open
owner: IQToolkit project owner
tags: [modernization, duckdb, analytics, capability, contract]
---

# DuckDB analytical capability contract proposal

D4 makes required `LOCAL-RM-M5` in the [roadmap](modernization-roadmap.md) and [implementation plan](modernization-implementation-plan.md) concrete for owner review. The proposed optional adapter supports analytical queries, explicit streaming, configured file sources and bounded import. Public names, workload selections, dependencies and security profiles below are proposals. This document neither implements an adapter nor accepts those decisions.

Production M5 still requires M0/M1, the shared SQL capability design gate, approved actual data and coverage decisions. The [D1 execution contract](modernization-execution-contract.md) remains unchanged. Independent [Q0 readiness preparation](modernization-duckdb-probe.md) can investigate native feasibility without approving production work. Q0.1 terminated FAILED at its fixed output-size guard; its archive observations do not prove native loading, ADO equivalence or support. PostgreSQL P4 also remains FAILED and supplies no DuckDB async evidence.

## Standards and evidence boundary

The selected standard is `ENG-STD-DOTNET-XPLAT` 1.1.0-draft.1, retained in the [matching repository snapshot](standards/source/Cross-Platform-DotNet-Engineering-Standard-1.1.0-draft.1.md), SHA256 `8c56930ec1cd26e8b58a273d47cb0b9713f55e6d4961b6d29a636ffaf4f0ebe2`. Dependency boundaries and strict new-code controls (`DOTNET-REPO-003/004`), central/locked/mapped/audited packages (`DOTNET-DEP-001/002/003/004`), public compatibility (`DOTNET-API-001/003`), tests (`DOTNET-TEST-001`), file/process/security controls and declared platform support apply. No whole-standard compliance claim follows.

The Development Plan Creation Guide 2.7 supplies requirement traceability, DP-05 actual-data authorization and DP-09 decision authority. Its Technical Probe rules and the autonomous guide preserve independent preparation and the full objective. The Technical Markdown Style Guide governs this document. Confirmed source, proposed toolkit behavior and unverified runtime behavior are distinguished throughout. Root retains exact source manifests; the read-only design worker independently checked the 21 successful initial snapshots.

## GOTCHA specification

**Goals:** Define an optional embedded analytical capability useful for approved reporting workloads without promising interchangeable OLTP semantics.

**Objectives:** Cover reads, projections, aggregates, parameters, streaming, files and bounded import; give every M1q requirement an explicit selection or deferral; preserve existing synchronous APIs and D1's genuine async contract.

**Tasks:** Review the workload/public decisions; acquire native and driver evidence independently; obtain actual data and coverage decisions; expand reviewed failing-test/implementation packets; verify packaging and each supported architecture. Termination of this D4 packet means a source-grounded, independently reviewed proposal, not implementation completion.

**Capabilities and constraints:** Existing translation, mapping, policy and execution seams; exact DuckDB.NET/engine sources; root-owned Q0 provenance. No production dispatch before its dependencies, fabricated rows/schema, implicit network/filesystem/extension access, paid resources or fake async wrappers.

**Health:** Every future empirical packet fixes inputs, output/storage/time bounds, ownership, interruption, failure disposition and cleanup before execution. Application row limits do not bound blocking engine work or process memory. No owner token/cost budget was supplied.

**Attributes and users:** An additive optional 1.x preview proposal for maintainers and selected reporting applications; exact support follows observed engine, driver, OS and architecture evidence.

**Runtime:** Preparation persists immutable sources and decisions. Disposable experiments remain outside production. Unexpected empirical outcomes stop that packet and trigger a reviewed replan; unanswered owner decisions stay open.

**Beliefs and intentions:** Sources establish plausible translation and synchronous analytical seams. Actual execution, import atomicity, security controls and large-result behavior remain unverified. Preserve the full M5 and support obligations while investigating them.

## Exact dependency context

The root-selected private candidate graph is not an approved production dependency graph:

| Package | Exact version | Applicable dependencies | License |
| :--- | :--- | :--- | :--- |
| DuckDB.NET.Data.Full | 1.5.6 | net10: Bindings.Full >=1.5.6; Arrow >=23.0.0 | MIT |
| DuckDB.NET.Bindings.Full | 1.5.6 | net10: none | MIT |
| Apache.Arrow | 23.0.0 | nearest net8: Scalars >=23.0.0 | Apache-2.0 |
| Apache.Arrow.Scalars | 23.0.0 | nearest net8: none | Apache-2.0 |

Official [Data.Full](https://api.nuget.org/v3/catalog0/data/2026.09.28.22.51.16/duckdb.net.data.full.1.5.6.json), [Bindings.Full](https://api.nuget.org/v3/catalog0/data/2026.09.28.22.54.09/duckdb.net.bindings.full.1.5.6.json), [Arrow](https://api.nuget.org/v3/catalog0/data/2026.05.06.04.19.21/apache.arrow.23.0.0.json) and [Scalars](https://api.nuget.org/v3/catalog0/data/2026.05.06.04.20.33/apache.arrow.scalars.23.0.0.json) catalogs declare these groups. Client commit is `e28f086d7bc0abe26353b0ef4185d8ded1bdc917`; Arrow commit is `79e3844aca811abb3f9c463c0c27d407723e13be`.

The pinned [Bindings project](https://github.com/Giorgi/DuckDB.NET/blob/e28f086d7bc0abe26353b0ef4185d8ded1bdc917/DuckDB.NET.Bindings/Bindings.csproj) selects engine 1.5.6 asset URLs, including glibc Linux ARM64/x64. Q0 separately retains immutable engine source snapshots at commit `069cc9f9b5be802405797faecc284961b07c70ef`; the loaded library's version/source ID remains to be observed. Q0 metadata, full-archive hashes and source manifests are retained under `artifacts/duckdb-probe/`. Archive signature-entry presence is not signature validation. Native licensing/notices, NuGet trust/audit, consumer locks and native runtime/platform verification remain distinct obligations.

## Adapter and repository seams

Proposed names are optional package `IQToolkit.Data.DuckDB`, `DuckDbQueryProvider`, `DuckDbLanguage`, `DuckDbFormatter`, `DuckDbTypeSystem` and separate streaming/import interfaces. Vendor and Arrow types stay behind optional package boundaries.

| Confirmed repository seam | Proposed change |
| :--- | :--- |
| [QueryTranslator](../IQToolkit.Data/Common/QueryTranslator.cs) mapping/policy/language pipeline | Declare supported expressions and reject unsupported commands before connection acquisition. |
| [QueryLanguage](../IQToolkit.Data/Common/Language/QueryLanguage.cs) and [SqlFormatter](../IQToolkit.Data/Common/Language/SqlFormatter.cs) | Dedicated quoting, type/scalar rules, functions, booleans, aggregation, parameters and LIMIT/OFFSET. Do not inherit TOP or another provider's comma-LIMIT syntax. |
| [EntityProvider](../IQToolkit.Data/EntityProvider.cs) and [ExecutionBuilder](../IQToolkit.Data/Common/ExecutionBuilder.cs) | Explicit execution-mode dispatch. ExecutionBuilder's private constructor prevents treating visitor subclassing alone as a usable extension. Shared hooks must remain additive. |
| [DbEntityProvider](../IQToolkit.Data/DbEntityProvider.cs) | Dedicated executor, typed parameters and connection/command/reader ownership. |
| [BasicMapper](../IQToolkit.Data/Common/Mapping/BasicMapper.cs) and [FieldReader](../IQToolkit.Data/Common/FieldReader.cs) | Deterministic constructor/converter selection and strict null/target-type materialization. Existing default conversions cannot establish all proposed type semantics. |
| BasicMapper DML lowering and DbEntityProvider transaction entry | Reject ordinary inserts/updates/deletes and unsupported transaction requests before I/O; route bounded import through a distinct capability. |

Existing synchronous subclasses and callers retain compatibility. No new abstract async or analytical obligations are added to legacy `QueryExecutor`.

## Query and parameter behavior

Propose flat projections, filters, explicit ordering, selected joins, grouping and existing Count/LongCount/Sum/Min/Max/Average families. Define empty/all-null results, overflow, DISTINCT and cardinality individually. Engine expression support does not establish LINQ compatibility. The pinned [SELECT transformation](https://github.com/duckdb/duckdb/blob/069cc9f9b5be802405797faecc284961b07c70ef/src/parser/transform/statement/transform_select_node.cpp) supports ORDER BY, LIMIT/OFFSET, GROUP BY and HAVING; toolkit lowering/result semantics still need actual tests.

Propose deterministic generated named parameters with explicit logical types, especially for null, decimal, date and list values. Reject missing, duplicate, extra and mixed-mode parameters. The pinned driver checks a count lower bound and skips unsuccessful named-index lookups; that binder does not establish strict toolkit binding. `Prepare()` is a no-op, so preparation caching is not promised. [PreparedStatement source](https://github.com/Giorgi/DuckDB.NET/blob/e28f086d7bc0abe26353b0ef4185d8ded1bdc917/DuckDB.NET.Data/PreparedStatement/PreparedStatement.cs).

Preview returns placeholders and capability diagnostics without values or execution. A parameterized native escape is an explicit policy-bypass surface restricted to approved operations. Initially exclude multi-statement execution: the driver processes statements sequentially. A SQL-prefix check cannot safely classify arbitrary native commands. Identifier case collisions and date/string functions require exact-version qualification.

## Streaming, budgets and async

Materialized and streaming reads are separate capabilities. Proposed streaming owns a connection/command/reader lease throughout enumeration, disposes on completion/error/early enumerator disposal, forbids overlap on that connection and sets `UseStreamingMode=true`. Preserve D1's explicit exclusion of immediate cleanup for a manually acquired enumerator abandoned without disposal. Verify the actual native streaming result. This flag alone is insufficient: current DbEntityProvider.Execute calls `ToList()` when the toolkit opened the connection. ExecuteDeferred is a candidate synchronous iterator seam whose projection/composition paths still need validation.

Budgets distinguish returned rows/bytes, retained batches, elapsed time, process memory and temporary storage. Sorts, joins and aggregation may block before the first row. The engine's memory limit is not a complete process-memory bound. [Pinned settings implementation](https://github.com/duckdb/duckdb/blob/069cc9f9b5be802405797faecc284961b07c70ef/src/main/settings/custom_settings.cpp).

CommandTimeout has no observed enforcement in the command implementation; Cancel interrupts the connection. A watchdog/interrupt design needs independent evidence of a pending operation, settlement, cleanup and connection reuse. [DuckDBCommand source](https://github.com/Giorgi/DuckDB.NET/blob/e28f086d7bc0abe26353b0ef4185d8ded1bdc917/DuckDB.NET.Data/DuckDBCommand.cs).

**D1 remains unchanged.** Pinned connection, command, reader and transaction paths do not establish genuinely asynchronous lifecycle operations. Arrow's async-named fetch checks cancellation before synchronous native fetch and returns a completed ValueTask; this does not establish cancellation during the fetch. [Arrow stream source](https://github.com/Giorgi/DuckDB.NET/blob/e28f086d7bc0abe26353b0ef4185d8ded1bdc917/DuckDB.NET.Data/Arrow/DuckDBArrowArrayStream.cs).

Under D1's proposed unsupported-provider contract, initial DuckDB async requests therefore fail before acquisition. Task.Run, completed tasks and synchronous iterators cannot qualify M1. A future native execution alternative requires a separate reviewed contract and empirical packet.

## Types, mapping and Arrow

Propose a selected scalar matrix for numeric widths, representable decimals, strings, booleans, UUIDs, binary values, finite dates/times/timestamps and SQL nulls. Specify overflow, precision, timezone behavior and unsupported infinite values.

Driver support for DateOnly/TimeOnly and vendor temporal representations does not establish shared toolkit conversion or interval-to-TimeSpan equivalence. [Date/time reader](https://github.com/Giorgi/DuckDB.NET/blob/e28f086d7bc0abe26353b0ef4185d8ded1bdc917/DuckDB.NET.Data/DataChunk/Reader/DateTimeVectorDataReader.cs). High-scale DECIMAL conversion can truncate beyond System.Decimal's supported scale. Reject lossy mapping or require an explicit exact converter/representation; DECIMAL(38,…) is not universally lossless as CLR decimal. [Decimal reader](https://github.com/Giorgi/DuckDB.NET/blob/e28f086d7bc0abe26353b0ef4185d8ded1bdc917/DuckDB.NET.Data/DataChunk/Reader/DecimalVectorDataReader.cs).

Select JSON text/converters and one-dimensional scalar lists only when approved workload data exists. Distinguish SQL NULL, JSON null, absent properties, empty lists and null elements. Defer arbitrary STRUCT/MAP/UNION/recursive LINQ translation. Native nested readers do not prove shared mapping behavior.

Arrow remains a proposed optional extension with explicit schema, per-batch ownership/disposal and bounded retained batches. Defer its public API initially while retaining investigation. No zero-copy or genuine async promise follows from interface presence.

## Files, extensions and import

Propose separate profiles for isolated metadata memory, approved existing embedded files and approved owned file sources. Existing-file reporting requests explicit read-only access. Engine initialization rejects in-memory READ_ONLY; catalog-only memory is not an enforced read-only database. [Storage initialization](https://github.com/duckdb/duckdb/blob/069cc9f9b5be802405797faecc284961b07c70ef/src/storage/storage_manager.cpp).

File-source configuration identifies approved format/schema/path/hash and operation limits. Reject query-derived paths, wildcards, URLs and implicit extension installation. Parquet is the proposed first import format, pending owner approval and exact engine/path-binding/runtime qualification.

Source hazards require explicit qualification:

- Driver file/shared-memory cache keys use case-insensitive filenames and exclude configuration. Later same-path connections can reuse earlier settings; case-distinct Linux paths can alias.
- Builder enumeration initializes native settings. The builder excludes VARCHAR[] options; engine path allowlists require an initialized filesystem and cannot change after external access is disabled.
- Disabling external access can retain attached database/WAL paths and the configured temp directory as allowed access. It alone does not prove zero filesystem writes.
- Empty temp-directory serialization/reparsing and setting order need effective-setting verification. Q0's memory proposal passes raw quoted emptiness before disabling external access.

Sources: [ConnectionManager](https://github.com/Giorgi/DuckDB.NET/blob/e28f086d7bc0abe26353b0ef4185d8ded1bdc917/DuckDB.NET.Data/Connection/ConnectionManager.cs), [builder](https://github.com/Giorgi/DuckDB.NET/blob/e28f086d7bc0abe26353b0ef4185d8ded1bdc917/DuckDB.NET.Data/DuckDBConnectionStringBuilder.cs) and pinned engine settings above.

Require an owned connection/database identity, trusted bootstrap before untrusted queries, effective access/allowlist/spill verification and rejection of conflicting configuration reuse. Extension access needs its own reviewed list of exact compatible artifacts. Initially exclude automatic installation/loading, community/unsigned extensions, secrets and remote access.

**Bounded import remains required M5 scope.** Propose append into an approved target using fixed schema/converters and limits on files, input/output bytes, rows, elapsed time and temporary storage. Require immutable owned input or qualified protection against replacement between validation and engine access. Path hashing alone does not eliminate that race.

An explicitly owned transaction requires actual atomicity tests. Driver isolation accepts Snapshot/Unspecified; the toolkit's default ReadCommitted needs an explicit adapter decision and pre-I/O rejection of unsupported levels. [Transaction source](https://github.com/Giorgi/DuckDB.NET/blob/e28f086d7bc0abe26353b0ef4185d8ded1bdc917/DuckDB.NET.Data/DuckDBTransaction.cs).

Distinguish known committed, known rolled-back and uncertain outcomes. Do not automatically replay after uncertain commit. Per-file/multi-file partial results do not imply atomicity. Defer replace/upsert, directory IMPORT DATABASE, generic bulk/appender promises and unrestricted SQL file operations.

## Complete proposed M1q ledger

| Requirement | Proposed selection or deferral |
| :--- | :--- |
| Records/immutable constructors | Select bounded immutable projection, deterministic constructor binding and failures. |
| Nullable metadata | Select SQL-null compatibility and invalid nonnullable-target failures. |
| Explicit converters | Select declared scalar/JSON/list conversions with loss and exception rules. |
| Modern date/time | Select finite DateOnly/TimeOnly/timestamps; defer unsupported infinite/interval semantics. |
| JSON/arrays/missing/null | Select explicit JSON text/converters and scalar lists when data approved; defer general nested translation. |
| Tenant policy | Defer initial analytics workload; document exclusion and native/file bypass. |
| Soft delete and bypass | Defer policy slice; classify bypass surfaces explicitly. |
| Deterministic keyset/cursor paging | Select approved unique order and source/query/config identity; define null ordering and source-mutation invalidation. Do not assume native continuation tokens. |
| Parameterized native escape | Select one approved statement, typed parameters and explicit bypass/control contract. |
| Query preview/capability failures | Select nonexecuting previews and unsupported-before-I/O behavior. |
| Conditional writes | Defer generic analytical updates. |
| Optimistic concurrency | Defer generic row writes; import/source identity checks are not optimistic CRUD. |
| Partial-failure batches/bulk | Select bounded import outcomes; defer generic batches/bulk APIs. |

CTEs, windows, richer aggregates, database-specific functions, source generation and NativeAOT remain separately selected follow-ons. ClickHouse remains conditional on an owner-selected remote workload, with independent visibility and partial-ingestion semantics. These deferrals do not remove any adopted roadmap outcome.

## Gates and completion evidence

Owner decisions remain open for public names/shapes, workload, query/type slices, import format/atomicity, file-security profile, Arrow and coverage. No acceptance is inferred from this design or an Oracle clarity verdict.

Production needs M0/M1, the reviewed shared SQL gate, actual approved inputs and coverage policy. Verify reads/aggregates/parameters, type/null/converter behavior, deterministic paging, early stream disposal, bounded large results, import success/failure/uncertain outcomes and denied file/extension operations against exact versions.

Require actual native evidence on **ARM64 and x64**. Neither RID presence nor one architecture certifies another. Other operating systems/musl remain explicit exclusions until qualified.

The independent consumer directly references only the actual new adapter package, exercises public symbols and real operations, and verifies the complete toolkit/driver/Arrow/native transitive graph through reviewed locks, source mapping and cold strict consumption. Preserve the existing six-package compatibility checks. Source templates remain nondiscoverable until isolated execution.

Support documentation includes experimental/supported/legacy status, exact version/OS/architecture matrix, runnable approved-data examples, migration/exclusions, separately reported integration skips and owner promotion. Git tags do not establish NuGet publication.

D4 completion means a complete independently reviewed proposal with decisions and source qualifications retained. Implementation completion additionally requires direct runtime evidence, ATLAS and integrated review. Neither gate closes M5 or the full roadmap alone.

## Changelog

| Version | Date | Change |
| :--- | :--- | :--- |
| 0.1 | 2026-10-03 | Proposed complete analytical/query/import/type/security/streaming contract and M1q ledger; source-qualified synchronous limitation and open production gates. |
