---
title: Modernization foundation hardness report
version: 0.3
last_updated: 2026-10-02
status: Partial implementation checkpoint
owner: IQToolkit project owner
tags: [verification, modernization, atlas]
---

# ATLAS hardness report: modernization foundation

This checkpoint covers packaging, CI declarations, private-member cleanup, centralized version/dependency metadata, source-derived standard adoption documentation, editor preferences and local engineering tools on Linux ARM64. It applies the owner-selected `ENG-STD-DOTNET-XPLAT` version `1.1.0-draft.1` under its legacy adoption scope. The [full implementation plan](modernization-implementation-plan.md) remains active; broader cache qualification, resource ownership, async execution and new providers remain incomplete. A later narrow nonquery command-disposal pair is documented in its separate [ATLAS report](modernization-command-ownership-hardness.md), with refreshed gate/API/coverage evidence; a subsequent [cache ATLAS report](modernization-cache-hardness.md) records scoped execution/MRU/root repairs and native relational characterization. The foundation observations below remain historical to their stated snapshot.

## Edge cases tested

- The package contract rejects the actual historical release's seven packages, including the Test executable, and accepts the current six library and six symbol packages. Bundled README/license hashes match current source.
- API resolution initially exposed missing transitive references; the corrected gate resolves them and rejects incomplete reference diagnostics. All six actual SDK comparisons execute with fresh validation semaphores.
- The pinned-release baseline rebuild succeeds in a fresh directory. Current API packages are built from fresh sources and retained by their actual SDK output paths, with hashes unchanged after comparison.
- Static managed coverage instrumentation completes on ARM64 and restores every checked DLL/PDB hash. Its measured scope excludes unloaded providers and the separate translation executable.
- The explicit dead-code analyzer initially reported seven private-member findings. After direct/reflection review, those private declarations were removed; the ordinary gate now runs the same analyzer profile and passes with zero warnings. Public/protected contracts and inherited tests are preserved.
- All eight actual assemblies retain `AssemblyVersion=1.1.1.0`; generated FileVersion is `1.1.2.0` and informational metadata identifies the unreleased `1.1.2-preview.1` package. Existing non-version assembly attributes are retained.
- Real locked restores succeed after moving existing versions to Central Package Management. All eight resolved package identities/content hashes and all six library nuspec dependency contracts match the rebuilt release, except own library package versions. No runtime dependency was added.
- Actual CI-profile builds reject `LocalWarningRelaxation=true` and `TreatWarningsAsErrors=false`. Both default local and explicit CI profiles pass here; native platform qualification outside this Linux ARM64 checkout remains unverified.

- The selected-source snapshot is byte-identical to the owner-selected file. The catalog inspection validates all 43 declarations, exact titles/forms/ranges, five parent relationships, the sole clause and 49 real evidence paths; the initial inspection exposed two incorrect paths, which were corrected before validation passed.
- Scoped .editorconfig preferences introduce no C# rewrite, suppression or API change. The refreshed strict build, existing suites, six API comparisons and package inspection all pass. Full protected governance and CI style/security/naming enforcement remain incomplete.

## Tool and service failure handling

Verification commands fail fast, preserve each attempt's logs and failure status, and update their success pointer only after all required checks pass. Source snapshot changes fail verification; actual successful runs have matching before/after snapshots. The snapshot-change rejection branch has been inspected, not exercised with a concurrent mutation. Missing baseline artifacts or unresolved API references are failures rather than certification skips. Historical release dependencies are not locked, so baseline preparation does not promise identical historical package bytes.

## Concurrency and load considerations

Build, restore, package and instrumentation operations were serialized through an exclusive lease in the shared checkout. Per-run directories isolate evidence, but the scripts are not safe for concurrent use of shared build outputs. No database throughput, application concurrency, retrieval quality or cancellation performance claim follows from these checks.

## Security and data exposure

No new provider requests, credentials, generated fixtures or production records were introduced. CI declares read-only repository permissions, pinned action commits and disabled persisted checkout credentials. NuGet source mapping uses the verified existing public feed and a project-local ignored package cache. Restore audits direct/transitive dependencies at high/critical severity; the actual vulnerability listing reported none in this snapshot, which does not guarantee future results. API support packages are pinned/locked metadata references and are not runtime library dependencies. Logs and manifests record build/source/package provenance; NuGet caches are excluded from the added API evidence upload paths. Existing parameter logging and tenant-policy cache risks remain work for later slices.

## Verification commands and results

