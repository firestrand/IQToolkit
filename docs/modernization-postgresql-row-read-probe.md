---
title: PostgreSQL pending row-read cancellation probe
version: 0.3
last_updated: 2026-10-03
status: Terminal failed row-read qualification; observed classification Oracle CLEAR
owner: IQToolkit project owner
tags: [modernization, postgresql, streaming, cancellation, probe]
---

# PostgreSQL pending row-read cancellation probe

P4 independently prepares LOCAL-RM-M1/M2 after [P3 query-acquisition cancellation](modernization-postgresql-cancellation-probe.md). It asks whether cancellation during a genuinely pending Npgsql ReadAsync preserves the native error/token and permits same-backend recovery. This is a driver feasibility Probe, with zero production code. M0/M1/M2, D1 acceptance, coverage, generated fixtures and other unanswered gates remain open.

## Execution contract and GOTCHA

**Role/type:** Probe. **Depends on phases:** None for independent feasibility; verified P0–P3 native/driver profile and prospective source/data review are prerequisites. **Execution baseline:** branch agent/modernization-roadmap, unchanged V1v production inputs; SDK10.0.401/runtime10.0.12, PostgreSQL18.6/Npgsql10.0.3 on Ubuntu24.04.5/aarch64. **Integration:** root owns every P4 write, SDK/service and evidence; execution_design source review; Oracle prospective/acquisition/runtime/integrated review. Independent version_metadata D2 investigation is read-only until assigned a separate document. **Facts introduced:** None in production. **Facts strengthened:** operation-specific row-read feasibility. **Facts protected:** current137 compiler/configuration inputs,12 archives/payloads,336 ordinary binaries and retained V1v runtime evidence. **Rollback:** stop only owned processes; preserve evidence and remove owned disposable app/cluster/socket/active code.

**Goals/objectives:** acquire authentic catalog rows/plan; observe reader acquisition completed and at least one real row before an incomplete native ReadAsync task; cancel only with fresh server activity proof; preserve original exception/token/native user-request response; recover all undelayed catalog rows on the same backend. **Tasks:** prospective review→P4.0 native acquisition→observed source/data review→P4.1 isolated SDK→P4.2 pending-read experiment→disposition/ATLAS/integrated review. **Capabilities:** installed private native prefix and SDK, actual engine catalogs, controlled Unix peer connections. **Off-limits:** production changes, authored rows/errors, payload padding, generated series, forced scheduling/barriers, paid service, public listener, system installation or ambient credentials. **Health:** at most three total pending-read eligibility windows/attempts; unexpected semantic failures are terminal, never retry opportunities;30-second consumer cancellation,35-second process watchdog,60-second server lifetime,1MiB per log and2GiB workspace thresholds. No token/cost budget supplied; no paid resources. **Attributes:** actual task/state decides eligibility; recheck every acceptance deadline after awaits; no fabricated or passing-skip evidence. **Users:** maintainers selecting M1/M2 execution capabilities. **Runtime:** private persisted source/output/metadata/hash records, bounded rows/observations; source/data-dependent replanning before new dispatch. **Beliefs/intentions:** source predicts asynchronous unbuffered reads but cannot prove a particular read remains pending; actual acquisition and runtime observation decide.

Technical authority: owner-selected ENG-STD-DOTNET-XPLAT1.1.0-draft.1, SHA256 `8c56930ec1cd26e8b58a273d47cb0b9713f55e6d4961b6d29a636ffaf4f0ebe2`; DEP-001/002, API-001 compatibility, PROC-001, FS-001/002, SEC-002 and PERF-001. Development Plan Creation Guide2.7 supplies DP05/DP09/Probe disposition; autonomous guide preserves full-phase gates. No whole-standard or performance verdict follows.

## P4.0: actual catalog and row-delivery acquisition

Root-allocated0700 directories:

- Native evidence/cluster: `artifacts/postgresql-row-read/native.qzwg05c9`.
- SDK evidence: `artifacts/postgresql-row-read/sdk.1f_r9cna`.
- Driver evidence: `artifacts/postgresql-row-read/live.15vqtfey`.
- Disposable platform-allocated app: `/tmp/iqtoolkit-row-read.zbaik87v`.

