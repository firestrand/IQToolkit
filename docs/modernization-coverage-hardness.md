---
title: Coverage acquisition and comparison hardness report
version: 0.2
last_updated: 2026-10-03
status: Scoped collector/comparison verification; integrated Oracle CLEAR
owner: IQToolkit project owner
tags: [modernization, testing, coverage, atlas]
---

# Coverage acquisition and comparison hardness report

V0.5c qualifies collection from a byte-identical copy of the test output and adds reusable supervised collection and read-only coverage comparison commands. The validator binds native Cobertura reports to measured DLL/PDB identities, source checksums, collection preservation manifests and observed test execution. It rejects empty coverage and checks matching class/method leaves for reductions. This implements a scoped baseline comparison under DOTNET-TEST-001 of the owner-selected ENG-STD-DOTNET-XPLAT1.1.0-draft.1; mandatory coverage targets remain unmet and unwaived.

## Edge cases tested

The installed SDK10.0.401 and locked Microsoft.CodeCoverage18.10.1 ran the unchanged test assembly from two independently copied output trees. Full collection produced86Passed/325NotExecuted and two identical2,083,887-byte reports. The complete historical/current XML is identical after changing only the root timestamp. The resulting four-module counts remain:

| Measured module | Covered/valid lines | Covered/valid branches |
| :--- | ---: | ---: |
| IQToolkit | 521/1381 | 378/824 |
| IQToolkit.Data | 2133/5942 | 1051/3676 |
| IQToolkit.Data.SqlClient | 9/560 | 2/431 |
| IQToolkit.Data.SQLite | 65/466 | 28/389 |
| Total | 2728/8349 | 1459/5320 |

The actual empty-discovery filter returned exit0, zero results and two identical178-byte reports. Those reports advertise line-rate1/branch-rate1 but contain no modules or denominators. The new validator rejects this genuine evidence with “No measured coverage modules or denominators.” No XML/TRX was fabricated or mutated.

Historical→current and current→self comparisons both pass:97 measured sources and16,716 compared class/method line leaves. Class-level sums reproduce the collector's8,349 lines and5,320 branches; method-level lines are compared separately, without counting them twice. Generated classes and source-line duplicates across classes remain part of the denominator.

Branch flags are the collector's actual True/False values. Exact line fractions carry covered/valid counts; condition records include arbitrary decimal percentages, including switch conditions. The comparison preserves condition-number/type structure and rejects reduced reported coverage. These reports cannot identify individual branch arms, so preserved counts do not prove the same arms executed.

Source checks exist for changed inputs, malformed/duplicate records, missing scopes, inconsistent totals and coverage regressions. Those branches were not separately exercised with invented reports. The observed controls establish acceptance of the qualified reports and rejection of genuinely empty coverage; they do not establish exhaustive parser or regression-detection coverage.

## Tool and service failure handling

The first acquisition, isolation.bpve1h8e, failed before SDK dispatch because it asserted213 output files; its reviewed manifest already contained the actual219. No collector ran. The corrected isolation.of6n2qzm passed, preserving all219 original files, all336 ordinary DLL/PDB paths/hashes and the checkout. Both shadow copies were restored normally. These observations do not prove originals were never temporarily instrumented or guarantee recovery after SIGKILL.

The first validator attempt, validate.qrugllst, compiled with zero warnings/errors, then rejected a real test-SDK document outside its initial normalized source-root assumption. The actual portable PDB names /_1/microsoft.net.test.sdk/18.10.1/build/net8.0/Microsoft.NET.Test.Sdk.Program.cs. Its SHA256 is A03E78A7ABD66B34604D00418EE1AD7875C28D0D9BB40047896B61DA1AA509CA; the existing UTF16 mapping identifies the package-cache root. The repair authenticates that source through the recorded mapping and PDB checksum instead of excluding it. The failed result and executed source remain retained.

The passing validate.2th3zxld parent ran the three reviewed commands with exits0/0/1 and checked the specific empty-report diagnostic. A compile failure cannot qualify the negative control. No process required forced termination; no live owned group, cancellation, cleanup or terminal evidence error remained. Source, all224 reviewed inputs and all336 ordinary binaries were unchanged during that attempt. The rejected control retained a failed result and preserved the previous success pointer.

## Concurrency and load considerations

Root serialized writes and SDK work; workers and Oracle reviewed read-only. The parent bounds each complete restore/build/validation command to120seconds, logs/reports to16MiB and new owned validation evidence to256MiB. The validator retains at most2,048 inputs and128MiB of copied bytes, with16MiB per file. Polling bounds reject overshoot; they are not filesystem quotas.

