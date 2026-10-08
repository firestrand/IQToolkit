---
title: DuckDB ADO catalog comparison probe
version: 0.2
last_updated: 2026-10-03
status: ADO PASS; integrated Oracle-clear
owner: IQToolkit project owner
tags: [modernization, duckdb, ado, probe]
---

# DuckDB ADO catalog comparison probe

Q0.4 measures the pinned driver's behavior against the independently acquired native catalog. It enables required LOCAL-RM-M5. The [SDK checkpoint](modernization-duckdb-sdk-hardness.md) passed the reviewed four-package graph and strict build; the [native checkpoint](modernization-duckdb-native-hardness.md) supplied genuine metadata. Production M5, public D1/D4 and all unanswered owner decisions retain their original dependencies.

## GOTCHA and execution contract

**Goals/objectives:** Prove that a privately compiled DuckDB.NET 1.5.6 consumer resolves the exact verified ARM64 native library and returns the six acquired query results unchanged, including column names, primitive types, order, multiplicity and nine SQL NULL cells. Check a named string parameter using an unchanged actual catalog name. Record reader closure, completed command disposal and Closed connection state before process exit.

**Tasks:** Save project/program/runner and immutable source → prospective SDK/source/data Oracle → one locked restore/build using the proven controlled cache → observed build review → seal the actual compiled inputs and separately review runtime → one bounded ADO comparison → observed review → owned disposition/ATLAS/new production protection/integrated review. Failure is terminal for that stage; no retry, literal parameter fallback or relaxed equality.

**Capabilities/ownership/users:** Maintainer-only Technical Probe. Root owns writes, shared plan/state, SDK and runtime. Two source workers and Oracle remain read-only, maximum three workers, no recursive delegation. No production source/API/dependency change, service, credentials, user data, generated provider payload, installation, publication or paid resource.

**Health/constraints:** SDK version 15 seconds, restore/build 120 seconds each; runtime 30 seconds, child elapsed checks 25 seconds; owned group termination/reaping at most five additional seconds. Logs/reports each ≤1 MiB, result ≤1,024 rows/16 columns/4 KiB UTF-8 cells; combined DuckDB evidence area and app <2 GiB. These are monitored rejection thresholds with possible overshoot, not hard allocation/RSS caps. Synchronous calls require the independent parent deadline. Core dumps disabled; source hashing/traversal outside command budgets. No ordinary writers during before/after checkout snapshots.

**Attributes/runtime/beliefs/intentions:** New private owned0700 app/evidence paths are fixed in workspace.json. Reuse the exact reviewed SDK graph/lock, SDK 10.0.401/net10.0/Release/framework-dependent/no apphost/no RID profile. This restore is cache-backed, not another cold-cache claim: controlled cache provenance is checked before and after, with fresh private audit/HTTP/scratch/tmp paths and unchanged signature policy. Build uses --no-restore and zero-warning enforcement. Preserve every original failed/passed checkpoint and historical inspector. Disposable source becomes nonactive snapshots after terminal review; remove only the owned app/helper and record cache retention/cleanup.

**Authority:** Selected Cross-Platform .NET Standard 1.1.0-draft.1, SHA256 `8c56930ec1cd26e8b58a273d47cb0b9713f55e6d4961b6d29a636ffaf4f0ebe2`; DEP-002/003/004, PROC-001, FS-001/002, SEC-002 and platform boundaries apply. Development Plan Guide 2.7 DP-05/DP-09 and Technical Probe rules, autonomous, agent-tooling and Technical Markdown guides govern scope and evidence. No full-standard compliance claim.

## Actual data and comparison

Only the native actual.json supplies expected provider columns/rows and exact SQL: controls first, then version, platform, settings, DESCRIBE and count. Retain actual strings and SQL NULLs, compare typed GetBoolean/GetInt64/GetString with GetValue/IsDBNull and the independent JSON values, and verify CLR type/native enum mapping. UseStreamingMode is enabled; the small catalog does not establish large-result bounded memory or actual streaming for every statement.

