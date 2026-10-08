---
title: Trimming and NativeAOT feasibility hardness
version: 0.1
last_updated: 2026-10-04
status: Scoped verification; observed Oracle CLEAR
owner: IQToolkit project owner
tags: [modernization, discovery, trimming, nativeaot, verification]
---

# ATLAS hardness report: trimming and NativeAOT feasibility

AP0 resolves the core analyzer portion of the required `LOCAL-RM-DISCOVERY` item. An untouched ordinary core rebuild passes with zero warnings/errors; the same core with strict trimming/AOT analysis rejects 45 source-bound diagnostics. Current code needs further compatibility work before any supported trimming or NativeAOT profile can be declared. This is an analyzer feasibility result, not a trimmed publication or native application test.

## Edge cases and measured findings

The profile is SDK 10.0.401, runtime/reference pack 10.0.12, `net10.0`, Release, CI, Linux ARM64. Exactly 28 existing core C# files / 298,513 bytes are copied without changes. `NOREFEMIT` remains undefined; support/publish properties and production build configuration are untouched. The [probe contract](modernization-aot-feasibility-probe.md) records the source-based hypothesis and bounds.

Both new profile graphs were deliberately prepared and independently reviewed before cold locked restores. Ordinary core has no external package dependency; the analysis graph contains only SDK-selected Microsoft.NET.ILLink.Tasks 10.0.12. Its signed archive SHA512 matches the official NuGet catalog and `.nupkg.sha512`; its separate content hash agrees across lock/assets/`.nupkg.metadata`. Both exact analyzer binaries match the authenticated archive payloads.

The ordinary rebuild exits0 with zero warnings/errors and fresh DLL/PDB outputs. Strict analyzed rebuild exits1 with these 45 unique errors, corroborated by native compiler SARIF 1.0.0 and exact file/region/message bindings:

| Diagnostic | Unique errors | Mechanism reported |
| :--- | ---: | :--- |
| IL2026 | 13 | Members declare trimming requirements, including GetCurrentMethod and a metadata-dependent expression constructor. |
| IL2060 | 8 | Reflected generic method construction cannot be statically analyzed. |
| IL2067 | 1 | Constructor metadata requirements do not flow through a Type parameter. |
| IL2070 | 2 | Interface/method metadata requirements do not flow through Type parameters. |
| IL2075 | 1 | Reflected method lookup lacks metadata guarantees from Object.GetType. |
| IL3050 | 20 | Dynamic-code requirements for generic instantiation and expression/delegate/array construction. |

Affected files are Updatable (24), TypeHelper (6), QueryCompiler (3), StrongDelegate (3), ExpressionVisitor (3), QueryCache (2), PartialEvaluator (2), QueryProvider (1) and ExpressionWriter (1). The source-bound report retains all exact coordinates and messages; counts do not imply an equal number of independent product defects. Build output repeats each diagnostic in its summary: 90 occurrences map to the same 45 SARIF records. Both profiles contain the same 29 informational analyzer notes; neither profile emits a warning, and the analyzed errors are exclusively IL2xxx/IL3xxx.

Acquired matching runtime source confirms LambdaExpression.Compile can select interpretation when dynamic code is unavailable. That does not establish compatibility of constructing the expression itself: the actual IL3050 findings include Expression.Lambda and runtime array construction. Metadata flow, generic instantiation and query-plan construction need separate designs and tests.

## Tool and service failures

Two official-source acquisitions failed on a guessed runtime filename. Both attempts and the incorrect initial failure attribution remain preserved with an explicit correction; directory discovery resolved the correct filename. Original compressed registration and identity-encoded catalog responses are distinguished.

Graph packet `graphs.srlr9u7x` ran all three SDK commands successfully, then its validator incorrectly compared the signed archive digest with signature-excluded contentHash. Its original FAILED result is immutable. Corrected offline inspection separately authenticated both identities, all 39 extracted payload comparisons, 40 catalog entries and the actual 43-entry archive including three container metadata entries. Observed Oracle graph review permits those authentic graphs as inputs to a new locked profile; the failed runner is not relabeled successful.

