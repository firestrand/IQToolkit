---
title: Recent final-verifier continuity hardness report
version: 0.2
last_updated: 2026-10-04
status: Partial verification; final assessment blocked
owner: IQToolkit project owner
tags: [modernization, verification, atlas]
---

# Recent final-verifier continuity hardness report

This report covers V1aj.1–4, including scoped continuity/cache/consumer controls and the failed fresh final assessment. The final verifier now recomputes the canonical-publication and nullable-column comparisons before replaying the older Math, command, null-array and original compilation history. Full replay selects the current nullable descriptor after terminal input checks. Continuity, cache compatibility and consumer refresh are verified in scope. The fresh final assessment remains FAILED and dependent V1ai.4 closure is blocked.

The technical standard is the owner-selected `Cross-Platform-DotNet-Engineering-Standard-1.1.0-draft.1.md`, SHA256 `8c56930ec1cd26e8b58a273d47cb0b9713f55e6d4961b6d29a636ffaf4f0ebe2`. Development Plan Creation Guide 2.7 supplies Test→Implement and real-data requirements. No fixture data, dependencies, production code or public APIs changed in this checkpoint.

## Edge cases tested

| Control | Actual result | Scope |
| :--- | :--- | :--- |
| Unchanged authentic prior verifier against current files | SDK 0, preflight 1; `Final input identity differs.` | Causal baseline: current formatter differs from the prior compilation. No active pointer. |
| First updated verifier | SDK/restore/build 0; preflight 1; `Coverage source differs from the compiled PDB checksum.` | Retained failed attempt. Older source hashing omitted its available PDB checksum, preventing hash-bound historical resolution. Wrong-context control did not execute. |
| Corrected verifier, complete replay | SDK/restore/build/preflight 0 | Both recent comparisons and the complete earlier chain qualify. Active descriptor is `378b60b0…13bf`. |
| Authentic old SQLite hash at the Test DLL logical path | Native 1; `Final input identity differs.` | The wrong path/hash pair cannot use a historical alias. No active pointer. |

Aliases are keyed by normalized logical path and expected SHA256: eleven identities cover nine paths, including two Test compilation epochs. Each older validator resolves its own context first. A missing or wrong identity falls back to strict current-file capture. The three older source-hash calls now pass their authenticated PDB checksum; checksum equality remains unchanged.

The canonical port preserves all 22 inherited validation/hash method bodies except the repository identifier, plus six fixed baseline aliases and two exact assignment recipes. It authenticates 195 original/snapshot pairs, native source, command logs and genuine retirement metadata before requiring reconstructed transition/outcome equality. The nullable class authenticates 409 pairs and recomputes its accepted comparison. No denominator, target or broad qualification is reset.

## Tool and service failure handling

The original causal rejection and failed integration attempt remain immutable. The initial preparation draft also retains the incorrect canonical source path identified before execution; its exact correction was reviewed without dispatch. The first native integration failure consumes artifact repair 1 of 3. The earlier nullable acquisition/qualification repair count remains 2 of 3 in its separate scope.

The retry keeps the same native assertions and bounds. Separate preparation/controller files and a separate R1 source review preserve earlier receipts. Commands have 120-second deadlines within a 900-second parent; logs and inputs are capped at 16 MiB. Terminal preservation and retirement each have 30-second bounds. Cancellation, forced termination, unobserved exits, source/input changes or cleanup failures reject qualification.

The successful retry completes in 11.030626 seconds with no cleanup, storage, cancellation or forced-termination errors. Five sessions are settled; 61 private files are retained with inactive source/project/runtime names and the original parent is absent.

## Concurrency and load considerations

Root owns shared source, registers, controllers, SDK/runtime and index. A single worker leased only the new canonical class; its lease ended before compilation. All SDK and verifier commands ran serially in an owned 0700 parent.

