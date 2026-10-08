---
title: DuckDB native catalog acquisition hardness report
version: 0.1
last_updated: 2026-10-03
status: Native acquisition and integrated protection Oracle-clear; SDK gates open
owner: IQToolkit project owner
tags: [modernization, duckdb, native, atlas, evidence]
---

# DuckDB native catalog acquisition hardness report

This ATLAS covers independent Q0.2 in the [native packet](modernization-duckdb-native-probe.md), enabling authentic native inputs before a separately reviewed SDK/ADO comparison. The full [roadmap](modernization-roadmap.md) remains active. No provider, async execution, import, large-result streaming, file-security profile or general platform support is implemented by this result.

## Edge cases and observed outcome

After revised prospective OracleCLEAR, Q0.2 ran once and terminatedPASS/exit0. It loaded the exact acquired native payload, resolved22reviewed exports and qualified all controls before issuing five further finite catalog queries. Original Q0.1 inspectionFAILED, separate Q0.1b metadataPASS and PostgreSQL P4FAILED remain unchanged.

| Actual observation | Result |
| :--- | :--- |
| Library | 62,945,312bytes; SHA256 `23686593dba4ec22c6e581bb1c843b3964e84c4bc4d0063d8b4daddd2e20e30a` |
| Engine metadata | v1.5.6 / source069cc9f9b5 / codenameVariegata / platformlinux_arm64 |
| ABI | 8byte pointers/alignment;48byte Result/16byte String; native BOOLEAN1/BIGINT5/VARCHAR17 paths |
| Typed controls | Six false flags, configuration locktrue, threads1, temporary directoryempty, memory rendering256.0MiB; three list lengths0; no SQL nulls |
| Authentic settings | 159unchanged name/input_type/scope rows; independent native count159 |
| DESCRIBE | Three rows/sixVARCHAR columns; SQL nulls preserved in key/default/extra |
| Native ownership | Six query results destroyed,492allocated strings freed, config destroyed, connection disconnected and database closed |
| Child time | 0.09702110901707783s reported through native cleanup; parent whole child0.12934464699355885s/including owned-group checks0.18221592402551323s |
| Memory observations | Child post-cleanup/pre-publication ru_maxrss51,838,976bytes; parent highest sampled VmRSS52,121,600bytes; both below1GiB rejection threshold |
| Terminal state | exit0; no live owned group members, forced termination, cancellation, cleanup or terminal publication errors |
| Provenance | 53reviewed source inputs and full checkout hashes identical before/after |

The source ID is an observed10character prefix of the pinned commit, not a full40character output. Pointer/structure checks describe the targeted binding; successful native calls corroborate this exact acquisition, not arbitrary future ABI versions. Schema nulls are real engine output and are retained as JSON null, independently from empty strings/zero/false.

## Tool and failure handling

Two source readers independently reviewed exact C API declarations, startup implementation, ownership and actual-data boundaries. Root retained17additional immutable implementation bodies/268,449bytes. Initial and revised53input receipts/52small-source snapshots preserve both prospective versions. Oracle required a pre-launch cancellation guard; root added guards before preparation and immediately before Popen, then obtained revisedCLEAR. No fabricated cancellation/error data or failure-path experiment was executed.

Native synchronous calls were supervised by an independent process-group watchdog. Child deadline25s/parent whole-child30s/termination-reaping allowance4s,1MiB report/log,1,024rows/16columns/4KiB strings and2GiB storage thresholds were fixed prospectively. Polling can overshoot between observations; terminal checks reject acceptance. They are not hard output/disk/RSS quotas. Source-hashing/traversal time is outside the child budget and is not database latency. The child disabled core dumps.

Every query result is destroyed on success/failure; bounded allocated strings are freed even on decoding/assertion failure. Config/open errors and connection/database ownership have explicit cleanup. Native diagnostic text can contain environment-derived paths, so query failures retain state/error-presence and sanitized type, and open-error storage is freed without recording its text. Parent preserves primary failure, bounded partial child output and terminal source/checkout/cleanup observations independently. Forced termination cannot qualify destructor completion or a pass.