Only Linux ARM64 was exercised. Concurrent invocation, adversarial path substitution, every filesystem and the hosted platform matrix remain unqualified. The shared latest-success pointer is not a concurrency contract. The shell follows existing private-build retention patterns; the reviewed parent supplied deadlines and cancellation supervision. The standalone shell command does not itself provide that supervisor or a collector.

## Security and data exposure

The framework-only tool builds outside the checkout with empty feeds, repository imports/CPM disabled and strict warnings. Bounded XML parsing prohibits DTDs and external resolution. It snapshots each referenced input before parsing and rehashes originals before reporting success. DLL CodeView identity and normalized portable-PDB checksum must match; all resolved documents use their declared SHA256 algorithm. Measured and test DLL/PDB pairs must also match the collection's unchanged preservation manifests.

The validator resolves measured repository sources through the explicit /_/ root mapping. The test assembly's actual cached SDK document additionally uses the recorded /_1/ mapping. A commit-specific Source Link URL alone does not authenticate uncommitted code. Changed compiled identities, source hashes, settings, collector, mapping or coverage structure require explicit requalification; the tool does not exclude changed sources or reset the baseline automatically.

Descriptors and strict-result receipts are reviewed evidence inputs, not signed approval records. The validator checks receipt hashes and independently matches nonempty collector/strict TRX identities and outcomes. It relies on the existing strict validator's recorded translation result; it does not rerun translation validation or establish that a supplied baseline was approved. Reports include source/test names and local paths. No application records, provider payloads, generated test cases or credentials were introduced.

## Descriptor contract

Run `bash eng/verify-coverage-results.sh <baseline-descriptor.json> <current-descriptor.json>`. Each descriptor is schema1 provenance metadata over untouched actual evidence. Every Input is an absolute Path and SHA256 Sha256. The private build and copied evidence remain under artifacts/coverage-results/runs; project files become nonactive .csproj.snapshot files before relocation.

| Field | Required evidence |
| :--- | :--- |
| Schema / RepositoryRoot | Version1 and absolute measured checkout root. |
| Reports | All actual Cobertura attachment copies, with unique canonical paths and identical hashes. |
| CoverageTrx | Actual collection test executions. |
| StrictTrx / Translation / StrictReceipt | Passing strict TRX, translation output and the existing verifier's hash-bound verified receipt. |
| Settings / Collector / Mapping | Exact runsettings, installed collector package archive and actual UTF16 source-root mapping. |
| Modules | The four measured module names, each with actual Dll/Pdb Inputs. |
| TestAssembly | IQToolkitTest with actual Dll/Pdb Inputs. |
| Preservation | Before/After Input pairs containing nonempty equal path/hash dictionaries; binary paths are relative to RepositoryRoot. |

The observed descriptors are retained under artifacts/coverage-probe/descriptors. Historical/current descriptors reference the actual historical and new reports. Their current source/module identities independently match historical symbol and binary receipts. These local descriptors are evidence, not a portable checked-in baseline or a policy exception. Relocation/mapping changes need reviewed qualification.

## Verification commands and results

- `bash -n eng/verify-coverage-results.sh`: passed.
- Historical descriptor→new full descriptor: exit0, run.QVFhh04u,2.617742seconds before cleanup.
- New full descriptor→itself: exit0, run.1Yvs5HNe,2.556691seconds before cleanup.
- Historical descriptor→genuine empty descriptor: expected exit1, run.ehgS4BD8,2.403856seconds before cleanup.
- All three private builds: zero warnings/errors; each input snapshot remained unchanged.

These are observed single-run timings, not performance guarantees. Baseline→current and self-comparison each report2,728/8,349 lines and1,459/5,320 branches,97 sources and16,716 leaves. The latest-success pointer identifies the passing self-comparison; the empty control has its own failed evidence directory.

## Reusable collector qualification

`bash eng/verify-coverage.sh <baseline-descriptor.json> <passing-strict-run-directory> [<vstest-filter>]` builds a framework-only metadata helper with empty feeds outside the checkout. It verifies the passing strict run's complete current source snapshot and linked execution receipt, authenticates the selected installed collector files against the pinned archive, records complete original binary/output/input sets and copies the actual test output including its hidden UTF16 mapping. No new package or product-runtime dependency was added.

