---
title: PostgreSQL active query cancellation probe
version: 0.2
last_updated: 2026-10-03
status: Scoped actual cancellation/recovery and integrated Oracle CLEAR
owner: IQToolkit project owner
tags: [modernization, postgresql, cancellation, probe]
---

# PostgreSQL active query cancellation probe

P3 independently prepares LOCAL-RM-M1/M2 after [P0–P2 acquisition](modernization-postgresql-probe.md). It qualifies only actual reader acquisition cancellation and same-backend recovery on the observed Linux ARM64 profile. Row-read cancellation, production provider support, memory and performance remain unqualified. Production M1/M2 gates and unanswered V0.2/V0.7/V1c.6/D1 remain open.

## Execution contract and GOTCHA

**Role/type:** Probe. **Depends on phases:** None; verified P0–P2 binaries, package graph and source review are empirical prerequisites. **Execution baseline:** branch agent/modernization-roadmap, V1v production snapshot; Ubuntu24.04.5/aarch64, SDK10.0.401/runtime10.0.12, PostgreSQL18.6/Npgsql10.0.3. **Work packets:** root owns P3 source, SDK, service and integration; execution_design reviews source read-only; Oracle reviews prospectively and after execution. Independent D3 documentation has an exclusive single-file worker allowance. **Facts introduced:** None in production. **Facts strengthened:** operation-specific driver feasibility. **Facts protected:** 137 compiler/configuration inputs,12 archive/payloads,336 ordinary binaries and existing runtime evidence. **Rollback:** stop only owned processes, remove private disposable app/cluster/socket, retain hash-bound source/output snapshots.

Technical authority: owner-selected ENG-STD-DOTNET-XPLAT1.1.0-draft.1, SHA256 `8c56930ec1cd26e8b58a273d47cb0b9713f55e6d4961b6d29a636ffaf4f0ebe2`; DEP-001/002, PROC-001, FS-001/002, SEC-002, OBS-001. Development Plan Creation Guide2.7 supplies DP05/DP09 and Probe disposition; autonomous guide retains full-phase closure obligations. This is scoped qualification, not whole-standard compliance.

**Goals/objectives:** Observe an incomplete ExecuteReaderAsync task while its actual backend is active in PgSleep; cancel its own token; capture the original native user-cancellation exception; acquire unchanged catalogs on the same backend; close and clean up. **Trigger/termination:** prospective Oracle CLEAR then exact commands below; stop on success, semantic failure, three ineligible attempts or any deadline/security/cleanup failure. **Capabilities/off-limits:** installed SDK/native prefix, private Unix transport and native catalogs only; zero production code, authored rows/errors, paid services, system installation, public listener or ambient credentials. **Health:** three total attempts,20-second consumer token,25-second owned-process watchdog,60-second server lifetime,1MiB per log and2GiB workspace thresholds; thresholds are polled and may briefly overshoot. **Attributes:** observed outcomes stay distinct; no semantic retry, passing skip or unobserved async claim. **Users:** maintainers deciding M1/M2 feasibility. **Runtime:** persisted actual JSON/log/hash evidence; bounded attempt/activity records; source-dependent replan after failure. **Beliefs/intentions:** P2 proves metadata readiness only; source predicts native cancellation with a retained token and usable connector, which actual observation must establish.

## P3.0: isolated SDK preparation

Root-allocated private0700 evidence directories are `artifacts/postgresql-cancellation/sdk.du8alrba` and `artifacts/postgresql-cancellation/live.sftd82ac`; disposable app is `/tmp/iqtoolkit-cancel.8qmir6h_`, allocated through Python's platform temporary-directory API. Verify ownership, nonsymlink paths and umask077. Source copies of P2's seven-file consumer scaffold retain import isolation; Program.cs changes and NuGet.config points to the new verified private feed. Reuse its reviewed lock unchanged: SHA256 `2ce1cf900414dba85facd6961590f546a8dff8efd42b83e8729c49066f416359`. Dependencies remain Npgsql10.0.3 and Logging/DI.Abstractions10.0.12, all direct exact pins, local verified feed only; official audit remains enabled. Verify all three signed archive SHA512 pins from P2's approved-pins.json before restore. Preserve distinct signed-archive and NuGet logical-content hash domains.

