---
title: Native cache sensitivity hardness report
version: 0.2
last_updated: 2026-10-04
status: Scoped verification; integrated Oracle CLEAR
owner: IQToolkit project owner
tags: [modernization, cache, testing, atlas]
---

# Native cache sensitivity hardness report

Task V0.6c qualifies all six named cache defect challenges under an explicitly diagnostic compilation/runtime profile. Each intended native assertion fails with its passing typed control; initial, restored and final seven-test runs pass. This report records the historical V0.6c outcome. Subsequent diagnostic final-gate binding is recorded in the [V0.6d final evidence binding report](modernization-final-binding-hardness.md). Optimized-artifact qualification, broader cache contracts, M0 and the full roadmap remain open.

Technical work follows the owner-selected ENG-STD-DOTNET-XPLAT 1.1.0-draft.1, SHA256 `8c56930ec1cd26e8b58a273d47cb0b9713f55e6d4961b6d29a636ffaf4f0ebe2`, and the development-plan/autonomous guides. No coverage target, fixture permission, policy requirement or public contract was waived. The prior source fixes and ordinary build outputs are unchanged.

## Edge cases tested

The experiment uses unchanged native-input tests and a single detached worktree overlaid with the exact current source snapshot. Native SQLite supplies actual compile options and policy metadata, with independent parameterized SQL controls. Missing acquisition, padding, setup exceptions and compiler failures cannot count as defect detection.

| Defect category | Intended native failure | Source line |
| :--- | :--- | :--- |
| Untyped equality execution | IsInstanceOfType: expected enumerable, actual Boolean | CacheMetadataTests.cs:38 |
| Same-CLR-root policy bypass | Second CollectionAssert.AreEquivalent | NativeProviderCacheTests.cs:128 |
| Wrong-entry hit | AreSame for the requested native option | CacheMetadataTests.cs:63 |
| Nonadding miss returns an entry | IsNull for the miss output | CacheMetadataTests.cs:51 |
| Disabled recency promotion | IsTrue for the retained native option | CacheMetadataTests.cs:77 |
| Frozen captured parameters | Second AreEqual after the captured selection changes | CacheMetadataTests.cs:186 |

Each patch changes one reviewed production source file. The native failure must match the exact test, assertion family, source document and intended line in its authenticated compiled method. The typed single-execution control passes for every seed. Exact baseline bytes are restored and rebuilt after each seed; all seven methods pass before continuing. No seed substitutes for another category.

The policy challenge concerns different immutable provider/root instances of the same CLR type. It does not resolve V1c.6 same-instance policy replacement, in-place mutation, tenant isolation generally, concurrency or cache boundary behavior.

## Tool and service failure handling

The first two attempts remain FAILED. Both pass their initial seven-test baseline and observe the intended untyped type assertion plus passing control. The optimized stack nevertheless reports allocation line 35 rather than assertion 38. Attempt 2's test-only no-inlining setting does not correct that attribution. Neither attempt supplies a qualified killed category.

Attempt 3 adds `-p:Optimize=false` to exactly 13 private graph rebuilds, retaining Release configuration, pinned SDK, locked restore, analyzers, CI settings, Source Link, portable PDBs and warnings-as-errors. Exactly 14 native test processes use `DOTNET_JitNoInline=1`; restore, build and evidence-verifier processes retain their original environments. Initial, mutant, restored and final runs all use the same declared diagnostic profile. The exact assertion-line check remains unchanged and succeeds empirically for all six failures.