The Bash supervisor assigns each SDK bootstrap, build, helper, collector and comparison stage an owned job group under GNU timeout:120 seconds, TERM followed by 5 seconds to KILL. It monitors 16MiB logs and 512MiB owned evidence/scratch rejection bounds. The qualification parent tracks all groups in its owned session and additionally counts existing protected evidence against its 512MiB limit. Limits are rejection thresholds rather than quotas. Process and effective operation exits are recorded separately; forced termination, cancellation, source drift or failed terminal preservation prevent the collector's success pointer from advancing.

The independently successful comparator has its own pointer. It may record a completed comparison even if subsequent collector preservation fails. The genuine empty control below preserved both pointers. This supervisor is explicitly Linux-specific; other adapters remain required work. Escaped sessions, abrupt SIGKILL recovery, adversarial path races, concurrent invocation and the unobserved deadline/cancellation branches are not qualified.

The sealed parent `artifacts/coverage-collector/verify.b9qy6fkk` passed with 764 unchanged reviewed inputs and 13 prospective source snapshots:

| Operation | Observed result | Time before cleanup |
| :--- | :--- | ---: |
| Fresh strict run.2TG4NYt9 | Exit 0; 86 passed / 325 explicit SQL Server skips / 247 translations. | 12.5447 seconds |
| Full collector run.JbUMhIGI | Exit 0; comparison run.6bQYOaCL verifies 97 sources / 16,716 leaves. | 114.0220 seconds |
| Empty collector run.RUlBWjnt | Native exit 0; overall exit 1 with `No measured coverage modules or denominators.` | 113.3360 seconds |

All builds recorded zero warnings/errors. No command needed forced termination; no live owned session members, cancellation, cleanup or terminal-evidence errors remained. Collection and terminal checks preserved all 336 ordinary DLL/PDB paths/hashes and 219 original output files. Both 219-file shadows were restored. The complete historical/full XML matches after removing only the root timestamp. Full report copies are 2,083,887 bytes with SHA256 FE28709CAC50E32BC7B322DE8443B0FFF0CF4113C690CE8AC804A5BE297FA57D; empty copies are 178 bytes with SHA256 99C0D67A750DEBAFF3B01CE03FFE7B4B7D78F4187FB7C3D882954DA5B82D9054 and no measured packages. Actual counts remain 2,728/8,349 lines and 1,459/5,320 branches across the same four modules.

These full/empty timings are slower than the earlier isolated acquisition. The retained logs provide no timestamped startup diagnosis, so the cause is unverified; no causal NuGet-cache claim or environment repair is inferred. The displayed SDK runner is VSTest 18.7.0 arm64; the locked test-SDK/collector packages are 18.10.1. Timings are single observations, not latency guarantees.

The collector's private workspaces and complete restored shadows are retained in each run's `environment`; active helper project files were renamed to byte-identical `.csproj.snapshot` files. The parent environment was moved on the same filesystem into its `retained-environment`; 58 files retain identical hashes and the original path is absent. Origin paths in runtime receipts remain provenance and are resolved through the disposition map. Final combined evidence before parent disposition was 285,437,755 bytes, below 512MiB.

Prospective source/exact-command, observed and integrated Oracle reviews are CLEAR for V0.5c.1b. The integrated review confirms the current tooling and774-path fence, only the five declared documentation/adoption deltas, protected library/archive/binary identities and retained disposition. No timeout/cancellation control or required coverage-target claim follows from these normal/full and genuine-empty observations.

## Outcome and remaining work

The copied-output acquisition, reusable comparison and bounded collector are verified in the observed Linux ARM64 scope. Final-gate integration, timeout/cancellation controls, Windows/macOS adapters and qualification, fixture authorization, the coverage-policy decision and full roadmap completion remain open. Unloaded MySQL/Access libraries and the translation executable are outside the measured four-library scope. Neither the percentages nor these controls satisfy the mandatory final/touched-file targets.

Protecting inspection passes all137 production/configuration input identities,12 archive and payload identities, and all336 current binaries against the retained V1x baseline. Library and dependency inputs did not change, so their prior API/package evidence remains applicable in that scope. This does not requalify packaged README content after the current README update or rerun the hosted platform matrix. Historical checkers remain unchanged.

Four owned outside workspaces were moved on the same filesystem into ignored evidence without duplicate output copies. Their complete file/hash maps match, original paths are absent and retained projects are nonactive snapshots. The disposition receipt and current source/documentation hash fence are retained under artifacts/coverage-probe. This cleanup preserves the original failed attempts and successful shadow outputs.

The catalog keeps all43 canonical definitions; TEST001 remains PARTIAL. Its adoption evidence records this comparison without claiming protected enforcement or whole-project compliance. No new commit, push, tag or package publication is part of this checkpoint.
