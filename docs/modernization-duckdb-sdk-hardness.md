---
title: DuckDB private SDK readiness hardness report
version: 0.1
last_updated: 2026-10-03
status: SDK readiness integrated Oracle-clear; ADO gates open
owner: IQToolkit project owner
tags: [modernization, duckdb, sdk, atlas]
---

# DuckDB private SDK readiness hardness report

The [Q0.3 packet](modernization-duckdb-sdk-probe.md) produced a genuine reviewed four-package lock, then passed a second empty-cache locked restore and strict Release build. This is SDK feasibility evidence for required LOCAL-RM-M5. No application binary, ADO connection or database operation was executed.

## Edge cases and identities

Sole-direct Data.Full 1.5.6 resolves Bindings.Full 1.5.6, Arrow 23.0.0 and Scalars 23.0.0. The actual lock SHA256 is `1867c8d9f292a6fe2b4c075b169ba2dd40f6e95dcc1045dcd851711af10f47f2`; Oracle independently cleared its exact nodes, edges and asset selection before cold dispatch. Data/Bindings select net10.0 and Arrow/Scalars net8.0. No ProjectReferences or unexpected package libraries.

Both restored caches use only the acquired local feed, with exact package mapping, required signatures/current repository certificate, auditing enabled for all packages at low severity and warnings treated as errors. SDK acceptance under that policy and empty diagnostics are observed; no independent per-package certificate report or universal security guarantee follows. Retained vulnerability data contains no entries for these four IDs at acquisition time.

The actual hash domains are distinct: lock.contentHash = assets.sha512 = cache metadata.contentHash; cache `.nupkg.sha512` = complete signed archive SHA512 = approved catalog SHA512. An initial read-only inspection incorrectly equated those groups and failed; corrected inspection and an independent reviewer verified the actual separation. SDK outcomes were unaffected. No authored hash was inserted into the generated lock.

The 13-file compiled output includes runtime assets for multiple architectures because the profile requests no RID. Copied Linux ARM64 libduckdb.so matches the independently verified 62,945,312-byte native payload SHA256 `23686593dba4ec22c6e581bb1c843b3964e84c4bc4d0063d8b4daddd2e20e30a`. This establishes copying identity only; the earlier native checkpoint loaded that payload independently, and ADO loading remains untested.

## Tool failures, concurrency and security

All five SDK commands exited zero, with no surviving owned group members, forced termination, cancellation, cleanup or terminal publication errors. Generate receipt 43 inputs/39 snapshots and cold receipt 49 inputs/45 snapshots matched before/after; complete normal checkout snapshots remained unchanged during each stage. Workers performed read-only source/review work, and root serialized SDK dispatch.

The private runner checks cancellation immediately before launch and after completion. Command failures preserve their primary type while cleanup/log observations are recorded separately. Fifteen-second SDK identity and 120-second restore/build deadlines, five-second cleanup, 1 MiB output and 2 GiB owned storage rejection thresholds were fixed prospectively. Polling permits overshoot; no hard disk/RSS cap or cross-platform runner certification is claimed. Forced termination cannot qualify a passing stage.

Explicit private CLI home/package/HTTP/scratch/tmp locations, empty parent MSBuild imports, pinned SDK, controlled environment, direct executable argv and disabled build servers avoid ambient configuration. No credentials, user records, provider fixtures, public listener, account, installation or production dependency changes. Official audit/certificate services were read for tooling policy only.

## Verification and limits

Historical commands, each invoked once after its own prospective Oracle CLEAR:

```text
/usr/bin/python3 -I artifacts/duckdb-probe/sdk.cjkgmfg9/sdk-probe.py generate
/usr/bin/python3 -I artifacts/duckdb-probe/sdk.cjkgmfg9/sdk-probe.py cold
```

Generate restore: exit 0, 2.101399267 seconds. Cold locked restore: exit 0, 2.135730354 seconds. Release build: exit 0, 1.290168456 seconds, zero warnings/errors. These are observed process durations, not database latency or repeatable performance claims. Evidence resides under `artifacts/duckdb-probe/sdk.cjkgmfg9`; exact source and outputs are retained for review. Disposition and integrated protection verdict are recorded at closeout below.

ADO must next prove effective controls and unchanged actual native catalog/schema/null/value equivalence under a separately reviewed packet. Its automatically added duckdb_api setting prevents claiming identical complete startup options. Parameter behavior, async I/O, large-result streaming, import, provider APIs, platform support and complete roadmap implementation remain open. All pending human decisions and historical failed evidence remain preserved.

## Closeout

Observed Oracle CLEAR covers both SDK outcomes, graph/hash domains, all 13 output identities and clean terminal evidence. Nine source snapshots and exact output bytes were preserved before removing the owned outside app; the helper is a nonactive snapshot. Private caches remain owned offline provenance until the next reviewed SDK/ADO checkpoint or goal closeout. The new SDK protection inspector passed:119 source hashes,137 unchanged production inputs,12 archive/payload identities and336 ordinary binaries; integrated Oracle review is CLEAR for this SDK checkpoint only. Historical checkers remain unchanged. Full roadmap and public/coverage/data/support gates are still active.