Controlled SDK environment uses unique CLI home, HTTP cache and cold package cache; `MSBUILDDISABLENODEREUSE=1`, `DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER=1`. Commands use absolute trusted executables and argument arrays, merged bounded output, owned process-group cleanup and recorded terminal exit. Exact restore/build argv are in the reviewed sdk-prepare.py; essential arguments:

```text
/home/firestrand/.dotnet/dotnet restore NpgsqlProbe.csproj --locked-mode --configfile NuGet.config --packages <new-owned-cold-cache> --disable-build-servers -p:UseSharedCompilation=false -nr:false
/home/firestrand/.dotnet/dotnet build NpgsqlProbe.csproj -c Release --no-restore --disable-build-servers -p:UseSharedCompilation=false -nr:false
```

Both commands have120-second watchdogs. Accept only unchanged reviewed lock, exact three-node/edge graph, actual cached signed archives matching the approved feed, unchanged source before/after, and zero-warning/error build. This produces no normal repository project/dependency change.

## P3.1: real operation and recovery

Validate four P0 binary hashes at `artifacts/postgresql-probe/build.cug7brys/prefix`. Require zero already-running processes from that exclusive prefix. Initialize a new cluster with `--auth-local=peer --auth-host=reject --no-locale --encoding=UTF8 --locale-provider=libc`; no TCP listener, private0700 socket, port55432, default read-only, statement_timeout5s, lock_timeout1s, idle transaction timeout5s, max_connections10 and existing P1 resource limits. Acquire a new system identifier and the same three native metadata/catalog SELECTs through psql -X -w before the driver operation. Do not reuse the old cluster's identifier.

Npgsql settings explicitly select actual OS peer username, Pooling=false, Multiplexing=false, Timeout5s, CommandTimeout5s, CancellationTimeout2000ms, SSL/GSS encryption disabled for this private Unix qualification. Open target and observer connections; actual ProcessIDs must differ. Observer runs parameterized pg_stat_activity SELECTs in autocommit. Capture pid/backend_start/query_start/state/wait_event_type/wait_event/query; bounded polling ends after2seconds or256 observations. No scheduling sleep, fabricated server row or barrier forces overlap.

Each attempt executes exactly `SELECT pg_sleep(4)` through target ExecuteReaderAsync with its own linked token. Eligibility requires fresh observed state=active, wait_event_type=Timeout, wait_event=PgSleep, exact controlled query and unchanged backend identity, plus target task incomplete immediately before cancellation. A race or absent prerequisite is recorded as ineligible, not successful. At most three total attempts; first eligible attempt ends the experiment, and any genuine cancellation/recovery failure terminates without retry. Each ineligible operation must settle and its reader/command must dispose before another attempt; no pending task crosses attempts. Fresh query_start must differ from the pre-attempt observation.