The exact admission graph has 3,310 input/snapshot pairs. Each conservative capture store is 462,375,265 bytes, below 640 MiB; allocated owned storage is 1,658,189,254 bytes, below 2 GiB. Actual owned storage is 1,053,210,372 bytes. Accepted and rejected native captures contain 3,092/835 inputs and 458,274,497/125,399,356 bytes. Before/after maps match. Root source/index and 336 ordinary binaries remain unchanged during execution.

These measurements qualify only this control packet. The earlier 18,310,224-byte historical final-capture margin is provisional; fresh complete producer and assessment graphs require independent measurement before dispatch.

## Security and data exposure

Inputs come from authentic prior compilation, native test and producer receipts. No fabricated database records or negative payloads are introduced. The negative control uses an authentic existing SHA256 in the wrong logical context. Pinned receipts, local alias recipes, normalized paths, file/link/size bounds and terminal rehashes preserve integrity.

No network service, cloud resource, credential, package publication, Git commit, push or tag is part of this checkpoint. Retention uses private owned directories and preserves the empty original index. Historical artifacts remain ignored local evidence.

## Verification commands and results

The reviewed packets retain exact argv and environment; abbreviated commands below describe their purpose:

- `dotnet --version` → SDK 10.0.401.
- Private framework-only restore and Release build with `-warnaserror`, CI properties and disabled build servers → exit 0, zero warnings/errors.
- Verifier `preflight` → complete replay exit 0 and current descriptor pointer.
- Verifier `recent-context-rejection` → authentic wrong-context rejection exit 1.
- Python controller syntax, `bash -n eng/verify-final.sh` and `git diff --check` → pass.

| Receipt | Immutable SHA256 |
| :--- | :--- |
| Causal baseline `baseline.obln8tf9/result.json` | `ba71c9dc36adc35e28f8a1475a8b066d7b9f72fa5ba71203f327401ca3109987` |
| Failed attempt `continuity.8fodt14w/result.json` | `73e928d7d6cc5670f6831127f15a6304ad929bbe34b9c9da4792b28d0aa4596f` |
| Successful retry `continuity.jjq1ow_3/result.json` | `2ab137e60aedc6d52cd2b8dc8fa752421346f9a6d824d5406220b7f9806b8a51` |
| Accepted outcome | `95c8b0acba5c2bb27dd21fb63488a6ed06c288add6e0855b7be090c70869e847` |
| Accepted before/after maps | `5e1ea859a36eab489dc21f07bc0422fd4bfb01c19d9aa49b372c47cbf15e35f2` |
| Rejected before/after maps | `f2d98f52066c8b29435e0b042a2a91779e4858fe60c8a4f0a7ccc39a192b4166` |
| Successful retirement | `f63f0f63f296276e6afbf321d5bf5752e6678d4233f73e328af154627e998c9f` |

Receipts are under `artifacts/recent-final-binding/`; source, packet and observed Oracle reviews identify their separate scopes. Integrated review is recorded separately after the documentation checkpoint.

## Cache and consumer integration

Eight checked consumer files were copied from the authentic generated `run.nvlwAtiy` producer. Seven complete graphs change only 18 toolkit content hashes; six archive pins change, with external nodes and edges preserved. Cache-good `cache-good.50zvjg43` has observed Oracle CLEAR: native SDK/restore/build/cache exits 0, zero warnings/errors, 4,600 logical / 4,126 physical capture files and 550,734,136 bytes. The retained diagnostic seed remains 58 commands, 68 executions, 62 passes and six intended failures; optimized-default qualification remains false.

Four separate reuse controls have observed Oracle CLEAR: complete preflight accepts current descriptor 378b; authentic wrong-context and two genuine failed-cache inputs reject. Their eight SDK commands settle, all inputs/source/index/336 binaries remain unchanged and all owned parents are absent. These outcomes do not qualify fresh final classification.

## Fresh producer chain and assessment failure

