---
title: Pending row-read probe and PostgreSQL contract hardness report
version: 0.1
last_updated: 2026-10-03
status: Integrated evidence Oracle CLEAR; row-read qualification FAILED
owner: IQToolkit project owner
tags: [modernization, atlas, postgresql, streaming]
---

# ATLAS: pending row-read probe and PostgreSQL contract

This report records independent [P4 feasibility](modernization-postgresql-row-read-probe.md) and [D2 proposal](modernization-postgresql-contract.md), using the owner-selected .NET1.1.0-draft.1 standard and applicable workflow guides. P4 cancellation qualification failed. D2 source/design clarity is Oracle CLEAR; public acceptance, M0/M1/M2 and existing fixture/coverage/policy decisions remain open.

## Edge cases and observed outcome

Actual PostgreSQL18.6 bootstrap catalogs supplied2,519 unique five-field rows, counts/schema/encoding and the non-null volatile sleep control. The acquired Seq Scan had no blocking child, and the full delayed projection matched the undelayed native key/value set. No authored row, generated series, padding or forced schedule was used. This establishes native readiness only.

The exact Npgsql10.0.3 consumer acquired a reader and validated139 unique rows before an incomplete native ReadAsync. A separate backend reported the target active/Timeout/PgSleep with matching PID/backend_start and new query_start. Post-await/pre-cancel checks held; cancellation entry2.839881ms, callback7.368542ms and observed settlement7.665824ms after entry. The read returned true normally, so the declared original-exception/token/native57014 assertion failed. The returned row was not copied/validated after cancellation; do not claim140 validated rows. No retry occurred.

Pinned source permits DataRow completion while graceful cancellation awaits the server's response; buffering/arrival races are plausible, but the precise boundary is unobserved. Disposal can consume messages and catch cancellation internally. Therefore neither successful native cancellation nor ignored cancellation/driver defect follows. Full recovery, backend reuse and recorded terminal connection states were not reached. Prior P3 reader-acquisition cancellation evidence remains independently qualified and narrower.

D2 reviews complete M2 CRUD/keys/transactions/parameters/paging/types and mapping/query selections. It identifies vendor-free result-producing DML lowering for RETURNING, deterministic positional parameter prerequisites, Npgsql10 date/time behavior, explicit array/JSON/null bounds, redacted native operations and owner-gated public/data choices. CPU-only construction/conversion may remain synchronous in async materialization; synchronous provider I/O requires a genuine async path. This is a proposal, without a production adapter or native write evidence.

## Tool and service failure handling

Prospective review required metadata getters inside reader disposal and cancellation/settlement of outstanding reads on observer/guard failure before disposal. These fixes preceded dispatch and are source-reviewed; those exceptional paths were not induced at runtime. Sixteen revised input snapshots bind the executed packet.

The cold locked restore/build passed with zero warnings/errors and unchanged lock/source/checkout. The driver exited1 at the exact assertion; its original failure result is retained. Because its runner raised before assigning command logSha256, a separate retained-output manifest hashes the actual failure log rather than inventing that original field. The failed outcome must never become a passing skip.

Reader disposal succeeded. Native fast shutdown returned0; server lifetime1.4997365750023164s, no remaining owned process/socket, no cleanup error and unchanged checkout. Native children can leave the parent's process group, so executable/PID/start-time/pidfd ownership checks remain necessary. Disposable app/data/socket/helpers are removed only after terminal ownership checks and exact nonactive source/output snapshots.

## Concurrency and load

Root exclusively dispatched SDK/services and owned integration; workers reviewed source or exclusively saved the separate D2 document, released before SDK. Target/observer used distinct physical connections; no command overlap on one connection was introduced. One actual eligibility window was observed. No memory, throughput, cancellation SLA, load, transaction or platform certification is claimed.

## Security and disclosure

Private0700 evidence/data/socket, peer authentication, reject-host rules, no TCP listener, read-only transactions and controlled environments remained in scope. No credentials, cancel key/wire packet, user records, schema writes, paid service or system installation were used. Source/archive/package/lock/binary hashes bind provenance. D2 endpoint/security/cursor/data policies remain explicit owner decisions.

## Verification commands and results

- `/usr/bin/python3 -I artifacts/postgresql-row-read/native.qzwg05c9/native-acquire.py`: PASS; authentic catalog/control/plan and delayed-set acquisition, clean native shutdown, checkout unchanged.
- `/usr/bin/python3 -I artifacts/postgresql-row-read/sdk.1f_r9cna/sdk-prepare.py`: PASS; exact unchanged three-package lock, new cold cache, zero-warning/error build, sources and checkout unchanged.
- `/usr/bin/python3 -I artifacts/postgresql-row-read/live.15vqtfey/driver-acquire.py`: FAILED terminally; eligible read returned true normally after cancellation. Reader/native cleanup passed; cancellation and recovery remain unqualified.
- `/usr/bin/python3 -I artifacts/postgresql-row-read/verify-checkpoint.py`: PASS for evidence integrity and safe disposition, explicitly retaining P4 FAILED and no recovery qualification;66 source files,137 protected compiler/configuration inputs,12 archives/payloads and336 ordinary binaries. Historical inspectors/results remain unchanged.
- Document front matter/fences/local links and `git diff --check`: PASS. D2 source/design clarity, P4 observed failure classification and final scoped integrated review: Oracle CLEAR. That verdict preserves P4 FAILED and all public/data/coverage/full-roadmap gates.

Current production evidence is protected by rehashing137 compiler/configuration inputs,12 archives/payloads and336 ordinary binaries. Prior V1v86passed/325unconfigured SQLServer skips/247translations/zero warnings is retained evidence, not a new run. No production/dependency/lock/publication change occurred in this checkpoint.

## Follow-ups

Any future row-stream experiment must first define a source-reviewed question permitting already delivered row completion, distinguish toolkit cancellation policy from native acknowledgment, and preserve this failed P4 run. Owner acceptance of the public execution contract is still required before production. Native writes/generated keys/types/concurrency, OS matrix and all roadmap milestone gates require their separately authorized evidence.
