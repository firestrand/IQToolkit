---
title: SQLite Math.Sin identifier culture hardness report
version: 0.3
last_updated: 2026-10-04
status: Scoped repair, diagnostic compatibility and current final verified
owner: IQToolkit project owner
tags: [modernization, sqlite, globalization, atlas]
---

# Hardness report: SQLite Math.Sin identifier cultures

V1ad in the [implementation plan](modernization-implementation-plan.md) repairs culture-sensitive uppercasing of SQLite Math method identifiers. `SQLiteFormatter.VisitMethodCall` now calls `ToUpperInvariant()` instead of `ToUpper()`. Every other production byte, including source line positions, is preserved.

The owner-selected [engineering standard 1.1.0-draft.1](standards/source/Cross-Platform-DotNet-Engineering-Standard-1.1.0-draft.1.md), development guides and AGENTS_TEMPLATES govern this checkpoint. DOTNET-GLOB-001 requires culture-independent machine identifiers; DOTNET-TEST-001 requires explicit infrastructure and preserved coverage baselines. The observations apply to Linux ARM64, SDK 10.0.401/runtime 10.0.12, Microsoft.Data.Sqlite 10.0.12 and native SQLite 3.53.3.

## Edge cases tested

The native engine supplies a fractional Double through `julianday(substr(sqlite_source_id(),1,19))`. Its actual function catalog must expose built-in one-argument `sin`. A separate ordinary SQLite connection executes parameterized `SELECT sin(@value)` to produce the independent oracle. Tests reacquire input and oracle on each execution; captured results are evidence rather than future fixtures.

| Case | Original formatter | Repaired formatter |
| :--- | :--- | :--- |
| Native prerequisites | Acquisition passes. | Acquisition passes. |
| Invariant culture | `SIN` produces the oracle's REAL/Double result and exact bits. | Same result and schema. |
| Installed Turkish culture | `SİN` fails with native `SqliteException`, codes 1/1, unknown function. | `SIN` produces the same oracle result and schema. |
| Culture restoration | The original culture reference is restored, including after the native error. | The original culture reference is restored. |

The causal RED is `red.b18i41v7`: acquisition and invariant controls pass, Turkish evaluation fails. GREEN is `green.1_odmlyu`: all three methods pass with identical test source and all six Test DLL/PDB outputs unchanged across RED and GREEN. Only the six expected full SQLite DLL/PDB copies change during the repair; later strict compilation propagates two harness copies.

Other Math methods, boundary values, dialects, cultures and platforms remain unqualified. These tests exercise the public formatter and native engine; they do not establish complete ORM, cache or localization behavior.

## Tool and service failure handling

`coverage.fr__v_c4` failed during source preparation because a SHA string comparison used inconsistent casing. No SDK command executed. The failed receipt and original source are preserved; seven private files were retained and two project filenames made inactive before the owned original directory was removed. The corrected preparation normalizes seal-map hash casing without changing the native collection profile.

Collection and comparison use separate reviewed packets. Collection preserves actual native outputs before the comparison packet is constructed. Complete measured inputs determine the comparison allocation; the private helper permits 4,096 paths, 384 MiB total retained input bytes and 16 MiB per file. Native supervision retains 480-second parent, 120-second leaf, 16 MiB log and 512 MiB complete-owned-storage bounds. Private archival has a separate 30-second bound and verifies copied bytes before removing only its owned original parent.

Original failed evidence remains failed. No coverage target or exhausted optimized-cache attempt limit is relaxed by this checkpoint.

## Concurrency and load considerations

The test class uses `DoNotParallelize` and restores `CurrentCulture` in `finally`. Root serializes SDK execution; workers receive bounded source-only or read-only leases. No concurrent provider or performance claim is made.

## Security and data exposure

Inputs come from local native engine metadata. The independent oracle binds the actual Double as a parameter. Observations contain no credentials, production data or user records. Local package-consumer and verification environments are retained with inactive project filenames; no package, remote Git operation or service is published.

