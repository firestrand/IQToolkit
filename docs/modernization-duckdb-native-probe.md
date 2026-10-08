---
title: DuckDB native catalog acquisition probe
version: 0.3
last_updated: 2026-10-03
status: Native acquisition PASS and observed Oracle-clear; SDK unqualified
owner: IQToolkit project owner
tags: [modernization, duckdb, native, catalog, probe]
---

# DuckDB native catalog acquisition probe

Q0.2 independently investigates whether the pinned Linux ARM64 DuckDB1.5.6 library loads and returns authentic metadata through its C API. Required `LOCAL-RM-M5` in the [roadmap](modernization-roadmap.md) retains the [analytical contract](modernization-duckdb-contract.md), production dependencies and public/data/support gates. Earlier [Q0 preparation](modernization-duckdb-probe.md) is integrated Oracle-clear for original archive inspectionFAILED, separate retained metadataPASS and D4 proposal clarity only. Neither previous result qualifies native loading.

## Execution contract and GOTCHA

**Role/type:** Technical Probe. **Dependencies:** Integrated Q0/D4 checkpoint, unchanged native/library/source provenance, and separately saved exact code/source/data packet with prospective OracleCLEAR before runtime. No production phase dependency for independent feasibility; production M5 still depends on M0/M1 and reviewed SQL/workload/file/import decisions.

**Authority:** Owner-selected `ENG-STD-DOTNET-XPLAT`1.1.0-draft.1, SHA256 `8c56930ec1cd26e8b58a273d47cb0b9713f55e6d4961b6d29a636ffaf4f0ebe2`; private process/storage/security/native platform rules apply. Development Plan Creation Guide2.7 supplies DP-05 actual-data, DP-09 decision authority and disposable Probe requirements. Autonomous, agent-tooling and Technical Markdown guides preserve scope and execution discipline. No whole-standard compliance claim.

**Goals/objectives:** Observe actual loader/export/ABI/version/platform identity, effective constrained configuration and unchanged native settings rows/schema/cardinality. Preserve nulls and multiplicity. No fixture, table, row, external data or error payload is authored.

**Tasks:** Retain immutable implementation sources→save exact private native child/parent/SQL/bounds→prospective Oracle→one bounded native acquisition→independent observed review→owned source disposition/ATLAS/new protection gate/integrated review. The initial source-only horizon is historical; this revision saves only the native packet for review. SDK/ADO dispatch remains prohibited.

**Capabilities/ownership:** Root owns all writes and runtime. execution_design/version_metadata inspect exact sources read-only; Oracle independently reviews. No recursive delegation. Existing installed Python/git/curl and actual acquired native artifact only; no installations, Docker permissions, service/public listener, credentials or paid resource.

**Health:** Proposed child execution budget30s including native cleanup, followed by at most4s bounded termination/reaping if needed; parent independently observes synchronous native calls. Proposed output1MiB/result<=1,024rows/<=16columns/cell<=4KiB; native memory setting256MiB is not a whole-process bound. Parent observes actual RSS with a1GiB rejection threshold; polling is not a strict allocation cap. Existing2GiB private workspace ceiling persists. Unexpected behavior terminates this packet with no automatic retry. Exact saved runtime code and prospective review must fix these bounds before dispatch.

**Attributes/constraints/users:** Maintainer-only feasibility evidence on native Ubuntu24.04.5/aarch64. A catalog-only in-memory engine is not an enforced read-only database; the engine rejects in-memory READ_ONLY. No genuine async, large-result streaming, import, filesystem isolation, package-consumer or support certification follows.

**Runtime/beliefs/intentions:** Immutable source and genuine engine outputs persist in ignored evidence. Disposable experiment source becomes a nonactive snapshot after terminal ownership checks. Preserve137production compiler/configuration inputs,12archive/payload pairs,336ordinary binaries and prior P0–Q0 failure/PASS evidence. Native evidence can enable a separately reviewed later SDK/ADO comparison, not public acceptance.

## Source horizon and proposed native configuration

The actual acquired library is `artifacts/duckdb-probe/acquisition.k152y57s/native/libduckdb.so`,62,945,312bytes/SHA256 `23686593dba4ec22c6e581bb1c843b3964e84c4bc4d0063d8b4daddd2e20e30a`, identical to both NuGet and separately acquired official release members. Engine source commit `069cc9f9b5be802405797faecc284961b07c70ef` is bound by retained release metadata. C header API1.5.6, driver commit and selected graph remain exact. At the source-only horizon, runtime version/source ID and exports still required observation; the actual native result is now recorded in the completion section below.