The installed runtime records VMR commit `95017c711e6afc1085133d440e42b4bd78155701`. Its immutable [configuration declaration](https://github.com/dotnet/dotnet/blob/95017c711e6afc1085133d440e42b4bd78155701/src/runtime/src/coreclr/jit/jitconfigvalues.h) and [inlining implementation](https://github.com/dotnet/dotnet/blob/95017c711e6afc1085133d440e42b4bd78155701/src/runtime/src/coreclr/jit/importercalls.cpp) ground the chosen runtime setting. The observed evidence establishes diagnostic-profile sensitivity, not optimized execution or performance.

The controller enforces 120 seconds per process, 900 seconds overall, 16MiB per log and a polled 2GiB owned storage bound. It allows at most three complete storage scans with 20ms retry intervals; no exclusions or threshold reductions. This was attempt 3 of 3. All attempts preserve failures and restore sources. No forced termination, remaining live group, cancellation, storage retry or cleanup error is recorded.

## Concurrency and load considerations

Root serialized all SDK and native execution. The successful run completes 58 commands in 101.214 seconds and records 817,462,528 owned bytes before archival. It exercises no provider concurrency/load contract.

The root checkout, index, 324 sealed inputs and all 336 ordinary DLL/PDB files remain unchanged during the successful run. Actual before/after index hashes are retained. Subsequent plan/state/README/catalog/report changes are explicit documentation deltas.

## Security and data exposure

The verifier authenticates native TRX identities/outcomes, DLL/portable-PDB pairs, current source checksums, compiled method bodies and assertion sequence lines. It rejects unexpected discovery/results, skips, nonassertion failures, linked or oversized inputs, JSON duplicate properties, XML DTDs and terminal input changes. Native observations and actual messages/stacks are retained; no provider rows, responses, TRX, coverage XML or approval records are fabricated.

Each attempt's 324 sealed execution inputs and 713 environment files are preserved byte for byte. Eleven project filenames per environment are mapped to inactive snapshots. Original temporary paths are absent; only the owned worktree registrations were removed, with native exit 0. No global worktree pruning or remote Git action occurred.

The successful run retains 14 separate sets of 219 compiled output files and 4,676 stage-specific input bindings. Binding keys include stage, original logical path and hash because rebuilds reuse paths with different bytes. Seed source aliases resolve to the exact seed snapshot; unchanged source aliases resolve to retained baseline bytes. Native descriptors, TRX and physical/virtual source-root mapping bytes are unchanged. These aliases prove preservation after relocation; they do not claim another native execution.

## Verification commands and results

Root ran `python3 artifacts/cache-sensitivity/run.py artifacts/cache-sensitivity/run.3_dhn7xv` after independent prospective CLEAR. Exact argv, environments, exits, logs and input identities are retained in that run's packet and result.

| Evidence | Result |
| :--- | :--- |
| `run.am_lcsgh` | FAILED: optimized exact-frame qualification; original evidence retained |
| `run.lq_ht8hn` | FAILED: no-inlining exact-frame qualification; original evidence retained |
| `run.3_dhn7xv/result.json` | PASSED under the declared diagnostic profile; six intended assertion kills and verified restoration |
| Fourteen native reports | 68 unique executions: 62 passes, six intended failures, no skips |
| Thirteen private graph rebuilds plus verifier build | Zero warnings/errors |
| `protecting.json` | Exact 58-command chain, six source deltas/restorations, 3,066 compiled files and root fences checked |
| `retained-stage-inputs.json` | All 4,676 logical-to-retained input hashes/byte lengths match |
| `environment-disposition.json` | 713 files/11 filename mappings preserved; original parent and owned registration removed |

Independent observed Oracle review is CLEAR for the actual native reports, command receipts, input/compiler/source bindings and preservation. Integrated source/evidence/documentation Oracle review is also CLEAR after correcting two historical state labels. Technical review is not owner approval or whole-phase acceptance.

## Remaining work

At the historical V0.6c checkpoint, all six fixed native cache challenges passed under the declared diagnostic profile with exact assertion attribution, controls, restoration and preserved evidence. Durable final-gate binding was then pending and the final command reported its seed obligation as not executed. The subsequent [V0.6d final evidence binding report](modernization-final-binding-hardness.md) records verified diagnostic binding and the remaining optimized-profile qualification gap. Optimized-artifact sensitivity, reusable CI integration, broader cache/null/boundary/concurrency/ownership facts, full coverage scope/targets, unanswered decisions, platform/backend qualifications and subsequent roadmap milestones remain unqualified or pending. Catalog adoption remains PARTIAL; there is no final-success pointer or full standards-compliance claim.
