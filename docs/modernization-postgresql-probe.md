---
title: PostgreSQL feasibility and acquisition probe
version: 0.4
last_updated: 2026-10-03
status: Local P0–P2 verified; integrated Oracle CLEAR
owner: IQToolkit project owner
tags: [modernization, postgresql, probe]
---

# PostgreSQL feasibility and acquisition probe

This is independent preparation for LOCAL-RM-M1 and LOCAL-RM-M2. It establishes whether a private native PostgreSQL instance and an exact Npgsql consumer can acquire real metadata on this Linux ARM64 host. It adds no production provider, public contract, dependency, supported platform, performance promise or production configuration. Production M1 still requires its M0 and public-contract gates; production M2 additionally requires the reviewed SQL capability contract. Unanswered V0.2, V0.7, V1c.6 and D1 remain unanswered.

## Execution contract

**Role:** Probe. **Depends on phases:** None; stage prerequisites below apply. **Execution baseline:** branch agent/modernization-roadmap, current V1v library snapshot, SDK 10.0.401/runtime 10.0.12, Ubuntu 24.04.5/aarch64/glibc 2.39. Docker socket is inaccessible, and no PostgreSQL executables were found. **Work packets/integration:** root owns every write, SDK command, native build and service; agents inspect sources/evidence read-only; Oracle reviews before each dispatch horizon and at closeout. **Target slices:** M1 async feasibility and M2 PostgreSQL. **Facts introduced:** None in production. **Facts strengthened:** native environment and exact-driver readiness, only to the extent actually observed. **Facts protected:** current library/configuration/package/consumer identities and native SQLite behavior. **Rollback:** stop only the task-owned server, retain evidence, remove only task-owned disposable code/data; no existing database or system installation is modified.

Technical authority is the owner-selected ENG-STD-DOTNET-XPLAT1.1.0-draft.1, SHA256 `8c56930ec1cd26e8b58a273d47cb0b9713f55e6d4961b6d29a636ffaf4f0ebe2`: DEP-001/002 exact isolated dependencies; PROC-001 bounded owned child processes; FS-001/002 private allocation/cleanup; SEC-002 no secrets/disclosure; native qualification is limited to the actual host. Development Plan Creation Guide 2.7 supplies Probe/data/decision routing; the autonomous guide supplies resumability and remaining phase gates. Existing coverage decisions are not waived by this probe.

## P0: Native source/build feasibility

**Type:** Probe. **Question:** Can already installed native tools build PostgreSQL 18.6 privately? **Hypothesis:** yes, with unavailable optional ICU/readline disabled. **Max production code:** zero. **Disposition:** temporary orchestration is retained as an evidence snapshot outside normal builds; durable output is this acquisition/readiness record. **Affected slices:** later M1/M2 live probes.

Pin the official PostgreSQL 18.6 source archive:

- URL: [postgresql-18.6.tar.bz2](https://ftp.postgresql.org/pub/source/v18.6/postgresql-18.6.tar.bz2).
- SHA256: `555610c24d53e4316da5b7d3fc25c279d96856d5e0e23ee308c328c5fa881d9f`.
- Checksum authority: [official checksum](https://ftp.postgresql.org/pub/source/v18.6/postgresql-18.6.tar.bz2.sha256); actual fetched checksum bytes/source URLs retained in `artifacts/source._1ngal9c`. P0 subsequently fetched and validated the22632314-byte archive in `artifacts/postgresql-probe/build.cug7brys`.
- License: inspect the archive's COPYRIGHT before executing its build. PostgreSQL 18 source/build documentation supplies the configure options; this is not an operating-system package install. [Requirements](https://www.postgresql.org/docs/18/install-requirements.html), [build procedure](https://www.postgresql.org/docs/18/install-make.html).

Root allocates a unique0700 `artifacts/postgresql-probe/build.*` directory under a verified current-user-owned nonsymlink parent; umask077 applies from creation. `/usr/bin/curl --disable` avoids user configuration and enforces a120 s transfer deadline with a125 s owned-process watchdog; its30 MiB size check is independently checked before extraction. Verify the exact SHA256 before extraction. Reject absolute/traversing names, links/special entries, members outside `postgresql-18.6/`, more than50,000members or more than500 MiB expanded data. Extract only into the newly owned private source directory. Record source hash/license/tool versions/environment/actual command argv and terminal exits.

Use a separate VPATH build and private prefix; no sudo, OS package installation, Docker access change, system service, global PATH/library/cache modification, custom PostgreSQL patches or production data. The only source-build shell boundary is the verified upstream configure/build system with fixed root-chosen arguments. Compile with at most four jobs; each stage has a deadline, merged stdout/stderr avoids pipe deadlock, process-group termination/reaping on deadline/cancellation and failure records. SIGINT/SIGTERM/SIGHUP only record cancellation; explicit body checks enter cleanup, signals cannot interrupt ownership setup/cleanup, and recorded cancellation prevents successful completion. Monitor50 MiB log and2 GiB workspace thresholds during stages and check terminal sizes; polling can briefly overshoot, and any observed excess fails the stage. Cleanup checks descendant processes separately from a terminal parent. No server is started by P0.

Literal native commands (variables are root-allocated absolute paths, never user input):

```text
<source>/configure --prefix=<prefix> --without-readline --without-icu
/usr/bin/make -j4
/usr/bin/make install
<prefix>/bin/postgres --version
<prefix>/bin/psql --version
```

Deadlines: configure120 s, make900 s, install120 s, version calls10 s. Server compilation success is accepted only with terminal successful exits and actual18.6 version output. Record native/compiler warnings honestly; no whole-project warning or native upstream test claim. If a required tool/header is absent, checksum fails or build fails, retain the failure and replan from the observed cause, with at most three evidence-driven corrections; do not silently download extra dependencies. ICU/readline/TLS support is not qualified by this private build. P0 does not run upstream regression fixtures.

## P1: Native acquisition

**Type:** Probe. **Question/hypothesis:** can a new private18.6 instance expose authentic system catalogs to its owner through peer-authenticated Unix transport? Expected yes; actual schema/output decides. **Prerequisites:** P0 actual source/build evidence and prospective operation/source review. **Max production code:** zero. **Disposition:** actual metadata/cleanup records persist; cluster and experiment stay outside normal builds and are removed after later same-engine acquisition. No business rows, authored schema, seeded fixture, extension, long-running operation or existing database connection is authorized. Native initdb bootstrap catalogs are actual engine output, not fabricated test responses.

Owned native prefix is `artifacts/postgresql-probe/build.cug7brys/prefix`; compare its four binary hashes to P0 before service work. Root allocates new0700 `artifacts/postgresql-probe/native.kuuybxnf` data/socket directories. Expanded orchestrator: `/usr/bin/python3 -I artifacts/postgresql-probe/native.kuuybxnf/native-acquire.py`, source reviewed before execution. Use fixed absolute argv, no shell, controlled PATH/locale, no inherited libpq credentials/config, and no password. `psql -X -w` disables user startup/password prompts. The OS peer identity is used only for local authentication; SQL emits its authentication method boolean rather than its identity.

Literal operation sequence:

```text
<prefix>/bin/initdb -D <owned-data> --auth-local=peer --auth-host=reject --no-locale --encoding=UTF8 --locale-provider=libc
<prefix>/bin/postgres -D <owned-data>
<prefix>/bin/pg_isready -h <owned-socket> -p 55432 -d postgres -t 1
<prefix>/bin/psql -X -w -q -A -t -v ON_ERROR_STOP=1 -h <owned-socket> -p 55432 -d postgres -f <owned-metadata.sql>
<prefix>/bin/pg_ctl -D <owned-data> -m fast -w -t 10 stop
```

The server runs in foreground, not as a system service. Its PostgreSQL children create new sessions; root therefore requires exclusive use of this unique compiled prefix, rejects pre-existing matching processes and records owned UID/executable/PID/start-time identities across groups. Fast shutdown is followed, if needed, by identity-rechecked pidfd TERM/KILL and parent reaping; group-empty alone is insufficient. Cleanup exceptions fail the result and are retained separately from the operation failure. Private configuration requires `listen_addresses=''`, only the owned Unix socket, socket mode0700, peer local/reject host authentication; no TCP listener or abstract socket. Verify actual data/socket owner/modes and `/proc/<pid>/exe` against the pinned prefix; check readiness within10 s without treating a fixed delay as proof. Record PID, process identities, observed settings/metadata, terminal exits and shutdown. Query deadline5 s, lock deadline1 s, idle-in-transaction deadline5 s, default transactions read-only, max10 connections,16 MiB shared buffers, no parallel workers, max WAL128MiB; no statement/parameter logging. Monitor2 GiB workspace and1 MiB logs/outputs; terminal excess fails. Server lifetime60 s maximum for P1. Verify no remaining owned native processes across sessions and no surviving socket before a pass. Signal handlers only record cancellation and cannot interrupt setup/cleanup.

The three fixed SELECTs run inside `BEGIN READ ONLY`/`ROLLBACK` and produce actual JSON:

1. Engine `version()`, `server_version_num`, native system identifier, effective read-only/listener/socket controls, Unix-client/server booleans and peer-method boolean.
2. First eight actual `pg_catalog.pg_type` rows in OID order: OID cast to bigint, name cast to text, length cast to integer and by-value boolean.
3. Actual `pg_catalog.pg_attribute` schema for `pg_type`: positive, nondropped attributes ordered by attnum, name/type/not-null metadata. No expected row values are authored; the acquisition fails if there are no usable rows/schema, if version is not18.6 or security/read-only controls differ. Save unchanged rows/shape as a later driver oracle; transformations are declared casts/JSON serialization, not invented source values.

References: [initdb authentication](https://www.postgresql.org/docs/18/app-initdb.html), [socket and listener controls](https://www.postgresql.org/docs/18/runtime-config-connection.html), [peer authentication](https://www.postgresql.org/docs/18/auth-peer.html), [shutdown/status](https://www.postgresql.org/docs/18/app-pg-ctl.html), [type catalog](https://www.postgresql.org/docs/18/catalog-pg-type.html), [column catalog](https://www.postgresql.org/docs/18/catalog-pg-attribute.html).

## P2: Exact Npgsql consumer acquisition

**Type:** Probe. **Question/hypothesis:** can an exact net10.0 Npgsql consumer cold-restore/build and match the acquired engine/catalog/parameter oracle? Expected yes; actual locked graph and live output decide. **Prerequisites:** observed P1 source/data review, P2 complete pin/source/operation review; SDK lease exclusively root. **Max production code:** zero. **Disposition:** project/helper snapshots and actual evidence persist privately; disposable project/data are removed after closeout, never enter normal builds. No project reference to IQToolkit or production dependency change.

Root chose the current servicing pair for the two Microsoft dependencies; this is a reversible private-probe pin, not a production update. Complete net10.0 graph:

| Exact package | Declared dependency edge | License / published repository commit |
| :--- | :--- | :--- |
| Npgsql 10.0.3 | Logging.Abstractions >= 10.0.0 | PostgreSQL; `d3768398c17877b3a916c3c4d87e8e11698991fc` |
| Microsoft.Extensions.Logging.Abstractions 10.0.12 | DI.Abstractions >= 10.0.12 | MIT; dotnet/dotnet `95017c711e6afc1085133d440e42b4bd78155701` |
| Microsoft.Extensions.DependencyInjection.Abstractions 10.0.12 | None | MIT; same dotnet/dotnet commit |

Primary catalog declarations, including full archive SHA512, retained under `artifacts/source._1ngal9c`: [Npgsql](https://api.nuget.org/v3/catalog0/data/2026.05.27.11.36.39/npgsql.10.0.3.json), [Logging](https://api.nuget.org/v3/catalog0/data/2026.09.08.19.40.26/microsoft.extensions.logging.abstractions.10.0.12.json), [DI](https://api.nuget.org/v3/catalog0/data/2026.09.08.19.40.26/microsoft.extensions.dependencyinjection.abstractions.10.0.12.json). Exact hashes are also embedded in the reviewed SDK orchestration's PINS table. Download only these official archives (120 s transfer/125 s watchdog,10 MiB each); verify archive SHA512, nuspec identity/version/license and bounded nontraversing ZIP inventory before restore. An unexpected graph/version/archive/license fails; no extra dependency or weakened check is inferred.

SDK evidence root `artifacts/postgresql-probe/npgsql.t_quc1gq`; disposable standalone project `/tmp/iqtoolkit-npgsql.hduhmt4g`,0700. Source snapshots retained in `project-source/`. SDK 10.0.401 pinned with disabled roll-forward; net10.0 framework-dependent, no apphost/RID restore, actual execution on Linux ARM64. Explicit empty local Directory.Build/Packages files prevent inherited repository configuration; own NuGet.config feeds only verified local archives and uses official NuGet metadata for enabled all-package auditing. Isolated CLI/HTTP/preparation/cold caches and obj/bin; source/app inputs snapshotted. NuGet audit and warnings-as-errors remain enabled.

SDK command: `/usr/bin/python3 -I artifacts/postgresql-probe/npgsql.t_quc1gq/sdk-prepare.py`. Its literal operations, each120 s except version15 s:

```text
<dotnet> --version
<dotnet> restore <project> --configfile <config> --force-evaluate --packages <prepare-cache> --disable-build-servers -p:UseSharedCompilation=false -nr:false
<review exact three lock nodes, versions, dependency edges and observed content hashes; snapshot lock>
<dotnet> restore <project> --configfile <config> --locked-mode --packages <new-cold-cache> --disable-build-servers -p:UseSharedCompilation=false -nr:false
<dotnet> build <project> -c Release --no-restore -warnaserror --disable-build-servers -p:UseSharedCompilation=false -nr:false
```

Verify unchanged reviewed lock and exact cached archive bytes after cold restore; retain assets, output DLL hash and terminal build/log results. Persistent MSBuild/compiler-server reuse is explicitly disabled, including environment controls, rather than relying on process-group cleanup to own global reusable servers. [Build options](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-build), [restore options](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-restore). Lock preparation is deliberate in this new isolated application; normal repository locks are never rewritten. Observed NuGet content hashes are recorded separately from full downloaded archive hashes.

Live command after successful reviewed SDK preparation: `/usr/bin/python3 -I artifacts/postgresql-probe/npgsql-live.m9figsjl/driver-acquire.py`. Restart the same private P1 cluster/socket under the reviewed native ownership/identity/pidfd cleanup contract; do not reinitialize or write business/schema data. Verify the four native hashes and probe DLL hash first. Server lifetime60 s; driver watchdog25 s, effective cancellation token20 s, open/command timeouts5 s; same read-only native server limits. Explicit builder Host/Port/database/matching OS Username, Pooling=false, controlled environment without ambient PG credentials/config; server-selected peer is verified by its actual metadata boolean.

Invoke the compiled probe with absolute socket/port and unchanged P1 oracle/SQL paths. Await driver opening, begin, query/row reads, rollback and close; compare all three metadata/schema/row JSON outputs with native psql acquisition on the same system identifier. Also stream eight actual typed catalog rows and parameterize the first acquired OID as Bigint; its one returned row must exactly match the native source row. Retain actual column PostgreSQL/CLR types and values before comparison assertions, actual driver assembly and final Closed state. Root independently compares the emitted JSON, native controls, shutdown/process/socket absence and unchanged checkout. No parameter interpolation or authored expected row values.

Acceptance is driver metadata/parameter/packaging smoke only. Fast small results do not prove pending I/O, bounded-memory large streams, active cancellation, failure recovery, nonblocking lifecycle or an IQToolkit dialect/provider. Exact Npgsql source selects async paths for these core operations; BEGIN can be queued, buffered reads complete inline, and savepoint ReleaseAsync selects synchronous I/O. Those distinctions remain in the source matrix. Such future behavior needs a separate expanded actual-operation packet, not a new fixture assumption.

## GOTCHA spec

**Goal/objectives:** replace the unavailable-Docker assumption with retained native feasibility and real catalog acquisition evidence. **Trigger/termination:** roadmap backend work; end this bounded probe after terminal stages, cleanup proof and Oracle review. **Capabilities/off-limits:** source/registry inspection, local native toolchain and exclusive root SDK/service lease; no production code, endpoint exposure, paid resource, unauthorized fixture or public-contract decision. **Health:** stage deadlines above, three changed-approach corrections maximum; no supplied token/cost budget. **Attributes:** actual native output, exact pins, provenance, explicit failed prerequisites; never infer certification from compilation. **Constraints/users:** host-only maintainers' readiness evidence; native optional features and other architectures remain unqualified. **Runtime:** private ignored workspace, persisted source snapshots/logs/hashes, owned process groups; replan on actual failed prerequisite. **Beliefs/intentions:** installed compiler/make/flex/bison/perl and zlib appear sufficient; verify that belief before introducing Npgsql or production code.

## Review and observed results

P0/P1 observed and P2 prospective/source Oracle CLEAR. All four actual stages passed: exact native 18.6 source/build; real eight-row/32-column acquisition; approved three-archive/three node cold locked Npgsql 10.0.3 consumer build with0 warnings/0 errors; same-engine native/typed/parameter/loaded-DLL comparison. Both server runs ended in approximately 1.11 s, with fast shutdown0, no surviving owned native processes/socket and no cleanup error. Every stage's paired checkout snapshots agree. No production code/dependency/lock changed.

`artifacts/postgresql-probe/integrated.json`, produced by `/usr/bin/python3 -I artifacts/postgresql-probe/verify-checkpoint.py`, independently rechecks actual stage logs/source/archive/output identities,55 pinned driver/BCL source files,137 unchanged toolkit compiler/configuration inputs,12 unchanged package/symbol archives and payloads, and all 336 qualified ordinary binaries. V1v86 tests/247 translations/325 unconfigured SQL Server skips remain the retained runtime checkpoint; no unnecessary toolkit SDK rerun is claimed. Exact documentation deltas are enumerated against that snapshot; historical V1w inspection remains unchanged.

After copying all actual project sources/lock/assets/output files, root verified no live owned native process and removed only the uniquely owned standalone project, native data/socket directories and four active runner files. Every retained project source also uses `.snapshot`, with an original-name/hash manifest; no runnable retained csproj can trigger IDE restoration. `.py.snapshot` sources, native archive/private prefix, package caches and actual JSON/log evidence remain private; `disposition.json` records ownership/paths/hashes/removal. Commands above are the historical executed experiment, not active build integration. [ATLAS report](modernization-postgresql-probe-hardness.md) and integrated Oracle review close this local readiness packet; public contracts, active cancellation, large streams, production providers, support certification, M0 and full-roadmap gates remain separate.

Final integrated Oracle verdict: CLEAR. Independently verified all four stages, signed archive/NuGet hash domains, exact graph/assets/three copied DLL payloads, native/typed/parameter/loaded-driver proof, shutdown/disposition, seven protected project snapshots,55 pinned source files,137 unchanged compiler/configuration inputs,12 package/symbol archives/payloads and336 unchanged ordinary binaries. Verdict is scoped to local environment/metadata-driver readiness and source-document clarity. All SDK/native sessions and worker tasks are terminal; no coverage, fixture, public-contract or provider-support approval is inferred.