Root retained17exact successful engine implementation bodies in `artifacts/duckdb-probe/native-source.vltmjrhi`,268,449bytes with URL/commit/byte/SHA256 manifest: C API CMake/config/open/result/value; database initialization; secret manager; settings.json; current_setting; list length; string formatter/parser declaration/implementation; generic conversion and allocated-string ownership; config declaration; external extension loading and filesystem. Existing retained header/settings/storage sources supply declarations and startup constraints. The source worker independently verified all17identities. Older source lookup failures remain recorded and are not evidence.

Proposed startup order, subject to exact source/code review:

```text
temp_directory                 ""
threads                        "1"
memory_limit                   "256MiB"
autoinstall_known_extensions    "false"
autoload_known_extensions      "false"
allow_persistent_secrets        "false"
allow_unsigned_extensions      "false"
allow_community_extensions      "false"
enable_external_access         "false"
lock_configuration             "true"
```

Use actual config-name enumeration (`duckdb_config_count`, `duckdb_get_config_flag`), then a non-null empty value for temp_directory and `duckdb_open_ext(NULL,...)`. Unknown names can return setter success and fail only during open, so setter status is not sufficient. Leave allowed_paths/directories/config exceptions unset and require effective empty lists. Lock always permits schema/search_path changes; empty allowed_configs excludes additional configured exceptions, not every possible setting change. Native non-null path can add database/WAL allowlists when external access is false. The pinned driver maps ordinary DataSource=:memory: to nativeNULL; its separate API-label setting remains contextual. Avoid property-grid descriptor enumeration, which can open a separate unrestricted default database.

Source memory parser supports requested256MiB=268,435,456bytes. The effective getter first truncates whole units and then the displayed decimal. Runtime `256.0 MiB` therefore establishes consistency with the source-parsed request, not exact internal byte equality; its integer interval is268,435,456–268,540,927 (remainder0–102wholeKiB plus0–1,023bytes). Record actual rendering and this limitation independently.

These controls constrain intended external SQL, extension autoinstall/autoload, persistent secrets and spill. Loader/engine startup still reads platform information and may register linked extensions. No zero-host-filesystem-access or no-linked-extension-registration claim is made. Capture effective controls before broader catalog reads; do not query secrets or unrelated setting values.

## Proposed actual data and SQL

The first result is one typed row for seven boolean controls, threads, empty temporary directory, memory rendering and lengths of allowed_paths/directories/allowed_configs. Require exact names/order/types, one row and nonnull values before extracting. Six flagsfalse/locktrue/threads1/list-lengths0 are operation constraints, not generated provider rows.

Then acquire only:

```sql
SELECT library_version, source_id, codename FROM pragma_version();
SELECT platform FROM pragma_platform();
SELECT name, input_type, scope FROM duckdb_settings() ORDER BY name;
DESCRIBE SELECT name, input_type, scope FROM duckdb_settings();
SELECT count(*) AS catalog_count FROM duckdb_settings();
```

Retain actual ordered column names/native types, SQL nulls, engine strings and rows. Compare genuine count to captured catalog cardinality. Preserve duplicates. No synthetic strings, schemas, expected catalog rows or error responses are supplied. Actual version/source ID must align with the pinned release; exact acceptance and export list await runtime code. Empty/missing/mismatched controls or metadata fail rather than skip or silently relax constraints.

## ABI and ownership constraints

The retained header defines Result as three64bit idx_t fields and three pointers (48bytes), String as pointer+64bit length (16bytes), state/type enums as C int and opaque handles as pointers. Exact primitive enums: BOOLEAN1, BIGINT5, UBIGINT9, VARCHAR17. Validate platform pointer/structure sizes and actual exports before opening.

Deprecated result/value APIs remain in the pinned default header surface. Their narrowly scoped use is for materialized metadata only; do not mix chunk APIs or treat them as future adapter architecture. Schema/type/null checks precede extraction because conversion accessors can return defaults. Borrowed names/error/version strings must be copied within bounds before owner release. Allocated String data retains its pointer (`c_void_p`), copies its bounded native length and is freed exactly once even on decoding/assertion failure. Never use `c_char_p` for allocated returned storage.

Destroy every zero-initialized query result even on failure. Destroy config on create/open failure; free allocated open errors; disconnect connection then close database after results/strings settle. Parent termination is not proof that native destructors ran; forced termination is a failed acquisition. Save authentic partial observations and failure type without exposing ambient data.

## Exact native dispatch packet

Historical preparation allocated owned0700 app `/tmp/iqtoolkit-duckdb-native.rus1dmi2` through platform temporary-directory creation. Evidence lives in previously allocated owned0700 `artifacts/duckdb-probe/live.nhwgatsx`; workspace.json binds both paths. Fixed experiment sources were app/native.py and evidence/acquire-native.py, initially unexecuted. They parsed as syntax-only source; independent readers cleared signatures/startup/type/null/ownership paths. The final parent limited KILL reaping to1s, recorded surviving group members on cleanup failure and checked cancellation immediately before launch. Revised prospective Oracle reviewed the final53sealed input hashes/52snapshots before the one native invocation; executed sources are now retained nonactive.