Verify ownership/nonsymlink paths and umask077. Reuse P0's four hash-verified native binaries and exclusive private prefix; require zero existing processes using it. Initialize a fresh0700 cluster with peer/reject-host authentication, no TCP,0700 Unix socket/port55432, default read-only and existing P1 resource/logging limits. This packet explicitly selects statement_timeout10s, lock_timeout1s and idle-transaction timeout5s; these private operation settings are not public defaults. Retain P1's foreground server ownership, child setsid awareness, PID/start-time/executable rechecks and pidfd cleanup.

Acquire the existing three native metadata/type/schema responses through psql -X -w plus complete positive, nondropped pg_attribute rows projected as bigint relation_oid, integer ordinal, text name, bigint type_oid and Boolean not_null. Also acquire actual selected/total counts, encoding and `pg_sleep(0) IS NOT NULL`. Require unique actual (relation_oid,ordinal) keys, a true native control and between1024 and4096 selected rows. These cardinality bounds are readiness gates, not fabricated records. Fail and replan if unmet; never pad or replicate rows.

Candidate target SQL, with a Double parameter selecting0.001seconds per actual catalog row:

```sql
SELECT attrelid::bigint AS relation_oid,
       attnum::integer AS ordinal,
       attname::text AS name,
       atttypid::bigint AS type_oid,
       attnotnull AS not_null
FROM pg_catalog.pg_attribute
WHERE attnum > 0
  AND NOT attisdropped
  AND pg_sleep($1) IS NOT NULL
```

No ORDER BY, aggregate or materialization belongs in the target query. Acquire its actual native EXPLAIN(VERBOSE,FORMAT JSON) with the same fixed delay; require the pg_sleep qualifier and a streaming Seq Scan without blocking children. Independently execute the delayed projection natively, capturing each actual row as JSON; compare complete key/value sets to the undelayed native oracle without assuming order. psql's acquisition wrapper adds JSON formatting only; the target plan is acquired separately. Record actual elapsed time; count×delay is not an upper bound. [Delay behavior](https://www.postgresql.org/docs/18/functions-datetime.html#FUNCTIONS-DATETIME-DELAY).

Exact native orchestration is saved/reviewed before dispatch:

```text
/usr/bin/python3 -I artifacts/postgresql-row-read/native.qzwg05c9/native-acquire.py
```

Command bounds: initdb60s; readiness10s with3s individual requests; metadata/plan10s; complete delayed acquisition13s with server statement timeout10s; shutdown12s plus identity-checked cleanup. Both redirected streams share a bounded log, direct argv avoid shell interpolation, recorded cancellation cannot interrupt cleanup or become success. Persist failures separately from cleanup. Stop the server after acquisition; retain its owned data/socket for the later same-engine packet only.

## Source and data prerequisites

Archive-verified PostgreSQL18.6 printtup.c/pqformat.c/pqcomm.c/dest.c show DataRow buffering and8192-byte normal send-buffer flushing; pg_sleep does not flush each row. pg_proc.dat identifies volatile non-null void pg_sleep, clauses.c excludes volatile pseudo-constant qualifications, and misc.c uses interruptible PgSleep WaitLatch with millisecond rounding. These sources and actual cardinality/plan provide plausibility, never pending-I/O proof. Retain exact archive-member byte comparisons and hashes.

