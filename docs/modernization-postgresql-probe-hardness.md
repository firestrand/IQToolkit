---
title: PostgreSQL and Npgsql readiness hardness report
version: 0.1
last_updated: 2026-10-03
status: Scoped verified; integrated Oracle CLEAR
owner: IQToolkit project owner
tags: [modernization, postgresql, atlas]
---

# ATLAS: PostgreSQL and Npgsql readiness

The independent readiness probe built PostgreSQL 18.6 privately and acquired genuine catalog metadata through native psql and an exact Npgsql 10.0.3 consumer on Linux ARM64. Both sources agreed. This supplies a usable local environment route despite unavailable Docker access; it implements no IQToolkit provider/public async API and closes no production milestone.

Executed by root (all writes, SDK and services), execution_design (source audit), version_metadata (registry/driver source audit) and oracle (independent review), on branch agent/modernization-roadmap based at60ddef6. Model identifiers were not exposed and are recorded as Unknown. The integrated dirty-checkout identity and qualified documentation deltas are retained in artifacts/postgresql-probe/integrated.json.

## Edge cases and boundaries

- Official22632314-byte source archive matched SHA256 `555610c24d53e4316da5b7d3fc25c279d96856d5e0e23ee308c328c5fa881d9f`; bounded extraction rejected links/special/traversing members by contract. No malicious archive fixture was introduced or tested.
- Native 18.6 configure/build/private-install/version commands succeeded; make took 46.19 s and its log contained zero `warning:` occurrences. ICU/readline support was explicitly disabled. TLS/ICU/readline, upstream regression tests and other platforms remain unqualified.
- Actual read-only native acquisition returned eight ordered `pg_type` rows and32 `pg_attribute` schema columns. Declared SQL casts/JSON preserved source values; no expected row payload, schema or business fixture was authored.
- Actual controls reported server_version_num180006, peer authentication, Unix client/server, empty TCP listener configuration, socket0700 and transaction read-only. Native system identifier `7692509564652654272` remained equal through the Npgsql run on the same cluster.
- Native readiness initially returned2 before0 in both acquisitions; bounded polling treated this as a real startup transition. It was not a semantic RED, skip or fixed-delay activity proof.
- Npgsql returned the same metadata/schema/type rows and eight typed rows: bigint→Int64, text→String, integer→Int32 and boolean→Boolean. A Bigint parameter taken from the first actual OID selected exactly its source row. Loaded Npgsql 10.0.3.0 DLL SHA256 matched the approved cold-cache net10.0 asset. This is small-result driver smoke, not a pending-I/O or streaming-memory test.
- Consumer closed its connection; both native server lifetimes were approximately 1.11 s, fast shutdown exited0, and no owned PostgreSQL processes/socket remained. Cancellation/error cleanup paths were source-reviewed, not exercised through fabricated failures.

## Tool and service failure handling

Oracle found and root corrected cancellation windows, terminal output checks, transfer deadlines, native children that call setsid(), and reusable SDK/compiler-server isolation before their affected stages ran. Signals only record cancellation; owned process cleanup completes before terminal failure propagation. PostgreSQL ownership uses the exclusive compiled executable, UID/PID/start-time and identity-rechecked pidfd signals across sessions, not group-empty inference. Cleanup failures are retained separately. None of the actual four stages failed; no semantic RED is claimed.

Only already installed native tools were used; no sudo, system package/service, Docker permission change, listener exposure or paid resource occurred. SDK/compiler servers were explicitly disabled. Logs/storage were monitored with terminal checks; polling permits transient overshoot, so these are observed thresholds, not strict kernel quotas. Windows/macOS/process-policy qualification is outside this host-only probe.

## Concurrency and load

Root held exclusive SDK and native service leases. Agents inspected sources/evidence read-only. PostgreSQL child process groups actually differed from the postmaster group and were captured; final ownership scans were empty. The probe used one client at a time and imposed native10connection/16 MiB-buffer/5 s-query controls. No contention, large-result, pooling, throughput, recovery or memory-performance claim is made.

## Security and data

Private directories were0700 from allocation, files created under umask077, data/socket ownership verified, local peer authentication required and host authentication rejected. No password/production endpoint/business dataset was used. Actual SQL reports the authentication method boolean rather than identity. Values came from native bootstrap/catalog output; pending fixture, coverage and public-contract decisions were untouched.

Three archives were verified against official catalog SHA512 and actual nuspec identity/version/license before SDK use: Npgsql 10.0.3 (PostgreSQL license), Logging.Abstractions 10.0.12 and DI.Abstractions 10.0.12 (MIT). Auditing and warnings-as-errors stayed enabled. Intentional private lock preparation was reviewed before a cold locked restore; three exact nodes/edges and cached archive bytes matched, lock remained unchanged. NuGet content hashes were recorded separately from archive SHA512. This is no blanket dependency-security or license-compliance certification.

## Verification and evidence

| Root command | Actual result and retained evidence |
| :--- | :--- |
| `/usr/bin/python3 -I artifacts/postgresql-probe/build.cug7brys/native-build.py` | P0 passed; exact hash/license, native 18.6, successful terminal commands, no remaining build groups, checkout unchanged. |
| `/usr/bin/python3 -I artifacts/postgresql-probe/native.kuuybxnf/native-acquire.py` | P1 passed; eight rows/32 columns/security controls, shutdown0/empty owned processes/socket removed, checkout unchanged. |
| `/usr/bin/python3 -I artifacts/postgresql-probe/npgsql.t_quc1gq/sdk-prepare.py` | P2 SDK passed; reviewed exact three node lock, cold restore, copied archive equality, build0 warnings/0 errors, unchanged app inputs and checkout. |
| `/usr/bin/python3 -I artifacts/postgresql-probe/npgsql-live.m9figsjl/driver-acquire.py` | P2 live passed; same-engine native JSON/typed rows/parameter result, approved loaded-driver hash, Closed connection, shutdown0/empty owned processes/socket removed, checkout unchanged. |

Each run retains result.json, exact argv/terminal exits/log hashes and paired checkout snapshots. SDK evidence retains source snapshots, reviewed lock, actual assets/output files and archive identity. Runner commands above describe the executed experiment; closeout preserved `.py.snapshot` sources and removed the uniquely owned standalone project/native data/socket/active runner files, recorded in disposition.json. Retained project sources also use `.snapshot` with an original-name/hash manifest, preventing IDE/LS restoration of the evidence copy. `/usr/bin/python3 -I artifacts/postgresql-probe/verify-checkpoint.py` passes:55 retained source hashes,137 unchanged toolkit compiler/configuration inputs,12 unchanged archives/payloads and all 336 qualified ordinary binaries. The existing V1v 86 tests/247 translations and325 unconfigured SQL Server skips are retained evidence, not tests rerun by this probe. Five existing documentation deltas and three added documents are explicitly qualified; no ordinary code/lock/build mutation is hidden.

## Outcome and follow-ups

Local native environment, exact driver smoke, safe disposition and independent provenance checks are verified; integrated Oracle verdict is CLEAR for this scoped readiness packet. No failed runtime stage or surviving owned service is hidden. The [source matrix](modernization-driver-capabilities.md) identifies SqlClient's cold-open/transaction/disposal fallbacks and Npgsql's savepoint-release caveat. Next ready work is a separately bounded real active-operation/cancellation probe and concrete PostgreSQL capability design. Production M1 still requires M0 and owner-accepted public contracts; M2 requires M1 and its SQL design gate. Backend certification, public semantics, hosted/native matrix, coverage targets and the full roadmap remain open.
