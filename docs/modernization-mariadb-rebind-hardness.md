---
title: MariaDB same-attempt packet rebind hardness report
version: 0.9
last_updated: 2026-10-06
status: Isolated control repair verified; live acquisition remains gated
owner: IQToolkit project owner
tags: [modernization, mariadb, recovery, verification]
---

# MariaDB same-attempt packet rebind hardness report

This report covers deterministic refusal ordering in the recovery-only `rebind-prepared` command in [the package acquisition GOTCHA](modernization-mariadb-package-probe.md). It does not authorize changing the real prepared attempt, network access, archive acquisition, SDK use, extraction or database operations.

## Edge cases tested

The production `rebind_prepared()` function is called using an isolated private temporary attempt. The test injects an oversized serialized replacement packet and a manifest identity change at the final pre-write check. Both cases return their expected controlled refusal. The test snapshots the attempt's entries, bytes and permission modes before the call, and confirms exact equality afterward. It also instruments the first recovery-copy operation and confirms neither refusal reaches it.

Three fresh temporary attempts exercise production `resume_rebind_install()` with injected atomic-install failures before the first rename, after the source rename but before the packet rename, and after both subject renames but before journal completion. In every case production `accepted()` rejects the incomplete attempt. A second call to production `resume_rebind_install()` validates the journal and retained evidence, rolls forward, and reaches the exact new source/packet pair and complete journal. A separate injected directory-fsync failure after the completion-journal rename leaves a visible complete marker; production `accepted()` refuses while the injected sync failure persists, then completed-journal re-entry syncs successfully before returning. The independent positive accepted-packet fixture verifies that a complete reconciled journal can pass the production acceptance boundary.

The integrated verifier checks current-catalog additions, missing manifest rows, every missing path in the exact legacy exclusion set, substituted and extra exclusions, stale report bindings, and accepted/rejected authorization-boundary behavior. Its production `accepted()` fixture now models all twelve files required by a completed attempt, including the three acceptance receipts, and reaches later acceptance validation with that directory listing. Injecting an unexpected sidecar then returns the specific fail-closed `rebind-attempt-state` refusal. The fixture snapshots each attempt entry's bytes/mode before and after the refusal. Private-attempt cases also interrupt recovery after each pre-journal artifact write, with a torn journal, with partial source/packet install temporaries, and with a partial completion-journal temporary, and after the completion-journal rename with directory `fsync()` failure. Production `accepted()` and re-entry refuse those states, and exact original packet/source bytes plus partial evidence remain unchanged; no automatic cleanup or retry is claimed.

Round27 source/packet review found two independent blockers in `accepted()`: worker startup re-entered the completed twelve-file equality check after the supervisor had written `started.json`, and post-rebind acceptance required every fresh review to repeat the older manifest that authorized the rebind. The first prevented worker startup; the second prevented a fresh current-epoch approval set. GOTCHA1.11 and plan0.146 now define three call stages (`ready`, `worker-start`, `preserve`) and separate the reconciliation manifest from the current review manifest. Production acceptance permits only stage-specific files. Start receipts bind the current packet, positive reservation and process/session identity; preservation entries are no-follow regular files with exact modes and bounded sizes, and event ordinals are contiguous. All three current approvals must agree on the fresh manifest while retaining the reconciliation digest.

The authorized deterministic fixture adds an authentic-shaped `started.json`, bounded partial archive/event/receipt files, and a current review manifest that intentionally differs from the reconciliation manifest. Production `accepted()` accepts valid worker-start and preservation sets, including an early startup failure without `worker-receipt.json`; it refuses stale packet/process bindings, wrong start/event/slot modes, oversized event/slot bytes, a gapped event sequence and an unknown sidecar. Production `telemetry()` accepts a valid fixed `worker-finished` event, rejects malformed event fields, and refuses a duplicate `Seq` value even when event filenames are contiguous. Each negative runtime case snapshots the private file names, bytes and modes before and after the call. Fixture metadata is handwritten and contains no package, HTTP, application or domain data. Tests do not touch the real attempt.

## Tool and service failure handling

All deterministic checks and complete packet/reconciliation/journal serialization and size validation precede the first attempt write. The source, profile, every current input, and review manifest are rechecked at that boundary. Recovery copies, staged files and reconciliation are written before the journal; interruption in this window is intentionally fail-closed and non-resumable. Production acceptance rejects every known residue, re-entry refuses without mutation, and the packet/source remain unchanged for manual evidence-preserving investigation. Before either subject rename, the durable journal records exact old/new source and packet identities, retained stage identities, source/profile/manifest bindings, preflight approvals and reconciliation identity. An incomplete journal blocks acceptance. Re-entry resumes only recognized old/new states using retained stage copies, then atomically records completion. Failures at the tested rename boundaries are resumable when their install temporaries are complete and verify against the retained stages; production acceptance rejects the interrupted state and production re-entry rolls forward without rollback. Pre-journal and partial-temporary failures preserve the old subjects and partial artifacts but require manual evidence-preserving investigation. The real attempt has not been rebound, so filesystem failure behavior during a real operation is unobserved.