Actual result/body/source/log hashes are retained under `artifacts/duckdb-probe/live.nhwgatsx`. Observed OracleCLEAR independently verified159rows/nulls/cardinality/controls and ownership before disposition.

## Concurrency, disposition and resource limits

Root was the only writer/runtime owner; agents performed read-only source and Oracle reviews. No SDK, service, network query or overlapping normal build ran. The Python child exited and no group members remained before cleanup. Config lock had no additional configured exceptions; schema/search_path remain built-in exceptions and are not claimed immutable.

Root copied both executed sources into nonactive `.snapshot` files and bound them to the reviewed receipt. The parent helper is now nonactive. Only the owned0700 app `/tmp/iqtoolkit-duckdb-native.rus1dmi2` and its two verified files were removed. Authentic output persists; no user-owned or system file was removed. Disposition is saved with source/body hashes and terminal ownership state.

Requested256MiB is source-parsed268,435,456bytes. The actual getter is truncated after whole-unit reduction;256.0MiB corresponds to inclusive byte interval268,435,456–268,540,927. This establishes source-consistent configuration, not exact runtime byte measurement. Neither memory_limit nor the observed RSS APIs provides a whole-process hard memory cap. ru_maxrss was sampled after native cleanup before report publication; VmRSS samples use a different kernel interface and cover the parent polling interval. Neither measurement is a benchmark or exact memory certification.

## Security and scope exclusions

The child received only PATH/LANG/LC_ALL, with no ambient credentials/proxy variables. Its in-memory open used nativeNULL, disabled intended external SQL/extension autoinstall/autoload/persistent secrets/unsigned/community extensions and spill, and verified actual empty path/directory/config-exception lists before catalog acquisition. Unrelated setting values, secret data and error-message text were not collected. No provider-shaped rows, generated series, user tables/schema or external corpus was authored.

These controls are not an OS sandbox. Loader/engine startup reads platform information, computes home configuration and can register linked extensions. In-memory READ_ONLY is invalid; the catalog operation was not enforced database read-only mode. No denial/host-filesystem-isolation, import atomicity, large-result streaming, interrupt race, SDK equivalence, async or x64/other-OS/musl qualification follows from this one native run.

## Verification and remaining gates

`/usr/bin/python3 -I artifacts/duckdb-probe/live.nhwgatsx/acquire-native.py` → single nativePASS/exit0; exact executed source is retained nonactive, so this historical command is not a current dispatch instruction.

`/usr/bin/python3 -I artifacts/duckdb-probe/verify-native-checkpoint.py` → protectionPASS: prior91plus17new source hashes,137unchanged production compiler/configuration inputs,12archive/payload pairs and336unchanged ordinary binaries. It bound the native result/source/receipt/nulls/cardinality/ownership/disposition and separately preserved originalFAILED/metadataPASS/P4FAILED outcomes. Exact normal deltas are14added documentation files and the same five existing state/documentation files. The historical Q0 checker hash is verified unchanged; this new checker writes integrated-native.json and does not broaden/overwrite the earlier integrated.json. Final integrated OracleCLEAR independently confirmed108sources/53nativeinputs/52snapshots/137productioninputs/12archive-payload identities/336binaries, exact disposition and truthful scoped reports. Only root status metadata changed after review.

Existing runtime gate evidence stays86passes/325unconfiguredSQLServer skips/247translations/zero warnings, protected by direct current source/configuration/archive/binary identity checks. No new ordinary build/test is claimed.

Next readiness work needs a separately saved SDK/lock/ADO packet using these unchanged native rows/schema/controls. Public contract acceptance, actual import/large-result data, coverage, hosted/platform/support decisions and M0/M1/M5 dependencies remain open. Completion of this checkpoint does not close the full roadmap.