Pinned Npgsql commit d3768398c17877b3a916c3c4d87e8e11698991fc: ExecuteReaderAsync calls NextResultAsync and prefetches a row; ReadAsync first tries buffered completion, then registers its token and awaits messages. [Read paths](https://github.com/npgsql/npgsql/blob/d3768398c17877b3a916c3c4d87e8e11698991fc/src/Npgsql/NpgsqlDataReader.cs#L174), [first-row acquisition](https://github.com/npgsql/npgsql/blob/d3768398c17877b3a916c3c4d87e8e11698991fc/src/Npgsql/NpgsqlDataReader.cs#L509). Preserve P3's user-request versus statement-timeout message distinction and synchronous cancellation-callback caveat.

Actual bootstrap catalogs are engine output. Fixed delay, SQL and deadlines configure operations; they do not invent rows, responses, errors or timings. Existing generated-fixture authorization is not consumed. Native acquisition and plan review must precede any SDK/behavioral dispatch.

## P4.0 observed result

The saved native result passed with no cleanup errors: 2,519 unique selected catalog rows from 3,161 total attributes, UTF8, actual non-null sleep control and volatile pg_sleep. EXPLAIN returned a Seq Scan with the sleep filter and no blocking children. The complete delayed acquisition matched every undelayed key/value; its measured command duration was 5.426432628009934 seconds. Server lifetime was 7.047556466946844 seconds; shutdown returned zero, sockets disappeared and no owned native process remained. The new cluster identifier is 7692531548785781497. The offline owned cluster remains solely for P4.2. Oracle reviewed acquisition CLEAR; this does not establish pending ReadAsync.

## P4.1/P4.2: concrete consumer dispatch horizon

After P4.0 actual source/data review, expand saved consumer/orchestration before Oracle dispatch CLEAR. Reuse the unchanged reviewed three-node P2 lock and exact signed packages, new private feed/cold cache/CLI home/HTTP cache, audit/warnings-as-errors/import isolation and disabled reusable build servers. No normal project/dependency change.

The eight app source files reside in the allocated external app above. Program.cs consumes the actual native metadata, SQL, cancellation-message source and catalog oracle as six path/port inputs after the executable. Root invokes these sequential orchestration commands only after prospective source/data CLEAR:

```text
/usr/bin/python3 -I artifacts/postgresql-row-read/sdk.1f_r9cna/sdk-prepare.py
/usr/bin/python3 -I artifacts/postgresql-row-read/live.15vqtfey/driver-acquire.py
```

SDK preparation copies only the previously hash-verified signed three-package archives into a new private feed, verifies SHA512 and nuspec declarations, and performs unchanged locked cold restoration/build. SDK10.0.401, net10.0, audit-all, warnings-as-errors, empty ancestor imports, no apphost, separate CLI/HTTP/package caches and disabled reusable build servers remain mandatory. SDK command deadlines are 15s for version and 120s each for restore/build; each log is bounded to 1MiB and workspace to 2GiB. Record all eight source identities, lock stability, actual loaded/compiled identities and checkout snapshots on every terminal path. No unlocked fallback.

The live helper restarts only the acquired offline cluster after native binary/source/SDK checks and exclusive-prefix process-zero proof. Target SQL is the native plain projection plus pg_sleep($1) IS NOT NULL; its Double parameter is the independently acquired fixed 0.001. Pooling/multiplexing/TLS/GSS are disabled in this peer Unix profile; connect timeout5s, command timeout10s and cancellation timeout2000ms are private probe settings. Run the compiled framework-dependent DLL directly with controlled environment, driver watchdog35s and overall30s; bound complete server lifetime60s and each log1MiB. PostgreSQL child cleanup rechecks executable/PID/start identity and uses pidfd, because children may leave the parent's process group.

Open target/observer on distinct actual backends. Acquire a reader normally; copy and validate unchanged returned catalog rows against the complete native oracle, keyed without an order guarantee. Inspect the actual native ReadAsync task after at least one row. Eligibility requires it incomplete plus fresh matching PID/backend_start/query_start, active state and Timeout/PgSleep. Recheck the two-second observation window and overall token **after** awaiting observer activity and immediately before cancellation. Missing pending I/O is an unmet prerequisite; unexpected errors stop rather than retry. Bound windows/attempts to three.

Capture actual original exception reference/token/native57014/user-message, callback and task settlement times separately. Dispose reader/command before recovery. With a fresh token, reacquire the entire undelayed catalog set and protected metadata on the same PID/backend_start; close connections and observe backend disappearance, rechecking its two-second deadline after the await. Require overall token still valid before success. Outer process/native cleanup bounds remain independent.

An ineligible pending read receives no cancellation: settle and validate its actual row before considering the next window, capped at three total. Buffered completed reads do not count as pending windows. An eligible read returning normally, altered exception/token, wrong native message, connection replacement, missing/duplicate catalog row, cleanup failure or expired deadline fails terminally. Prefix/full-set validation is keyed by actual relation_oid/ordinal and compares all five fields, never ordering or a prescribed prefix size. Read exceptions retain the original reference even if reader cleanup also fails. If an observer/guard/read failure exits with an outstanding read, request its linked cancellation and await settlement before reader/command disposal; record cleanup exceptions separately and preserve the original failure. All reader metadata acquisition is inside its disposal boundary. The observer uses parameterized pg_stat_activity and must match the current backend start, a new query start, exact target SQL, active state and Timeout/PgSleep immediately before cancellation.

Full P4 closure requires original source/output snapshots, actual row subset/full recovery provenance, safe disposable-code/data cleanup, scoped ATLAS, current production/archive/binary identity inspection and integrated Oracle CLEAR. No memory, throughput, transaction, cross-platform, public API or production-provider certification is implied.

## P4.1/P4.2 observed outcome and disposition

Revised prospective source/data review was CLEAR after moving metadata inspection inside reader cleanup and ensuring observer/guard failures cancel and settle an outstanding read before disposal. Sixteen reviewed inputs and nonactive snapshots bind the dispatched source. The isolated SDK command passed: unchanged three-package lock and sources, cold restoration, zero warnings/errors, unchanged checkout.

The live command failed terminally with exit1. Reader acquisition completed; 139 unique returned rows matched the native oracle. One pending native ReadAsync and a fresh separate observer established the exact target's active/Timeout/PgSleep state, matching PID/backend_start/new query_start. The post-await and immediate pre-cancel checks passed. Cancellation entry was 2.839881ms into the observation window, callback duration 7.368542ms and observed settlement 7.665824ms after entry. The read returned true normally; it did not provide the required original cancellation exception/token/native user-request57014. The declared assertion correctly failed. No retry or passing skip followed.

Normal row completion is compatible with the pinned driver: [Read handling](https://github.com/npgsql/npgsql/blob/d3768398c17877b3a916c3c4d87e8e11698991fc/src/Npgsql/NpgsqlDataReader.cs#L257) returns true on a DataRow while [graceful cancellation](https://github.com/npgsql/npgsql/blob/d3768398c17877b3a916c3c4d87e8e11698991fc/src/Npgsql/Internal/NpgsqlConnector.cs#L2163) permits the original connection to receive its response. A row can precede the cancellation error. This is a source-informed explanation, not proof of the exact buffering/arrival boundary or ignored cancellation. [Reader disposal](https://github.com/npgsql/npgsql/blob/d3768398c17877b3a916c3c4d87e8e11698991fc/src/Npgsql/NpgsqlDataReader.cs#L1093) consumes remaining protocol and may catch cancellation internally; successful disposal does not establish an unobserved native error.

Reader disposal passed. Full catalog recovery, same-backend recovery and recorded Closed states were never reached and remain unqualified. Outer native fast shutdown returned0, server lifetime1.4997365750023164s, socket disappeared, no owned process or cleanup error remained, and checkout snapshots matched. The original failed result/log are retained; the failed command lacks an original logSha256 field because its runner threw before assigning one. A separate retained-output hash manifest binds that log without modifying the historical result.

Oracle observed classification CLEAR means the failed evidence is correctly described; it does not mean P4 cancellation passed. [ATLAS](modernization-row-read-and-postgresql-contract-hardness.md) records final snapshots/disposition/identity review. Any future experiment must first define a reviewed stream-level cancellation question that permits rows already delivered by the driver and distinguishes toolkit cancellation policy from native cancellation acknowledgment. It must preserve this terminal failure and the owner-gated public contract rather than silently redefine P4 success.
