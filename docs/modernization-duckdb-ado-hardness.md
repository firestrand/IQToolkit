---
title: DuckDB ADO catalog comparison hardness report
version: 0.1
last_updated: 2026-10-03
status: Private ADO checkpoint integrated Oracle-clear; broader gates open
owner: IQToolkit project owner
tags: [modernization, duckdb, ado, atlas]
---

# DuckDB ADO catalog comparison hardness report

The [Q0.4 probe](modernization-duckdb-ado-probe.md) passed its one ADO comparison against the independently acquired native catalog. The actual copied library was mapped before database opening under an explicit private resolver. All six native results matched; a named string parameter returned the unchanged acquired `Calendar` row. This is finite metadata feasibility evidence for LOCAL-RM-M5, with production/provider/support gates still open.

## Edge cases and observed behavior

The control result matched all 13 typed nonnull columns before later queries. Version v1.5.6, platform linux_arm64, all 159 ordered settings rows, three DESCRIBE rows with nine genuine SQL NULL cells and independent count159 matched the native observations. Primitive native enum IDs1/5/17 and CLR bool/long/string matched; typed getters were cross-checked against GetValue/IsDBNull. No row normalization, deduplication, authored provider payload or literal-parameter fallback.

The selected parameter value came unchanged from the acquired native catalog. Its narrow string result matches filtered native data, without an independent C API parameter oracle. Query order and multiplicity were preserved. SQL DESCRIBE was used directly; GetSchemaTable's different metadata semantics were not substituted.

All seven readers recorded IsClosed after disposal; command Dispose calls completed before the next query. Connection State and its captured borrowed native SafeHandle both recorded closed after connection disposal. The borrowed handle was not disposed independently. These are named managed state/cleanup observations, not a native allocation census or general error/abandonment guarantee.

The resolver recorded 38 requests and loaded the reviewed 62,945,312-byte native payload SHA256 `23686593dba4ec22c6e581bb1c843b3964e84c4bc4d0063d8b4daddd2e20e30a`; actual map/path/hash/version prerequisites passed before Open. It deliberately intercepts a third-party assembly for this private experiment. Ordinary SDK native resolution is still unqualified. The module handle lived until successful child exit; no unsafe eager Free, manual unload or destructor observation is claimed.

## Tool failures, concurrency and security

SDK preparation used the exact reviewed four-package lock SHA256 `1867c8d9f292a6fe2b4c075b169ba2dd40f6e95dcc1045dcd851711af10f47f2` and proven 51-file cache. Cache bytes remained unchanged through SDK and runtime. Fresh tooling/audit caches and actual assets retained audit enabled/all/low with empty diagnostics. Cache reuse is not a new cold extraction or fresh signature-validation proof.

Prospective SDK review bound 63 inputs/59 snapshots; compiled runtime review bound 81 inputs/66 snapshots and 13 actual output files. All input and ordinary checkout hashes remained unchanged in both stages; the runtime app added only actual.json. All four commands exited zero, without cancellation, forced termination, live group members, cleanup or terminal publication errors. The child PID was absent at disposition. Workers reviewed read-only; root serialized compilation and execution.

Fixed command/runtime, output/cell/cardinality and private storage bounds were checked prospectively and terminally. Polling can overshoot and supplies no hard disk/RSS quota. The child's 25-second elapsed check ends before report serialization; the parent separately bounds the complete child at30seconds plus≤5seconds owned cleanup. CommandTimeout and streaming flags do not establish enforcement or actual streaming for each statement.

Private owned directories, restrictive creation, controlled direct tools/environment, explicit config/lock/mapping, disabled build servers and no inherited credentials were retained. External access/extensions/persistent secrets were disabled and configuration locked before catalog queries, as confirmed by actual controls. Native memory rendering remains rounded consistency, not exact runtime bytes/process memory. In-memory READ_ONLY is not used; engine/platform initialization reads remain possible. No service, account, installation, user records, file import or production dependency change.

## Verification and disposition

Historical invocations, each run once after its own prospective Oracle CLEAR:

```text
/usr/bin/python3 -I artifacts/duckdb-probe/ado.iw8qxwoa/ado-probe.py sdk
/usr/bin/python3 -I artifacts/duckdb-probe/ado.iw8qxwoa/ado-probe.py runtime
```

Locked cache-backed restore: exit0/1.298975406seconds. Strict Release build: exit0/1.413227634seconds/zero warnings/errors. ADO child complete operation: exit0/0.327830992seconds, including ownership checks0.356426407seconds; inner post-cleanup/pre-publication elapsed0.1847586seconds. These are one observed process timing, not latency/performance guarantees.

Evidence resides under artifacts/duckdb-probe/ado.iw8qxwoa. Observed Oracle CLEAR independently checked the rows/types/NULLs, actual parameter, provenance, owned cleanup and stable inputs. Ten source/input snapshots and unchanged actual report are retained. All 13 outputs moved through an owned same-filesystem rename, avoiding a duplicate-copy storage peak. The outside app was removed and runner renamed to a nonactive snapshot. The previous controlled cache stays owned offline for a later reviewed packet or goal closeout. Original SDK/native/P4 outcomes and historical inspectors are preserved.

## Remaining obligations

The new protection inspector passed123 source hashes,137 unchanged production inputs,12 archive/payload identities and336 ordinary binaries; integrated Oracle review is CLEAR for this checkpoint only. The next independent readiness question is ordinary SDK native resolution, separately expanded and reviewed; this run qualifies only explicit private resolution. Async/cancellation, large results, transactions/writes, broader types/parameters, file/import policy, real adapter behavior, public acceptance, coverage/support matrices and the complete roadmap remain unimplemented or unqualified as recorded in the plan. No required gate is waived by this probe.