Historical command invoked once after complete revised source/data/code prospective OracleCLEAR:

```text
/usr/bin/python3 -I artifacts/duckdb-probe/live.nhwgatsx/acquire-native.py
```

The parent launches exactly one child with direct argv `/usr/bin/python3 -I -B /tmp/iqtoolkit-duckdb-native.rus1dmi2/native.py`, private cwd, new session/group and only PATH/LANG/LC_ALL environment entries. It validates tool installation ownership and rejects writable substituted executable paths. No shell/native CLI, service or network query is invoked. Git's bounded5s builtin file listing captures complete normal checkout hashes; fsmonitor and untracked-cache options are disabled for that listing. Normal writers remain stopped until the terminal after-snapshot.

The child disables core dumps, verifies platform/ABI and pinned native bytes, loads the exact library with local/immediate symbol resolution and resolves22required exports before any config/open. It records actual library version/config names/setter states, applies all ten options in order, opens nativeNULL, destroys config, connects and acquires the control row. Only after exact typed/nonnull/value qualification does it issue the five finite SQL statements above. Source-ID acceptance requires10–40lowercase hexadecimal characters matching the retained commit prefix; it does not claim a full40character observation if the native output is shorter. Platform must be actual linux_arm64.

The source fixes every control column name and type (sevenBOOLEAN, threadsBIGINT, temp/memoryVARCHAR and threeBIGINT lengths), exact one-row/value rules, query aliases and ordered catalog/schema/cardinality checks. Result values retain SQL NULL independently from zero/false/empty strings. Catalog strings are copied unchanged; names/scope/type output excludes unrelated environment-derived setting values. SQL and startup literals are operation configuration, not provider payload fixtures.

Ownership is explicit: every result destroyed in finally, each allocated value-string pointer freed in finally, config destroyed even on failed create/open, open-error allocation freed without retaining path-bearing text, connection disconnected before database close. Native query failures retain status/error-presence and a sanitized failure type, not uncontrolled engine diagnostic text. Complete/partial observations are exclusively created as app/actual.json and preserved in evidence when bounded. Child result records actual PID/kernel maximumRSS and cleanup counts/handle outcomes.

The child checks25s operation/cleanup elapsed and1MiB terminal observation size; the parent independently accepts at most30s whole child including report publication, then at most4s owned group termination/reaping. Parent polls log/result/disk/RSS limits and rejects them again where terminal evidence allows. RSS1GiB is a monitored rejection threshold, not a hard allocation cap; child kernel ru_maxrss is sampled after native cleanup before report publication, distinct from parent VmRSS observations. Limits are not a zero-overshoot or exact memory guarantee. Fixed per-result bounds are1,024rows/16columns/4KiB strings; private storage remains<2GiB and each report/log<=1MiB. Parent hashing/traversal/evidence time is outside the child budget and does not qualify database latency.

The sealed receipt binds exact child/parent, selected standard, native library, source bodies/manifests, metadata/provenance and current packet/plan. Preserve immutable small-source snapshots before dispatch. Compare receipt inputs and full checkout hashes before/after; app may gain only actual.json. Unexpected files, input drift, forced termination, lingering members, cleanup/publication errors, cancelled state or guard failures yield terminalFAILED, never a skip or semantic retry. Each primary failure is preserved while final source/checkout/child-body publication is attempted independently. SIGKILL/termination does not prove destructor completion. No subsequent SDK/ADO command follows from a pass.

## Completion limits

Historical native dispatch obtained revised prospective OracleCLEAR after a prelaunch cancellation guard was added;53input hashes/52snapshots matched. Single nativePASS/exit0 observed v1.5.6/source069cc9f9b5/Variegata/linux_arm64,159actual settingsrows/3DESCRIBEschemarows and independent count159, all13typednonnullcontrols, six result destructions/492stringfrees and closed handles. Parent whole-child0.12934464699355885s/including group checks0.18221592402551323s; source/checkout unchanged, no groups/cancellation/forced termination/errors. Observed OracleCLEAR independently verified actual metadata/nulls/provenance/cleanup.

Both executed sources and actual output were preserved before the owned app was removed and parent made a nonactive `.snapshot`. Commands above are historical evidence, not current dispatch instructions. [Native ATLAS](modernization-duckdb-native-hardness.md) records exact bounds and limits; final protection/integrated review follows. SDK/ADO comparison requires another separate packet after these authentic inputs. All original failed and passed readiness artifacts remain preserved; this packet neither reruns nor broadens their historical inspectors.

Completion of Q0.2 requires actual eligible native metadata or a truthful terminal failure, retained exact provenance/source/output, safe owned disposition, ATLAS, production/artifact identity checks and integrated Oracle review. Full-roadmap and pending human decisions remain active; no native/source/package readiness alone implements M5.