`fresh-final.k2nm113b` / `run.33Yxvef9` passes all nine native stages, including strict build with zero warnings/errors. Actual strict results are 435 definitions / 110 passes / 325 explicit SQL Server skips / 247 translations. Coverage is 2,968/8,359 lines and 1,602/5,336 branches, with 97 sources and 16,736 leaves. Its callback fails before assessment on the authentic independent-consumer preparation status `prepared`. Original result `7b1db138b1cf09aaf04229eefe80cfcad4fe78c640b78a92d7dfb36d61a3e539` remains FAILED; no assessment exit or outcome is retroactively added to it.

R1 recovery preparation `assessment-recovery.wiljdom9` fails before any SDK dispatch, packet or snapshot publication. Admission incorrectly recurses every platform's native output directory. R2 aligns that selection with the C# helper's immediate native output files plus explicit managed/Linux ARM64 assets; independent consumer recursion and entire retained-chain storage accounting stay intact. The original R1 failure and five-file retirement remain protected.

R2 `assessment-recovery.8xvjjknw` seals 5,585 origin/snapshot pairs and protects 13,780 files in ten exact root sets. Admission selects 5,528 logical / 5,208 physical files, 660,148,737 bytes, and 320 permitted cache duplicates worth 79,942,581 bytes. Only the two exact previously approved 18,762,916-byte archives exceed the unchanged 16 MiB input bound. Allocation is 2,208,386,010 inner / 3,632,861,904 outer bytes, below unchanged 3/4 GiB bounds. Source, concrete packet and observed review remain separate.

Native SDK identity exits 0 and assessment exits 1, but assessment rejects `evidence-integrity` with `InvalidDataException: Native consumer session identity differs.` The original witness line begins ` 574814  574814  574814`; parsed PID/PGID/SID all equal the recorded PID. `PackageIndependentEvidence.cs:536` anchors directly to digits and rejects authentic leading `ps` padding. No guard edit or weakened identity check follows. There is no fresh fact classification or final-readiness outcome.

The packet completes in 11.866106 seconds, with terminal preservation 2.826672 seconds and retirement 1.254956 seconds. All seals/snapshots, protected exact file sets, source/index and 336 ordinary binaries verify; both sessions settle, no forced termination/cancellation/storage/cleanup error occurs, and five private files are retained inactive with the parent absent. Observed Oracle CLEAR covers failure classification and preservation, not assessment qualification.

| R2 receipt | Immutable SHA256 |
| :--- | :--- |
| Result | `45d9eae496f0026cdaaed3a7797767a5418fe0b4d26dc260f67a938a11b0d051` |
| Native evidence-integrity rejection | `4a6e41f298e4259d838a3853d9d7355d38dbb9d73d464eb85a88b3c2e1faad12` |
| Parent retirement | `bdd8b444679cb485480c50465d8b260b98f17d3732321c9a6b571cc5d9797e9c` |

The PDB correction used repair 1, preparation-status correction used repair 2, and native-selection correction used repair 3. The integration budget is exhausted at 3/3. [Blocked work](../BLOCKED.md) records V1aj.4 and dependent V1ai.4 closure; no fourth recovery, source repair or recompile is authorized. Earlier nullable and optimized budgets retain their own historical counts.

## Scope and follow-ups

Fact registration is prepared for 34 rows and 95 distinct methods, preserving the prior 33 raw rows/claims and 87 methods. Actual scoped qualification counts are not claimed until fresh classification executes. Consumer lock/feed copies and current diagnostic cache compatibility are verified in scope. Nine fresh producer stages pass; their final assessment fails evidence integrity, so fresh classification and complete final binding remain unqualified.

Coverage targets and branch arms remain unverified and unwaived. Full readiness, M0, roadmap completion and unanswered owner/data/public/provider/platform/workload decisions remain open. The three existing documentation hash values are repinned in a separate reviewed metadata checkpoint, not native-reverified. Native evidence retains draft 0.109 and its exact profile/source snapshot; later metadata review does not prove current-checkout readiness.