Record monotonic cancellation entry/return and operation settlement separately. The token callback synchronously opens a separate cancellation connection; CancellationTimeout does not bound the complete Cancel() call. Capture original exception identity through a transparent observer/rethrow, its actual type, inner native PostgresException, SQLSTATE, controlled MessageText, token equality and connection state. Positive result requires OperationCanceledException containing the actual user token, native SQLSTATE57014 **and the upstream source-derived user-request message**. Statement timeout has the same SQLSTATE and cannot pass. Native18.6 source at src/backend/tcop/postgres.c distinguishes these outcomes; derive the expected message from the archive-verified source and retain its hash. Raw cancellation packets/backend secret keys are never recorded. [Native cancellation protocol](https://www.postgresql.org/docs/18/protocol-flow.html#PROTOCOL-FLOW-CANCELING-REQUESTS), [actual activity and PgSleep](https://www.postgresql.org/docs/18/monitoring-stats.html).

Dispose operation resources, run all three acquired metadata SELECTs and the typed eight-row catalog query on the same connection with a fresh token, and compare actual results to the fresh native oracle. Require unchanged ProcessID and observer backend_start; State=Open alone is insufficient. Require the sleep no longer active. Close target, independently observe its backend disappearance, close observer, and verify both Closed. Capture actual loaded driver hash and compare to the verified archive/net10.0 asset. Native shutdown and identity-rechecked pidfd cleanup retain P1's across-session child ownership rules.

Root dispatch commands after prospective source review:

```text
/usr/bin/python3 -I artifacts/postgresql-cancellation/sdk.du8alrba/sdk-prepare.py
/usr/bin/python3 -I artifacts/postgresql-cancellation/live.sftd82ac/cancel-acquire.py
```

Native initdb60s/readiness10s/query10s/shutdown12s and owned cleanup deadlines remain explicit in the source. Reserve cleanup independently of the consumer token. Persist failure and cleanup outcomes separately; no unfinished task, timed-out callback, broken/replaced connection, statement timeout or surviving native process qualifies success.

## Source and data review boundary

Pinned Npgsql commit d3768398c17877b3a916c3c4d87e8e11698991fc: NpgsqlCommand1391/1558, NpgsqlDataReader335/509/1093, NpgsqlConnector2117–2327/1734–1800/2941, Internal/NpgsqlReadBuffer263–367, Internal/NpgsqlConnector.FrontendMessages354–370 and PostgresErrorCodes.QueryCanceled. Retain additional source bytes/URLs/hashes separately; do not rewrite P2's historical55-file manifest. [Synchronous cancellation dispatch](https://github.com/npgsql/npgsql/blob/d3768398c17877b3a916c3c4d87e8e11698991fc/src/Npgsql/Internal/NpgsqlConnector.cs#L2232), [response/fallback path](https://github.com/npgsql/npgsql/blob/d3768398c17877b3a916c3c4d87e8e11698991fc/src/Npgsql/Internal/NpgsqlReadBuffer.cs#L263).

Fixed pg_sleep duration is operation configuration; all activity, catalog rows, errors and timings come from actual engine/driver execution. No generated fixture authorization is consumed. A sleep query proves reader acquisition cancellation only: Npgsql prefetches its first row before acquisition returns. Pending ReadAsync requires a separate real streaming packet. TLS, cross-platform behavior, nonblocking lifecycle, memory, throughput, transactions and IQToolkit translation remain outside this result.

## Closeout criteria

Save actual evidence and ATLAS, delete only owned disposable app/cluster/socket/active helper code, retain nonactive .snapshot sources and hash-bound outputs, run a new integrated metadata inspector against unchanged production identities and explicitly allowed documentation deltas, and obtain integrated Oracle CLEAR. Historical P0–P2 inspectors remain unchanged. Full roadmap and production readiness are still open.

## Observed P3 result

Prospective source/data Oracle CLEAR preceded both actual commands. Isolated cold locked restore/build passed with0warnings/errors, unchanged three-node graph and reviewed lock. Native acquisition used a fresh system_identifier7692523652508630171. First attempt observed actual targetPID2817220 active/Timeout/PgSleep with a new query_start and incomplete task; observerPID2817221 was distinct. Cancellation preserved original exception/token and native user-request57014, with callback3.558627ms and cancel-to-settlement4.344048ms. Recovery retained PID/backend_start and matched all fresh native metadata/eighttypedrows/32schema columns. Both connectionsClosed, targetbackendgone, shutdown0/noownedprocess/socket/cleanuperrors; each stage's checkout snapshot unchanged. These timings are observations, not performance commitments.

Independent review identified a historical source limitation: polling checks the two-second window before awaits, without repeating it at both acceptance points. The actual run stayed within both windows, proven by cancelEntry2.735446ms and the entire driver0.437958248s/server1.553251817s lifetimes. The new integrated inspector requires that observed containment proof. Retain executed source unchanged; a future harness must add post-await eligibility/disappearance deadline checks and a final overall-token check. This result does not claim the disposable loop generally enforces a two-second ceiling. No repeat is needed to qualify the recorded in-bounds run; no missing failure path becomes observed.

See [scoped ATLAS](modernization-cancellation-and-keyed-contract-hardness.md) and retained artifacts/postgresql-cancellation for original source/output/cleanup/provenance. Final integrated inspection and Oracle are CLEAR for this scoped checkpoint. Production M1/M2, row-read cancellation, other operations and all pending owner decisions remain open.
