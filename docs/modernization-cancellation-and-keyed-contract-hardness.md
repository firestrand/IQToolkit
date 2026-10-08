---
title: Cancellation probe and DynamoDB design hardness report
version: 0.1
last_updated: 2026-10-03
status: Scoped integrated evidence and Oracle CLEAR
owner: IQToolkit project owner
tags: [modernization, atlas, postgresql, dynamodb]
---

# ATLAS: cancellation probe and keyed-contract design

This report closes only independent [P3 feasibility](modernization-postgresql-cancellation-probe.md) and [D3 design preparation](modernization-dynamodb-contract.md). It does not close M0/M1/M2/M3, approve a public contract or qualify another platform/provider. The owner-selected .NET1.1.0-draft.1 standard remains authoritative; existing fixture, coverage, policy and public-contract decisions are unanswered.

## Edge cases and observed outcome

P3 observed one eligible reader-acquisition attempt, with a distinct observer backend reporting the target's actual active/Timeout/PgSleep state and new query_start while its task remained incomplete. TargetPID2817220 and observerPID2817221 belonged to a fresh private18.6 cluster, system_identifier7692523652508630171. Source/configuration choices are operation controls; activity, errors, rows and times are actual outputs.

Cancellation preserved the same exception reference, original operation token and OperationCanceledException→PostgresException chain. Native SQLSTATE57014 carried the user-request message derived from archive-verified PostgreSQL source; a statement timeout sharing that SQLSTATE could not satisfy the declared result. The callback took3.558627ms and operation settlement4.344048ms from cancellation entry on this run. These are measurements, not SLAs or proof of a nonblocking callback. Source establishes that the callback uses a synchronous separate connection.

Recovery reused the same PID/backend_start, matched all fresh native metadata, eight JSON/typed catalog rows and32 schema columns, and left the target idle. Loaded Npgsql10.0.3 DLL hash matched the verified signed package's net10 asset. Both connections became Closed, the target backend disappeared, native fast shutdown exited0 and no owned native process/socket remained. No ineligible attempt, timeout/fallback, broken connection, error retry, callback hang or forced cleanup occurred; rejection predicates/source review do not substitute for observing those paths.

The disposable source checked the two-second polling window before each await rather than at both acceptance points. Independent review identified this limitation. It does not invalidate this actual run: cancellation entry was2.735446ms and the complete driver process lasted0.437958248s inside a1.553251817s server lifetime, containing both observation loops. The integrated inspector requires this conservative actual containment proof. Executed source remains unchanged. Any future harness must recheck elapsed time after the awaited observation, before eligibility/disappearance acceptance, and verify the overall token before success. General enforcement of a two-second polling ceiling is not qualified.

D3 reviewed Query-versus-Scan filter rules, classic versus current multi-attribute GSI scope, strong-read rejection, absent/null/projection mapping, empty-page continuation, returned/evaluated-item accounting, cursor scope/protection, SDK retry ownership, conditional conflicts and uncertain writes. Official metadata for SDK candidates was retained and hash-verified. These are source-grounded proposal checks; no AWS/local SDK runtime, fixture, write or resource was exercised.

## Tool and service failure handling

P3 source/data/prospective Oracle CLEAR preceded dispatch. The unchanged exact three-node P2 lock restored into a new private cold cache, with signed archive SHA512 validation and distinct logical NuGet content hashes; SDK build reported0 warnings/errors. Restore/build/server commands use owned bounded child processes and recorded terminal exits. Native children create separate sessions, so P1's executable/PID/start-time recheck and pidfd cleanup remains necessary. Actual cleanup needed no fallback signals and reported no errors. Active experimental app/cluster/socket/helper code is removed after protected source/output snapshots; failed cleanup would remain a separate failure.

No semantic failure was retried or turned into a skip. Prospective review corrected the ineligible-result guard before execution: only normal completion or deliberately requested exact native-user-cancellation may permit another attempt. No setup/compile/runtime failure occurred in this packet. Positive behavior is established only on the observed host/profile.

## Concurrency and load

Root exclusively owned SDK/service dispatch. Workers reviewed source or wrote only the explicitly assigned D3 document. Target and observer used separate physical connections without explicit observer transactions, prefetched schedules or concurrent commands on the same connection. One controlled read-only sleep operation was observed; pending row reads, large-stream memory, throughput and transaction behavior remain separate experiments.

D3 proposes lazy page traversal without prefetch, isolated mutable requests/budgets, finite call/evaluation/deadline bounds and explicit owned-versus-borrowed SDK client lifetime. No concurrent AWS behavior or hard monetary bound was measured. Local emulation cannot certify AWS consistency, conflict or billing semantics.

## Security and disclosure

Peer authentication, reject host rules, empty TCP listen_addresses,0700 private Unix socket/data/evidence and controlled environments were verified. No credentials, cancellation secret key or wire packet was recorded. SQL only reads engine metadata and performs the controlled sleep; no user tables, records, extensions, system install or public listener was introduced. Binary/source/pin/lock hashes bind the executed dependencies.

D3 proposes short-lived identity, validated endpoints, explicit tenant and cursor scope, protected continuation and safe structured diagnostics. Keys/documents/condition values/continuations and raw payload-bearing errors are excluded from default telemetry. Cursor protection, security policy, data approval and AWS resource/cost ownership remain owner decisions.

## Verification commands and results

- `/usr/bin/python3 -I artifacts/postgresql-cancellation/sdk.du8alrba/sdk-prepare.py`: PASS, three exact packages, unchanged reviewed lock/source, zero-warning/error build, unchanged repository snapshot.
- `/usr/bin/python3 -I artifacts/postgresql-cancellation/live.sftd82ac/cancel-acquire.py`: PASS, fresh native oracle and active cancellation/same-backend recovery, cleanup success, unchanged repository snapshot. Active scripts are retained as nonactive .snapshot evidence after disposition.
- `/usr/bin/python3 -I artifacts/postgresql-cancellation/verify-checkpoint.py`: PASS,59 retained source files,137 unchanged compiler/configuration inputs,12 unchanged archives/payloads,336 unchanged ordinary binaries and safe disposition/no owned native processes.
- Saved-document front matter, fenced blocks, local links and `git diff --check`: PASS in the scoped review.
- Oracle: prospective P3 source/data and D3 source/clarity CLEAR; observed P3 qualification independently confirmed; final integrated P3/D3 verdict CLEAR.

The ordinary V1v evidence remains protected:86 tests passed,325 unconfigured SQL Server tests skipped,247 translation checks passed, zero warnings. Current inputs/payloads/binaries are rehashed; no unnecessary production SDK rerun or broader backend certification is claimed. Full roadmap remains active, with required public/data/coverage/environment/support gates open.
