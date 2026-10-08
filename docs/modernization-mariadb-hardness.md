---
title: MariaDB source and readiness hardness report
version: 0.4
last_updated: 2026-10-05
status: Source/readiness results; runtime compatibility unverified
owner: IQToolkit project owner
tags: [modernization, mariadb, verification]
---

# MariaDB source and readiness hardness report

This ATLAS report covers the source/readiness stage in [the GOTCHA scope](modernization-mariadb-probe.md). It does not certify a MariaDB provider or an acquisition/native controller.

## Edge cases tested

The first acquisition actually rejected two unreviewed checksum redirects and retained the HTTP 404 for the exact driver tag. Both rejected redirects produced zero-byte local captures before response-body acquisition; the remote body lengths were not observed. A local readback encountered gzip bytes and failed UTF-8 decoding; the original error record and subsequent bounded decode are retained. The annotated server tag was dereferenced to its commit; signature verification remains false. The retained driver archive matches the acquired NuGet raw SHA512 and size. The ARM64 index matches both acquired checksum lists. None of these observations proves runtime protocol/type/SQL compatibility.

## Tool and service failure handling

Four fixed acquisition stages made 19 top-level acquisitions; historical redirect-inclusive wire request counts are unmeasured. the first exited 1 with partial metadata, while the later three exited 0 for their scoped outputs. Each stage creates a distinct directory and exclusively writes responses; an existing stage fails before new requests. Socket timeouts and cumulative checks between reads are cooperative, not hard process deadlines. No cancellation, output-storage exhaustion or complete cleanup/controller behavior was exercised; native dispatch therefore remains gated on a separate reviewed controller/packet. The larger server bundle lacks a digest in the acquired repository file lists and is unqualified. The declared six-package closure needs four missing Ubuntu packages: `liburing2`, `libpcre2-posix3`, `mysql-common` and `libconfig-inifiles-perl`. An independent handoff checks 179 metadata constraints; three cached InRelease signatures subsequently verified against the installed Ubuntu archive keyring. Binary/library/bootstrap compatibility and independent keyring distribution verification remain open.

## Concurrency and load

Root is sole writer of documents, scripts, source receipts and the Git index. Two independent tasks wrote disjoint allocated handoffs and completed; their real thread/turn provenance is retained. HTTP acquisitions were sequential. No concurrent engine queries or load/latency/throughput claims are made. The metadata readback measured the owned evidence area at 744,033 bytes at that checkpoint, below the 512 MiB scope limit; later records add bytes and need a fresh check before archive acquisition.

## Security and data exposure

Only public metadata and local package/source inventories were acquired. No user connection string, password, workload data or remote service credential was requested. Temporary public-CDN signed query strings were removed from affected receipts; original receipt digests, redacted paths and classifications remain, and the original executed script is distinguished from its repaired version. Do not claim the original receipt bytes are preserved. All original 86 review inputs are physically preserved by hash. Revised tools sanitize public URLs and exceptions, close accepted/rejected redirect responses without unbounded draining, count transport attempts including redirects, and retain per-attempt partial/failure captures. HTTP protocol/read exceptions are now captured at each acquisition boundary; available `IncompleteRead.partial` bytes are retained only within the byte limit. Installed stdlib source/MRO confirms this exception is outside the former OSError/ValueError catch set. HTTP and gzip/JSON failure paths were source-inspected; no remote protocol failure or malformed fixture was induced or relabeled. The decode handler records ordinary gzip truncation/corruption and JSON recursion failures so raw-byte receipts and final outcomes remain available. No revised acquisition main was network rerun. No downloaded payload or package maintainer script was executed. No independent release-signing-key validation occurred.

## Verification commands and results

| Executed check | Observed result |
| :--- | :--- |
| `python3 artifacts/mariadb-discovery/acquire-public-metadata.py` | Exit 1: four successes, three retained failures. |
| `python3 artifacts/mariadb-discovery/acquire-metadata-followups.py` | Exit 0: two JSON identities and four observed redirects; executed source snapshot precedes the repair. |
| `python3 artifacts/mariadb-discovery/acquire-checksums.py` | Exit 0: four checksum bodies acquired through exact scoped redirects. |
| `python3 artifacts/mariadb-discovery/acquire-arm-index.py` | Exit 0: listing and actual index acquired; both expected digests matched. |
| Initial Python AST/compile and passive imports of four scripts | Passed in normal mode; AST/passive imports also passed under `python3 -O`. No acquisition main function invoked by these checks. |
| Revised tools and shared HTTP guard: syntax/passive imports, existing-stage refusal and configured transport-budget exhaustion | Five sources passed normal and optimized checks. All four acquisition main functions refused existing stages before HTTP; the exhausted transport budget refused before network dispatch. No new HTTP request ran. |
| Ubuntu InRelease signature verification | Three literal `gpgv` argv invocations are recorded in `ubuntu-signature-verification.json`; all exited 0 with VALIDSIG. This verifies signatures against the installed keyring, not independent keyring distribution or current production patch policy. |
| Retained metadata body readback | 19 local capture sizes/hashes matched; three original acquisition failures retained; rejected redirect remote body lengths unknown. |
| Source/index/binary preservation readback | 14 entry subjects, Git index and all 336 ordinary DLL/PDB hashes unchanged. |

Commands were actually run; records are under `artifacts/mariadb-discovery`. Syntax/import and refusal checks do not establish independent source acceptance. The wider project verification remains failed/stopped from its prior packet; it was not rerun, reset or waived. No new staging, commit, push or release occurred.

## Remaining limits

Source/readiness checks passed within their stated scope. Three original acquisition failures and the original local decoding failure remain historical outcomes. The next work is qualified package/dependency acquisition and private extraction, followed by independently accepted compiler/engine experiments. SQL/type/key/transaction/authentication/resource compatibility, native startup, workload quality, public support decisions and full modernization remain unverified.