## Verification commands and results

Literal command arguments, hashes, logs, native results and terminal process checks are retained in each run's `packet.json` and `result.json` under `artifacts/sqlite-math-culture/`.

| Verification | Retained run | Observed result |
| :--- | :--- | :--- |
| Native acquisition | `acquire.meb_1d_2` | Four commands exit 0; one pass; Oracle CLEAR. |
| Causal RED | `red.b18i41v7` | Command exits 0/0/0/1; two controls pass and Turkish alone fails; Oracle CLEAR. |
| Identical-test GREEN | `green.1_odmlyu` | Four commands exit 0; three passes; Oracle CLEAR. |
| Full strict suite and translation | `strict-current.iy8gmcf7` / `run.smNIdDh4` |427 definitions, 102 passes, 325 explicit SQLServer skips, 247 translations; zero warnings/errors; Oracle CLEAR. |
| API and packages | `api.jb6r5wb4` / `run.Ms5Q9VUr` | Six compatibility validations; twelve archive DLL/PDB payloads match; Oracle CLEAR. |
| Local symbols | `symbols.rjxtshez` / `run.UMCl0k72` | Six archive pairs, 135 source checksums and SourceLink maps; Oracle CLEAR. Remote retrieval unverified. |
| Independent consumers | `consumers.78w957rd` / `run.EyuxrTHW` | Six profiles, seven graph reviews and native SQLite executor pass; Oracle CLEAR. |
| Native collection | `coverage.nx5khe0v` | Eight commands exit 0; 219 copied outputs and 336 ordinary binaries preserved; Oracle CLEAR. |
| Source-changing comparison | `qualification.1p9l6zbh` | Four commands exit 0; 16,724 leaf identities and 97 source paths preserved without regression; Oracle CLEAR. |
| Durable compilation preflight | `continuity-controls.q5b4hzbp` | Eight expected exits 0/0/0/1/0/0/1/0; authentic old-helper/profile rejections and current MathDescriptor acceptance; Oracle CLEAR. |
| Historical diagnostic compatibility | `cache-controls.t8c2glp3` | Nine expected exits 0/0/0/1/0/0/0/1/1; authentic legacy-scope and failed-producer rejections; current diagnostic replay accepted; Oracle CLEAR. |
| Fresh current final and fact execution | `current-final.lqzqnp2s` / `run.nZsVfFZQ` | Nine component passes; assessment exit 1 with verified evidence integrity and five readiness gaps; 33 facts/87 scoped passing methods; observed Oracle CLEAR. |

Consumer refresh changes exactly 18 toolkit content hashes across seven lock files plus six feed SHA entries. Versions, dependency edges and external-package hashes remain unchanged; original bytes and the exact-copy receipt are retained.

Coverage rises from 2,738/8,353 to 2,748/8,353 covered lines and from 1,460/5,320 to 1,480/5,320 covered branches. Both formerly uncovered line 371 entries are covered. The comparison preserves 15 permanent historical pins and 973 historical snapshots, adds 1,181 retained inputs, and verifies 1,748 sealed inputs with all336 ordinary binaries unchanged. It completes in 3.907778 seconds using 386,525,518 owned bytes; 51 private files are retained with one inactive project filename mapping and the original parent absent.

## Scope and follow-ups

The adopted verification tool replays the math transition before all earlier histories. Twenty-nine validator blocks preserve the executed private algorithm. It authenticates the emitted receipt and all 1,181 origin/snapshot pairs before installing five historical aliases. Earlier aliases retain precedence; current capture remains strict.

Both tool builds report zero warnings/errors. The current preflight retains 2,422 inputs totaling 346,458,841 bytes within unchanged 4,096-input/384 MiB bounds. The separately reviewed eight-command packet uses a 2 GiB complete storage bound and completes in 9.551573 seconds using 774,701,121 bytes. All 2,631 seals, source/index and 336 ordinary binaries remain unchanged. Its 59 private files are retained with two inactive project filenames and the original parent absent. This qualifies compilation and history replay, without executing cache compatibility or the fact classifier.