First compiler packet `compile.xz968pe7` ran SDK identity, normal locked restore and ordinary rebuild successfully, then rejected the SDK's actual `csc` launcher because it expected `csc.dll`. Its empty-SARIF assumption also needed to distinguish informational notes. Analysis did not run. Original FAILED receipts and all outputs remain retained. Distinct corrected sources and packet were reviewed before final attempt3of3; warning/error policy, source and expected diagnostic families were preserved.

Final `compile-r1.dky73c5h` captures both real rebuilds successfully. Subsequent filesystem-only classification verifies the authentic SARIF schema and tool, exact unchanged source regions, identical notes and native/SARIF error correspondence. No extra SDK rerun or suppression is used. Both failed packets and their independent reviews remain part of the evidence.

## Concurrency, load and security

Root serializes SDK ownership; workers and Oracle are read-only. Ordinary and analysis projects, package caches, HTTP caches, locks and outputs are separate. The probe uses existing source and official NuGet metadata rather than generated provider data, databases, credentials or application records. A minimal process environment is used; no production dependency or public API is changed.

All three packets settle their sessions and retire only their owned private parents, retaining byte-identical inactive source/project/runtime files. The final packet preserves 467 reviewed input/snapshot pairs; checkout sources, index and all 336 ordinary DLL/PDB identities remain unchanged. Complete AP0-owned evidence/cache/scratch is 45,192,699 bytes at final retirement, below 2 GiB. Aggregate native packet elapsed time is 11.837441 seconds, below 900 seconds; per-command, 16 MiB artifact, 30-second preservation and 30-second retirement bounds hold. Three native attempts are used; no fourth SDK attempt is allowed by this packet.

## Verification and retained identities

`dotnet restore ... --locked-mode` succeeds for both reviewed profiles with zero warnings/errors and unchanged locks. `dotnet build ... --no-restore -t:Rebuild ... -warnaserror` passes normally and rejects exactly the declared IL2xxx/IL3xxx families with both analysis switches enabled. Actual Csc invocation, `/warnaserror+`, delivered analyzer configuration, `/reportanalyzer`, exact ILLink `/analyzer:` paths and native diagnostics establish analyzer execution; package presence alone is insufficient. Native SARIF, binlogs, compiler logs and generated configuration are retained.

`python3 artifacts/aot-feasibility/classify-diagnostics.py` passes offline with 45 unique source-bound errors, 25 trimming / 20 AOT errors, 90 native occurrences and 29 identical informational notes in each profile. The final result remains a capture receipt; the separate report establishes source-bound diagnostic classification.

| Evidence under `artifacts/aot-feasibility/compile-r1.dky73c5h/` | SHA256 |
| :--- | :--- |
| `result.json` | `f9b86feb83ca272f2afc687e7f04ead1ebb6abe8305027ae0bec6bffad105c66` |
| `parent-retirement.json` | `e461bad7d9dbbc998672b93ba7dc598dcc973a2eacb61c536645a2ed05eccc29` |
| `source-bound-diagnostics.json` | `fb75454ce58892488c896518d89e844de8faec1a4ee9e072c24d44f6620c50b6` |

The selected standard is ENG-STD-DOTNET-XPLAT 1.1.0-draft.1. Its strict warning, restore-profile and explicit AOT architecture rules govern this probe. The development/autonomous guides govern reviewed dispatch, bounded failure handling and evidence preservation.

## Remaining work

Relational mapping/providers, alternate compilation symbols, full trimming, NativeAOT publication, native query execution, semantic correctness and performance are unverified. A future architecture decision needs an owner-selected workload/platform/support contract and independently expanded profiles. Annotation, explicit metadata registration, typed expression construction or generation are candidate approaches, not selected implementations. Do not set IsTrimmable/IsAotCompatible or add diagnostic suppressions from these findings.

AP0 closes only this analyzer-discovery slice. MariaDB compatibility, Milvus client parity, vector abstraction interoperability and all backend implementation gates retain their scope. The stopped V1aj final assessment and earlier exhausted budgets are unchanged; its profile still describes the separately reviewed0.110 metadata epoch. Full coverage/branch targets, owner/data/public/provider/platform decisions and the full roadmap remain open. No package publication, Git release or platform support claim follows.