| Command or check | Actual result and evidence |
| :--- | :--- |
| `GITHUB_ACTIONS=true bash eng/prepare-api-baseline.sh` | Passed; pinned source, zero-warning/error build and seven original packages. Parent CPM, lock generation and build targets explicitly disabled for historical source. `artifacts/api-baselines/runs/run.5mbh8iJw`. |
| `GITHUB_ACTIONS=true bash eng/verify-api.sh <reproduced-packages-directory>` | Passed after editor configuration; six static API comparisons, stable source/package/reference hashes. `artifacts/api-compatibility/runs/run.NNVtl4wi`. This is a local CI-profile exercise, not a hosted run. |
| `dotnet msbuild eng/VerifyPackages.proj -p:PackageDirectory=<current-api-run>/packages -warnaserror` | Passed for six preview library/symbol pairs, including current README/license bytes; `artifacts/standard-adoption/editorconfig-packages.txt`. |
| Same package inspector against the reproduced historical packages | Correctly rejected seven packages; `artifacts/reproduced-baseline-contract-rejection.txt`. The reusable contract was added after the original metadata inspection; no earlier executable RED is claimed. |
| `GITHUB_ACTIONS=true bash eng/verify.sh` | Passed with matching source snapshots: zero warnings/errors including unused-code diagnostics, 7 MSTest passes, 325 unconfigured SQL Server skips, 247 translation passes. `artifacts/verification/runs/run.GGT0LVPl`. |
| `bash eng/verify.sh` | Earlier local profile passed before editor configuration: zero warnings/errors, 7 passes/325 integration skips and 247 translation cases, matching before/after source snapshots. `artifacts/verification/runs/run.MZhyQfF5`. The later editor configuration was reverified by run.GGT0LVPl and API run.NNVtl4wi; all 172 then-current compilation/API inputs matched the latter. The separate command-disposal slice subsequently changed source and refreshed its own gates. Documentation bookkeeping does not change those inputs. |
| Coverage collector with `eng/coverage.runsettings` | Passed; 2294/8300 lines (27.63855%) and 1230/5314 branches (23.14641%) in the same four loaded libraries, all 52 checked DLL/PDB hashes restored immediately after collection. Subsequent rebuilds supersede those output bytes. The final local-profile collector repeat retains byte-identical before/after hash manifests and an explicit restoration result under `artifacts/standard-adoption/coverage._xkbmv7f/`; its metrics and test counts match the earlier CI-profile measurement. Both ratios exceed the earlier measured baseline after declaration removal. `artifacts/standard-adoption/coverage-summary.json`. |
| Explicit IDE0051/IDE0052 analyzer rebuild from Task V0.8 | Original probe failed with seven findings; corrected standalone and ordinary-gate rebuilds pass. `artifacts/dead-code/probe.txt`, `artifacts/standard-adoption/analyzer-build.txt` and ordinary gate build log. |
| Actual assembly/dependency inspection and CI rejection invocations | Passed; actual metadata, graph/nuspec comparisons and both CI configuration rejection logs in `artifacts/standard-adoption/`. |
| Actual catalog/source inspection | Passed; all 43 canonical definitions, five parent relationships, sole clause and 49 existing evidence paths. Selected and repository-source bytes match the recorded SHA-256. Four artifact hashes retained in `artifacts/standard-adoption/catalog-inspection-final.txt`. This is an inspection, not the protected V0.9b gate. |
| Bash syntax, XML/YAML structure and `git diff --check` | Earlier checks passed; resumed shell/XML checks and diff check pass. Oracle engineering and documentation reviews CLEAR for scoped integration. Hosted CI, native Windows/macOS execution and runtime package consumers remain unverified. |

Independent Oracle review was CLEAR for the foundation checkpoint and V0.9a/c delta: source/API input hashes, package bytes, metadata, test counts, CI rejection evidence and durable coverage restoration manifests match the recorded results. This verdict has the scope stated above.

## Remaining work

The C# standard selection is resolved. Generated-fixture authorization and the legacy coverage-policy decision remain pending before dependent implementation. Establish focused cache/array/resource regressions and runtime consumer checks, complete protected standard governance/enforcement and the extended final gate, then proceed through the complete roadmap. All 43 rules now have source-derived adoption dispositions, and a byte-identical standard snapshot makes catalog inspection portable. Nullable/full-style retrofit, protected governance controls and source retrieval from a clean committed checkout remain unestablished. The four declared OS/architecture CI jobs remain unexecuted. This report does not close M0, the final verification gate or the overall goal.
