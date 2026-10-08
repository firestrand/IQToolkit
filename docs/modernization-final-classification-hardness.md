---
title: Final verification classification hardness report
version: 0.1
last_updated: 2026-10-03
status: Scoped verification; integrated Oracle CLEAR
owner: IQToolkit project owner
tags: [modernization, verification, testing, atlas]
---

# Final verification classification hardness report

Task V0.6b adds executable fact and touched-file classifiers to `eng/verify-final.sh` and fixes the coverage verifier's invalid-argument output ownership. The fresh integrated command passes its component gates and rejects final readiness with actual classified gaps. V0.6, M0 and the full roadmap remain open.

Technical work uses the owner-selected ENG-STD-DOTNET-XPLAT 1.1.0-draft.1. Its repository snapshot remains byte-identical to the selected Downloads file, SHA256 `8c56930ec1cd26e8b58a273d47cb0b9713f55e6d4961b6d29a636ffaf4f0ebe2`. The development-plan and autonomous guides retain their existing coverage and evidence obligations. No alternative policy, fixture permission or public contract was adopted.

## Edge cases tested

The classifier preserves all 32 fact-ledger rows, including exact claims, lifecycle, evidence IDs and raw-row hashes. Sixteen scoped facts bind 84 distinct native methods to their reviewed test/oracle/production source hashes, matching compiled DLL/PDB identities and method-body source documents. Every method has a unique linked passing execution in both this invocation's strict and coverage TRX, with distinct paths and execution IDs. Fifteen broader, proposed, historical-only or external facts remain unqualified; async is explicitly due in M1. Passing scoped methods do not activate broader facts.

Changed-file classification authenticates baseline commit `60ddef60d4b635c4b24b78bff4327e3f3ff9ca5e`, its tree and exact native Git inventory, then checks current source blobs. The 170 changed paths receive explicit dispositions: 82 unmeasured, eight below target, 77 non-runtime, one line-qualified file without a branch denominator and two reviewed removals. Only the exact V1t core-settings removals have removal provenance; unreviewed deletions reject. Command scripts retain executable CLI targets. Unknown layers and missing measurements cannot become blanket exclusions.

Line counts aggregate unique source/line leaves from class-level coverage only. Method duplicates are excluded; native class/line branch fractions remain separate, and individual branch-arm identity is unsupported. Current examples remain below their unchanged targets:

| File | Unique covered lines / lines | Covered branches / branches | Layer target |
| :--- | :--- | :--- | :--- |
| QueryCache.cs | 56 / 71 | 28 / 38 | 95% |
| MostRecentlyUsedCache.cs | 51 / 59 | 13 / 18 | 95% |
| DbEntityProvider.cs | 246 / 322 | 80 / 122 | 92% |
| SqlFormatter.cs | 192 / 552 | 165 / 469 | 95% |

MRU has 60 raw class-level leaves; source/line deduplication produces 59 unique lines. TestHarness and engineering executables remain unmeasured. No threshold was reduced.

The authentic retained coverage verifier first reproduced the invalid-argument defect: native exit 1 and rejection, followed by two empty input-map writes into an owned working directory. After the validated-retention guard, the same zero-argument control returns the same native rejection and leaves the directory empty. Both controls preserve inputs during execution and leave no owned process. The fresh successful coverage comparison protects legitimate retained-input writes.

## Tool and service failure handling

Missing, linked, oversized or changed evidence; duplicate identities; unsupported manifests; missing source documents; and ambiguous compiled methods reject integrity. Readiness rejection qualifies only after every component stage succeeds. Setup, build, cancellation, timeout and cleanup errors cannot satisfy this control.

Commands use the existing framework-only helper templates and empty feeds. Ordinary stages have 120-second bounds, compound coverage 300 seconds and the parent qualification 900 seconds. Logs are bounded to 16MiB and newly owned scratch/evidence to a polled 2GiB threshold. At most three complete storage scans may retry, with 20ms pauses and retained errors. This qualification required no storage retries or forced cleanup.

The source review corrected two concrete fail-open cases before runtime: arbitrary deletions receiving the settings-removal rationale, and executable `.cmd` files receiving metadata exclusions. Their review history is retained; no fabricated provider rows, responses, coverage XML, TRX or approval record was created.

## Concurrency and load considerations

Root serialized all SDK operations. The full command authenticated ten owned stage sessions, finished in 197.608 seconds and retained 1,135,399,292 owned bytes. All sessions are terminal with no observed live members, cancellation or cleanup error. This supplies no provider concurrency or load guarantee.

The private helper application and parent workspace were archived with all bytes preserved; two helper project filenames were recorded as inactive snapshots. Original temporary paths are absent. Current fresh tool source is retained inside the final run. The earlier four-command helper build predates the final cross-run identity checks; those latest checks compiled and executed in the fresh integrated command.

## Security and data exposure

Source and evidence paths are bounded and retained with hashes. XML disables DTD resolution, JSON rejects duplicate properties, and compiled-source checks use portable-PDB SHA256 documents. Layer exclusions do not waive native providers or executable scripts. Technical Oracle review is separate from owner approval, protected governance, tenant security and public API decisions.

## Verification commands and results

The fresh root qualification is `artifacts/final-classification/qualification.l4pgyyt6/result.json`. Its reviewed command is `bash eng/verify-final.sh artifacts/api-baselines/runs/run.5mbh8iJw/packages`. All nine component stages return 0; final assessment returns the required 1 with verified evidence integrity.

| Evidence | Native result |
| :--- | :--- |
| Final run `run.b57V768R` | Five explicit readiness gaps; fact and touched-file gaps now reference executable receipts |
| API `run.oTR9IeUW` | Six comparisons against authentic 1.1.1; 12 DLL/PDB archive payloads match current ordinary outputs |
| Symbols `run.im8t11pV` | Six package/symbol pairs; local source checksums verified |
| Consumers `run.bnW3dAgC` | Seven hash-only graph reviews, six cold locked compiles and native SQLite consumer |
| Strict `run.j51DFlqA` / results `run.cteH7VU8` | 416 discovered, 91 passed, 325 explicit SQL Server skips, 247 translations; zero warnings/errors |
| Coverage `run.4tzg5zmc` / comparison `run.kCm2viFp` | 97 sources, 16,716 leaves; no regression, 2732/8349 lines and 1460/5320 branches |
| Argument controls `coverage-arguments-red.h0c00t4g` / `coverage-arguments-green.ly9ayn4b` | Observed cwd writes → same rejection with no cwd writes |
| `artifacts/final-classification/protecting.json` | 137 production/configuration inputs, 336 ordinary binaries and 43 canonical rules unchanged; 1035 retained input snapshots checked |

The parent preserves all 312 reviewed inputs and the full checkout during execution. Post-run plan/state/catalog/README/report updates are documentation deltas; the feed remains archived exactly as tested, with its earlier bundled README. No repack, publication or remote Git action occurred.

## Remaining work

Pass: scoped executable classifications, causal output-ownership repair, fresh component gates and preservation checks. Fail: complete coverage scope and targets, all due fact bindings and six cache seed challenges. Broader null/boundary/concurrency/ownership behavior, unanswered fixture/policy/public/data decisions, hosted platforms and later providers remain required. Catalog adoption remains PARTIAL; no full standards-compliance or roadmap-completion claim is made.

The next independently ready packet is design and qualification of the six fixed cache sensitivity categories against unchanged native evidence. Same-instance policy replacement remains a separate unresolved V1c.6 contract.