The subsequent diagnostic compatibility packet uses the reviewed 6,144-path/640 MiB coordinator limits, with 16 MiB per file and unchanged 128 MiB native-stage limits. Its conservative four-store allocation is 2,795,759,897 bytes within a 4 GiB complete-owned bound. The actual nine-command run completes in 20.324923 seconds using 2,086,392,991 bytes; both helper builds have zero warnings/errors. All 3,898 sealed inputs, checkout/index and 336 ordinary binaries remain unchanged. The 61 private files are retained with two inactive project mappings and the original parent absent.

The current compatibility receipt authenticates exactly 19 source deltas and 18 additions. Compared with the historical producer, 306 ordinary binaries remain identical; 16 Data, six Test and eight SQLite changes have authenticated original bytes. The six Test originals retain their native-null acquisition provenance. Classifier changes are exactly the two count guards, and eight consumer copies preserve their complete dependency graphs while refreshing 18 toolkit content hashes and six feed hashes. The replay preserves all 14 original reports, 4,676 stage bindings and 68 unique executions: 62 passes and six intended assertion failures. This qualifies saved diagnostic evidence against current source, without establishing optimized cache behavior. All three historical seed attempts retain their original outcomes and exhausted retry cap.

Observed V1ad.4b Oracle review is CLEAR with no findings. The protecting check authenticates all four logical/physical capture sets, 3,898 seal/snapshot pairs, 336 binaries and 61 archived files. Fact execution and current-final qualification were separate subsequent gates; their observed results follow below.

Pass: the tested SQLite Math.Sin identifier repair, inherited strict results, compatibility, local symbols, independent consumers and the private native coverage transition.

The 33-row binding manifest is prepared and source-reviewed CLEAR. All 32 inherited rows and 84 method bindings are preserved; three sine methods, two inherited production hashes and two classifier count guards are the exact changes. That source-only preparation preceded the separately qualified durable classifier execution below.

Integrated V1ad.0–.3e Oracle review is CLEAR for this tested repair, its protecting evidence and exact static binding preparation.

The fresh current final run qualifies all nine components and retains the expected assessment exit 1 with verified evidence integrity. Actual strict results are 427 definitions, 102 passes, 325 explicit SQLServer skips and 247 translations. The executed classifier preserves all 33 facts: 17 scoped, 15 unqualified and one not due, with 87 distinct passing method bindings. Current coverage is 2,748/8,353 lines and 1,480/5,320 branches with all 97 sources/16,724 leaves preserved. Preflight retains 2,422 logical/physical inputs using 346,460,582 bytes; assessment retains 3,867 logical inputs/3,556 physical snapshots using 446,931,166 bytes within the reviewed 6,144-path/640 MiB bounds.

All 3,900 seals, checkout/index, 336 ordinary binaries and 21,059 prior-evidence files remain unchanged. Actual runtime is 213.289366 seconds with 2,350,829,452 owned bytes. Ten owned stages settle without forced termination, live descendants, cancellation, cleanup or terminal-evidence failure. The inner wrapper records one failed complete `du` scan caused by three disappearing diagnostic paths; the bounded retry succeeds. Outer storage scan errors are zero. Preserve both observations separately. The owned environment is retained inactive with its original parent absent; complete post-archival storage is 2,350,835,040 bytes.

Observed V1ad.4c Oracle review is CLEAR. A separate metadata-binding checkpoint uses the retained compiled verifier and genuine producers, without new SDK/test/coverage/seed execution; its final result and metadata hashes belong in an ignored epoch receipt. The five readiness gaps remain full-project coverage, final line target, touched-file targets, mandatory fact bindings and optimized cache qualification. The historical V1ac final run remains historical after this source change. Access/MySql coverage, unwaived coverage floors, optimized cache qualification, ownership inventory, public async contracts, backend work and native platform qualification remain open. This report does not close M0 or the full roadmap.
