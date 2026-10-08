---
title: CI consumer interruption hardness report
version: 0.1
date: 2026-10-04
status: Scoped closure; integrated Oracle CLEAR
owner: IQToolkit project owner
tags: [modernization, reliability, atlas]
---

# Hardness Report: CI consumer interruption publication

The CI consumer runner now records failure when its reviewed foreground pipeline is interrupted by group SIGTERM. Success requires completion of every existing stage and final archive comparison. The durable [outcome assertion](../eng/verify-ci-consumer-outcome.sh) checks actual failed/nonzero/pointer-preserving interruption receipts and complete six-profile/seven-review/SQLite receipts; the owning supervisor separately authenticates eligibility and process disposition.

Scope is GNU Bash 5.2.21 on Linux ARM64, SDK 10.0.401/runtime 10.0.12, current six 1.1.2-preview.1 library archives and installed SQLite 3.53.3/Microsoft.Data.Sqlite 10.0.12. The selected owner-supplied .NET draft 1.1.0 and development-plan guide 2.7 govern the [V1ab packet](modernization-implementation-plan.md#phase-v1ab-truthful-ci-consumer-interruption-outcomes). This is an engineering reliability fix; no library API, dependency, reviewed lock, provider data or fact semantics changed.

## Edge cases tested

The original feed.3su6_6a8 interrupted run remains failed at its supervising layer while its authentic CIrun.1ZeLSQug status remains misleadingly passed. Its actual wrapper exit was not recorded. A new Test used real native compilation and kernel process identities, avoiding any inference from that missing exit.

Before each deliberate SIGTERM, the supervisor observed completed native preparation, all seven graph reviews, exactly one real IQToolkit public compilation JSON/confirmation, five live owned session members including an actual dotnet process, incomplete remaining profiles and no SQLite verification. It rechecked the leader start identity, session/group and incomplete stage before sending one group SIGTERM. Missing eligibility would invalidate the control.

| Packet / native CI run | Actual exits: SDK / wrapper / assertion | Producer status | Success pointer | Scope result |
| :--- | :--- | :--- | :--- | :--- |
| red.euayd1zn / run.MfnTfolJ | 0 / -15 / 1 | passed, misleading | Advanced prematurely | Named causal RED |
| green.jr5nr_c9 / run.Xq5o292y | 0 / 143 / 0 | failed | Byte-identical | Reviewed interruption GREEN |
| complete.o9lv9k7n / run.GCavXro9 | 0 / unrecorded / assertion not run | passed | Advanced after native components | Supervising storage failure; unqualified complete control |
| supplemental-complete.kdeffuz7 / run.opJ5xgsP | 0 / 0 / 0 | passed | Exact new native run | Complete success control |

The RED assertion was exactly `Interrupted CI consumer verification must record failed status.` The defective pointer's before/after bytes and misleading native status remain retained. After observed Oracle CLEAR, root restored only that task-owned pointer to its authenticated preceding complete run. The minimal producer repair adds incomplete/completed state, TERM state/exit 143 and an EXIT guard preserving prior nonzero exits. Completion is set only after the final existing archive comparisons.

The complete success assertion requires exactly one actual compilation JSON for each of the six named profiles, all six provenance confirmations and public-symbol confirmations, seven exact lock-delta confirmations, native SQLite success, wrapper 0/passed and its own exact success pointer. Native package inputs and current archive payloads remain authenticated. Direct child-runner interruption status is not certified by this outer-runner repair.

## Tool and service failure handling

Prospective Oracle review corrected controller failures before dispatch: protected prerequisite capture, independent secondary log/publication observation and post-settlement time/log limits. Those original controller sources and manifests remain retained.

The original three-invocation budget was exhausted by RED, GREEN and the failed complete supervisor. That third run genuinely completed six profile compilations/seven reviews/SQLite, but supervision rejected an evidence bound and failed to record the wrapper exit. Terminal bytes were 900982770 and the log 65269 bytes; the actual rejecting scan value is unavailable. Scanning a temporary parent before its tree relocates into the separately scanned destination can count the moving tree twice. This is a plausible source-based explanation, not proof of that original threshold. The original failure was neither rewritten nor accepted.

A separate V1ab.3 Verify packet authorized one supplemental complete-only invocation/no retry after independent review. The private supervisor now retains exact rejection values, maximum and terminal completed-scan bytes, and actual native exit during settlement even after another failure. It scans every owned tree with no exclusions; its reviewed 2 GiB limit accommodates the complete retained producer and temporary/destination relocation views. This resource allocation changes no outcome/provenance/coverage requirement.

| Packet | Seconds | Terminal owned bytes | Inactive private files retained |
| :--- | ---: | ---: | ---: |
| RED | 20.735 | 445811465 | 420 |
| GREEN | 20.952 | 444972931 | 420 |
| Original complete failure | 39.252 | 900982770 | 562 |
| Supplemental complete | 40.475 | 901191349 | 562 |

Supplemental maximum completed scan was 901053168 bytes. Configured limits were 240 seconds per owning packet, 120 seconds per command and 16 MiB per log; original packets used 1 GiB complete storage scans, supplemental used 2 GiB. These are observed scan/runtime bounds, not hard peak-memory or instantaneous disk measurements. Three bounded scan retries apply only to genuine scan races. All four native sessions ended with no surviving owned non-zombie members, no cleanup/storage exceptions or cancellation; original storage-bound rejection remains an operation failure.

Original and supplemental private parents were retained only after owned sessions became inactive. Any remaining discoverable csproj filenames were preserved under snapshot names with explicit mappings, all copied bytes were hash-verified, and only authenticated task-owned temporary parents were removed. No unrelated language server was terminated.

## Concurrency and load considerations

This qualifies one sequential Linux/GNU owned-group SIGTERM point during actual verification. It does not establish arbitrary interruption stages, direct-PID cancellation, escaped/background children, SIGINT/HUP/KILL recovery, standalone child-runner cancellation semantics, crash scavenging, other OS families, provider async cancellation or load behavior.

Foreground pipelines remain synchronous. GNU Bash defers a trapped signal while waiting for foreground commands; this supports child completion before outer retention, while actual owned-member settlement remains independently checked. [GNU Bash Signals manual](https://www.gnu.org/software/bash/manual/html_node/Signals.html).

## Security and data exposure

Only genuine existing package archives, restored dependency graphs, public symbol metadata and unchanged native SQLite engine metadata were used. Signals and stage selection are operational controls. No fixtures, representative records, generated provider responses, credentials, public services, new dependencies or remote publication were introduced. Private evidence stayed in owned temporary/retained namespaces.

## Verification commands and results

Literal producer command for each reviewed packet:

```bash
/usr/bin/bash /home/firestrand/Projects/IQToolkit/eng/verify-ci-consumers.sh /home/firestrand/Projects/IQToolkit/artifacts/api-compatibility/runs/run.UGPPa1Zm/packages
```

The durable assertion uses the mode, sole authenticated actual evidence directory, recorded before/after pointer files and native exit file:

```text
/usr/bin/bash /home/firestrand/Projects/IQToolkit/eng/verify-ci-consumer-outcome.sh interrupted|complete <actual-owned-evidence> <pointer-before> <pointer-after> <native-exit-file>
```

Sealed concrete argv, source/controller hashes, actual dispatch resolutions, native logs, process identities, snapshots and retention inventories are under artifacts/consumer-interruption. The assertion/controller bytes remain identical across RED/GREEN; only producer completion/TERM handling changes. Supplemental private-controller measurement changes preserve those original sources and all failed receipts. All 334 sealed inputs, Git index and 336 ordinary binary identities remained stable inside each native packet.

No untouched library SDK test/coverage collection was rerun for this Bash-only fix. Integration authenticates the existing current 99 passes/325 explicit SQL skips/247 translation checks, twelve archive payloads, seven retained compilation pairs, 97 measured sources/16724 coverage leaves and original historical chain. Coverage remains 2738/8353 lines and 1460/5320 branches; targets and durable current-final binding remain open.

Native controls and integrated protecting checks pass; independent integrated Oracle review is CLEAR for this scoped closure. Full modernization, remaining obligations in the 32-row fact ledger, unwaived coverage targets, optimized-cache sensitivity, platform/backend evidence and pending owner decisions remain required.
