---
title: Current command compilation continuity hardness report
version: 0.1
last_updated: 2026-10-04
status: Scoped closure; integrated Oracle CLEAR
owner: IQToolkit project owner
tags: [modernization, verification, atlas]
---

# Hardness Report: current command compilation continuity

The final engineering helper now provides `command-continuity-control` for the authenticated V1z→V1aa command-initialization transition. It recomputes the executed qualification, compares the receipt exactly and verifies all 587 recorded input identities and their retained snapshots. Its success publishes no active descriptor or final-success pointer.

The [V1ac contract](modernization-implementation-plan.md#phase-v1ac-durable-current-command-compilation-qualification) follows the owner-selected .NET standard 1.1.0-draft.1 and development-plan guide 2.7. Qualification is scoped to Linux ARM64, SDK10.0.401/runtime10.0.12 and the existing authentic SQLite3.53.3/Microsoft.Data.Sqlite10.0.12 evidence. The [durable class](../eng/FinalVerification/CommandInitializationContinuity.cs) ports the previously executed private algorithm; all 25 validation methods remain byte-identical. Shared capture and terminal preservation belong to the existing final coordinator.

## Edge cases tested

| Native packet | Actual exits | Observed result |
| :--- | :--- | :--- |
| red.6uyn83x2 | Old helper1; assertion1 | Exact `evidence-integrity` rejection: `Final input identity differs.`; named causal RED retained |
| green.1zz2jyit | Restore0; build0; control0; assertion0 | Exact executed receipt and input/snapshot bindings verified; zero build warnings/errors |

The RED assertion was `Current command-initialization continuity must bind without changing permanent anchors.` The retained old compiled helper ran its genuine preflight against current artifacts. The same durable [outcome assertion](../eng/verify-command-continuity-outcome.sh) qualifies the new control's precise scope. Both controls require absence of active-descriptor publication.

The port preserves the byte-exact GetCommand source transformation, five local baseline aliases, changed Data/Test DLL/PDB identities, inherited PDB documents, exact five compiled native guard methods and native GREEN TRX. Strict definitions/outcomes grow419→424; all97 measured source identities are accounted for, all16716 inherited leaves preserve structure/coverage and exactly eight covered leaves are added. Existing coverage remains2738/8353 lines and1460/5320 branches. The original/V1y/V1z nine pinned chain inputs and386 historical physical snapshots remain authenticated.

Current reads remain current; historical aliases are local to the new class's baseline context. `CompilationContinuity.cs`, `CacheSeedEvidence.cs`, classifiers and all32 fact rows are unchanged. Every previous profile field remains byte-equivalent as a parsed value; five separately pinned command fields were added. Existing preflight/cache/assessment branches remain unchanged except indentation. The shell wrapper copies the new class into its owned helper build.

## Tool and service failure handling

Prospective source/controller reviews and observed RED review were CLEAR before implementation; prospective GREEN review was CLEAR before SDK dispatch. No unexpected native failure or repair retry occurred. Two SDK packets used the declared three-packet cap. No library build, test/coverage recollection or cache seed rerun was performed.

The expected complete shared closure was measured before GREEN:1178 paths and162345693 bytes. Native retention matched those values exactly, below the unchanged2048-path/192MiB/per-file16MiB helper limits. The control's inputs-before/after maps match. Parent limits were900seconds,120seconds per command,16MiB per log and2GiB complete storage scans.

RED completed in0.460seconds with18429135 terminal owned bytes; GREEN in4.004seconds with164955669 terminal owned bytes. These are observed supervisor totals, not instantaneous peak-memory guarantees. Both owned sessions terminated without forced termination, live members, cancellation, cleanup/storage errors or scan retries. Root retained private files only after inactivity, verified every copied hash, preserved the GREEN project as a snapshot and removed only authenticated task-owned temporary parents. GREEN retained29 private files.

The first read-only protecting inspection followed an already-inactive private original path and raised FileNotFoundError. Its failed source and receipt remain retained. The corrected inspection resolves only those disposed private paths through their independently verified retention map; live current inputs remain direct. No native producer was rerun or rewritten.

## Concurrency and load considerations

The source-only worker owned the new class; root owned integration, SDK execution and retention. Builds were serialized. Root checkout/index and all336 ordinary binary identities remained unchanged within both SDK packets;24 inputs were sealed for RED and1206 for GREEN. This does not establish concurrent final-helper execution, provider load or other OS/architecture qualification.

## Security and data exposure

Inputs were authentic local source, PDB, DLL, TRX and coverage artifacts. Framework-only compilation used the existing template and empty-feed configuration, disabled parent imports/CPM and shared compilation, and enabled CI/warnings-as-errors. No new dependency, generated provider data, credentials, external database, paid service or remote publication was introduced.

## Verification commands and results

Concrete unique paths, source/controller hashes, complete argv, actual exits, logs, preservation maps and inactive dispositions are retained under `artifacts/command-final-binding/{red.6uyn83x2,green.1zz2jyit}`. The GREEN operation was:

```text
/home/firestrand/.dotnet/dotnet <owned-built-Verifier.dll> command-continuity-control /home/firestrand/Projects/IQToolkit /home/firestrand/Projects/IQToolkit/eng/FinalVerification/profile.json <owned-empty-retention>
/usr/bin/bash /home/firestrand/Projects/IQToolkit/eng/verify-command-continuity-outcome.sh green <owned-outcome.json> <recorded-native-exit.txt>
```

The native control and assertion exited0. The result explicitly reports `FinalReadinessVerified=false`, `DurableFinalProfileAccepted=false` and `FullGoalComplete=false`. The original old-helper rejection remains retained.

At the historical V1ac.1–V1ac.2 scoped-control checkpoint, ordinary historical-anchor resolution, active-descriptor selection, current diagnostic-cache compatibility, eight production-source fact dependency hashes and fresh final orchestration remained separately expanded integration work. Their later scoped qualification is recorded in the [ordinary current final verification report](modernization-final-current-binding-hardness.md). Unwaived coverage targets, optimized-cache qualification and owner/provider/platform gates remain open. Protecting checks pass and independent integrated Oracle review is CLEAR for this scoped transition control.