The optional parameter query selects name/input_type/scope by `$selected_name`; parameter name is bare selected_name. Its value is the first acquired catalog name, unchanged; set Value before DbType.String. Expected rows are the acquired catalog filtered by that value, preserving multiplicity. This proves a narrow ADO named-string result against native data, not independent C API parameter-binding equivalence or a general parameter contract. No generated table, fixture or error response is used.

Raw configuration preserves `DataSource=:memory:;temp_directory='';threads=1;memory_limit=256MiB`, followed by disabled extension installation/loading, persistent secrets, unsigned/community extensions and external access, with configuration locked last. Never round-trip the builder or enumerate its descriptors. The driver maps ordinary :memory: to native NULL and automatically adds duckdb_api; complete startup options differ from the native run. The six comparison queries omit that label's value. Allowed-list lengths must still be zero. The native engine is not READ_ONLY and `256.0 MiB` is rounded consistency, not exact runtime bytes or a process cap.

## Native resolution and ownership

Hash actual copied managed/native files before use. Register a resolver for the bindings assembly; permit only its duckdb import and load the exact copied linux-arm64 native path once. Trigger ServerVersion, then verify the process's actual native map before opening a connection. Any registration, path/hash or identity mismatch fails before catalog access. Do not retain unrelated host mappings or ambient diagnostics.

This resolver intercepts the third-party bindings assembly as an explicit private control; it does not certify ordinary package/native resolution. The module handle intentionally lives until child exit: cached P/Invoke pointers and possible late SafeHandle finalizers make eager NativeLibrary.Free unsafe. This is process-lifetime module ownership, not a claim that a manual unload/destructor occurred. Readers, commands and connection are separately disposed before normal child exit. Record cleanup errors independently without replacing the original operation failure. Reader IsClosed, connection Closed, the captured borrowed native handle IsClosed and completed Dispose calls establish only their named observations; no general leak-free proof follows.

## Remaining gates

Actual compiled output must be reviewed/sealed before runtime. Success establishes catalog/primitive/named-string feasibility on the observed Linux ARM64 host. Genuine async, cancellation, large-result streaming, write/transaction/type coverage, import/file security, adapter implementation, support promotion and complete roadmap delivery remain open. Final scoped closeout requires authentic observations or truthful failure, retained inputs/outputs, owned disposition, ATLAS and integrated Oracle review.

## Observed checkpoint

SDK review CLEAR bound63inputs/59snapshots; locked cache-backed restore/buildPASS with zero warnings/errors and unchanged51cachefiles/checkout/source. Actualcompiled review CLEAR bound81inputs/66snapshots/13outputs. The single boundedADO childPASS: pinnedlibrary mapped beforeOpen, allsixnative results unchanged159catalogrows/3schemarows/9nullcells/13controls/version/platform/count; namedparameterCalendar yields unchangedactualrow. Allseven readersclosed/commandDisposecompleted; connectionClosed and borrowednativehandleIsClosed. Parentwholechild0.327830992seconds/withgroupchecks0.356426407seconds, innerpostcleanup/prepublication0.1847586seconds; exit0/noownedgroups/cancel/forcedtermination/cleanup/publicationerrors. Observed OracleCLEAR; source/cache/checkoutstable, appaddedonlyreport.

The executed runner/ten source-input files and actualreport are retained as nonactive snapshots. Thirteen compiled outputs moved by same-filesystem rename, preventing duplicate-copy budget pressure; owned outsideapp removed. Historical SDK/native inspectors and all original failed/passed evidence are unchanged. [ADO ATLAS](modernization-duckdb-ado-hardness.md) records exact observations and limitations; protection PASS123 source hashes/137 unchanged production inputs/12 archive-payload identities/336 binaries, integrated Oracle CLEAR for this private ADO checkpoint only. Ordinary SDK native resolution still requires a separately reviewed experiment; this explicit resolver run cannot qualify it.
