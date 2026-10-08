---
title: DuckDB preparation and analytical proposal hardness report
version: 0.1
last_updated: 2026-10-03
status: Integrated Oracle-clear preparation; runtime and owner gates open
owner: IQToolkit project owner
tags: [modernization, duckdb, analytics, atlas, evidence]
---

# DuckDB preparation and analytical proposal hardness report

This ATLAS covers independent Q0 source/archive preparation, the separate Q0.1b retained-metadata inspection and the [D4 analytical proposal](modernization-duckdb-contract.md). It protects existing evidence and production inputs while making required `LOCAL-RM-M5` more concrete. No adapter, native runtime, SDK restore/build, analytical query, import or public contract is qualified by this checkpoint.

## Edge cases and exact outcomes

Q0.1 **FAILED** terminally, exit1. Its seven commands exited0 with no remaining live owned group members; checkout hashes matched. `readelf -h -d -V` generated1,171,319bytes, exceeding the prospective1,048,576byte per-log limit. The final command-log size assertion rejected it before ELF acceptance or success publication. The original source, result, report and prospective receipts are preserved. Oracle independently cleared this failure classification as an inspection-output problem, not a driver/native defect. There was no semantic retry or raised limit.

Q0.1b **PASSED** once, exit0, after a separately reviewed packet. It downloaded nothing and invoked no subprocess, SDK, native load or SQL. It independently verified the four full NuGet archives against official catalogs, identities/licenses/commits and selected dependency groups; compared actual native bytes against both NuGet and separately acquired official release members; streamed the retained failed-run report with bounded line/section retention. Original acquisition status remains FAILED.

| Observation | Actual evidence |
| :--- | :--- |
| Q0.1b operation lifetime | 0.8264502949896269s, within its60s bound |
| Reviewed inputs | 64 before/after hashes identical; no verification errors or cancellation |
| Native payload | 62,945,312bytes; SHA256 `23686593dba4ec22c6e581bb1c843b3964e84c4bc4d0063d8b4daddd2e20e30a` |
| Independent upstream archive | 37,763,835bytes; SHA256 `b72ed9f05003f5e9d2015f7ceada6416b377d9dd33169cdde3c9e33897856eee` |
| ELF metadata | ELF64/little-endian/AArch64/DYN; SONAME libduckdb.so; no RPATH/RUNPATH in the complete retained dynamic section |
| Retained selected sections | 2,807byte header/dynamic prefix and1,884byte version-needs section;4,691bytes total |
| Runtime qualification | nativeLoaded=false; sdkExecuted=false; runtimeSupportQualified=false |

The needs section has six providers matching six NEEDED libraries, with each declared version count verified. Requirements include GLIBC2.25, GLIBCXX3.4.22, CXXABI1.3.8 and GCC4.5.0. These are binary requirements; they do not establish successful host loader resolution or platform support. NuGet signature-entry presence does not establish cryptographic signature validation.

## Tool and failure handling

Original archive downloads used fixed curl connect/total/independent watchdog limits, exact byte/hash bounds, owned outputs and terminal process-group checks. The original failure shows that a fast producer can exceed a polled output limit before exit; the terminal size guard correctly rejected acceptance. Its excess report remains failed evidence, not a compliant log.

Q0.1b hashes the entire retained report without generating another large report. Lines, retained sections, parser counts, inputs and outputs have prospective bounds. It rejects unexpected identity, schema, section or provider/count conditions. Cancellation/deadline/input-verification failures occur under primary failure handling. Final hashing records per-input errors and attempts independent terminal publication, preserving primary failure. Final hashing still contributes to elapsed-time acceptance. No fabricated error case was executed to demonstrate branches; these failure paths are source-reviewed.

Both initial and revised prospective source receipts remain. Revised Q0.1b has64inputs and60immutable small-input snapshots. Runtime data, lock generation and native loader/ABI inspection need a separately expanded packet; this report grants no next-runtime dispatch authority.

## Concurrency and resources

Root owned all normal writes, acquisitions and inspection. Agents performed source/design/Oracle work read-only; no recursive delegation, SDK/service overlap or production mutation occurred. No DuckDB connection or query was created. Old PostgreSQL failed row-read evidence remains separate and unqualified.

Owned disposable helpers are retained as nonactive source snapshots after terminal checks. Official packages/native payloads and immutable metadata/source/output evidence persist in ignored private paths. No user-owned files, installed system tools or external resources are removed. Platform readiness remains Linux ARM64 metadata only; x64, other operating systems and musl are unqualified.

## Security and contract limits

No credentials, user records, external datasets, provider-shaped fabricated rows or generated series were used. Official immutable sources and genuine archive/report bytes supplied the inputs. Original public asset redirects were recorded only as host/path without signing query parameters.

D4 retains file ownership/bootstrap/allowlist/spill hazards, native/import policy bypass, input-replacement races, uncertain commit outcomes, typed parameter/null/conversion rules, explicit streaming ownership and synchronous driver limits. Oracle required correcting an initial abandonment-cleanup overpromise to early enumerator disposal, explicitly preserving D1's limitation for manually abandoned enumerators. Engine commit attribution was separated from client asset selection. The corrected proposal is Oracle-clear for source and clarity only.

Production M0/M1/M5, public/workload/security/data decisions, generated-data authorization, coverage and support promotion remain open. In-memory READ_ONLY is invalid; the proposed future catalog-memory operation must not claim enforced database read-only access. No method name, RID, successful metadata inspection or proposal clarity supplies genuine async/native/support evidence.

## Verification and remaining work

`/usr/bin/python3 -I artifacts/duckdb-probe/acquisition.k152y57s/acquire-packages.py` → terminalFAILED/exit1; retained exact source and outputs.

`/usr/bin/python3 -I artifacts/duckdb-probe/retained-inspection.x4unbiou/inspect-retained.py` → metadata-onlyPASS/exit0; originalFAILED preserved and64input hashes stable.

`/usr/bin/python3 -I artifacts/duckdb-probe/verify-checkpoint.py` → metadata protectionPASS. It rehashed prior66source snapshots, new25successful immutable sources,137unchanged production compiler/configuration inputs,12package/symbol archive payloads and336unchanged ordinary binaries. It verified selected-standard identity, corrected D4 Oracle-bound document hash, nonactive helper disposition, originalFAILED result and newPASS metadata. Explicit current deltas are12added documents and the same five existing documentation/state files. No owned prior PostgreSQL native process remains. Earlier P4 outcome and inspector are preserved, not broadened. Final integrated OracleCLEAR independently confirmed those identities,64inputs/60snapshots, source disposition and documentation integrity for this preparation checkpoint only.

Historical runtime evidence remains86passes/325unconfiguredSQLServer skips/247translations and zero warnings; no new ordinary test/build execution is claimed here. Reusing that retained protecting gate is justified by direct current compiler/configuration/archive/binary identity checks, not by running a new suite or declaring all roadmap behavior covered.

Next independent work is a separately reviewed native catalog/config/ABI acquisition packet, followed by separately reviewed SDK lock/ADO equivalence. Approved actual import/large-result data and owner decisions remain prerequisites for their dependent implementation. The full roadmap remains active.