## Concurrency and load

The protocol remains a single-root-writer recovery action for one named attempt. No concurrent mutation or load claim is made. The isolated verifier uses a temporary directory and does not share mutable state with the real attempt.

## Security and data exposure

The test does not use package bytes, credentials, external services or database contents. It does not perform network, SDK, engine, provider, archive or database operations. The actual original packet, prepare receipt and live attempt remain untouched. This single-host local test does not establish crash, power-loss, cross-platform filesystem or concurrent-writer guarantees.

## Verification commands and results

| Executed check | Observed result |
| :--- | :--- |
| `/usr/bin/python3.12 -I -B artifacts/mariadb-package-discovery/verify_preparation.py` | Historical pass before the real same-attempt rebind; its old-packet delta check is intentionally not reusable against the later reconciled source/document epoch. |
| `/usr/bin/python3.12 -I -B -O artifacts/mariadb-package-discovery/verify_preparation.py` | Historical pass before the real same-attempt rebind; its old-packet delta check is intentionally not reusable against the later reconciled source/document epoch. |
| `/usr/bin/python3.12 -I -B artifacts/mariadb-package-discovery/verify_preparation.py --acceptance-fixtures-only` | Passed; production ready/start/preservation lifecycle and current-review-manifest fixtures pass; no live mutation or dispatch. |
| `/usr/bin/python3.12 -I -B -O artifacts/mariadb-package-discovery/verify_preparation.py --acceptance-fixtures-only` | Passed; same lifecycle and epoch-binding fixtures pass with assertions disabled; no live mutation or dispatch. |
| Current event telemetry controls within both fixture-only commands | Passed; valid event accepted, malformed/duplicate sequence refused, event mode/size rejected, and snapshots prove the fixtures remain unchanged. |
| Isolated production rebind oversized-packet refusal | Passed; no mutation calls and exact attempt snapshot preserved. |
| Isolated production rebind late-manifest-change refusal | Passed; no mutation calls and exact attempt snapshot preserved. |
| Isolated journal failure before first subject rename | Passed; production `accepted()` rejected it, `resume_rebind_install()` reached the exact new pair. |
| Isolated journal failure between subject renames | Passed; production `accepted()` rejected it, `resume_rebind_install()` reached the exact new pair. |
| Isolated journal failure after both subject renames, before completion marker | Passed; production `accepted()` rejected it, `resume_rebind_install()` verified pair and recorded completion. |
| Isolated pre-journal interruption/torn journal cases | Passed in normal and optimized Python; nine private-attempt cases prove production acceptance/re-entry refusal and exact preservation of original packet/source and partial evidence, as recorded in `root-local-checks.json`. |
| Partial source/packet install and completion-journal temporaries | Passed in normal and optimized Python; four private-attempt cases prove production acceptance/re-entry refusal and exact preservation of original subjects and partial temporary evidence. |
| Completion-journal directory-sync failure | Passed in normal and optimized Python; `accepted()` refused after the completion rename while directory `fsync()` failed; completed-journal resume synced successfully before returning. |
| Unexpected file in otherwise complete attempt | Passed in normal and optimized Python; production `accepted()` accepts all twelve required files, including approvals, and refuses an added sidecar with `rebind-attempt-state` while preserving every entry's bytes/mode. |

## Summary

- **Pass:** Covered deterministic packet-size and final manifest refusal paths, production acceptance/resume at all three subject/journal interruption boundaries, exact twelve-file membership with approvals, three runtime acceptance stages, and normal/optimized checks, including completion-marker directory-sync failure. Current reviews bind a fresh manifest while reconciliation retains its original authorization epoch; mixed approvals still fail. Early worker startup failure can preserve evidence without a receipt that was never written. Telemetry rejects malformed and duplicate event sequences; all tested runtime refusal paths preserve fixture names, bytes and modes. Pre-journal artifact interruption and torn-journal cases refuse in production acceptance/re-entry while preserving original packet/source and partial evidence.
- **Fail:** No failure observed in the scoped checks.
- **Follow-ups:** These source/document edits supersede Round27 and require fresh independent planning/source review. The completed attempt is bound to earlier source/document identities and cannot be dispatched or rebound; prepare a new packet only after source/planning acceptance, then obtain distinct exact-packet CLEAR before run. Archive operations remain gated. Power-loss ordering, failures outside the injected pre-journal/journal-update/install-temporary, atomic-rename, and completion-directory-sync boundaries, and cross-platform semantics remain unverified.
