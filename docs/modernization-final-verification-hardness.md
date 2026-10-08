---
title: Final verification precursor hardness report
version: 0.1
last_updated: 2026-10-03
status: Scoped verification; integrated Oracle CLEAR
owner: IQToolkit project owner
tags: [modernization, verification, testing, atlas]
---

# Final verification precursor hardness report

`eng/verify-final.sh` now runs the available final checks as one command and binds their actual evidence. It deliberately rejects final readiness while required coverage, fact and seed validators remain incomplete. This implements Task V0.6a, an independent technical enabler; it does not complete V0.6, M0 or the modernization roadmap.

Technical work uses the owner-selected **ENG-STD-DOTNET-XPLAT 1.1.0-draft.1** at `/home/firestrand/Downloads/Cross-Platform-DotNet-Engineering-Standard-1.1.0-draft.1.md`. Its byte-identical repository snapshot has SHA256 `8c56930ec1cd26e8b58a273d47cb0b9713f55e6d4961b6d29a636ffaf4f0ebe2`. The existing development-plan and autonomous guides supply execution and coverage obligations; no alternative policy or exception was adopted.

## Edge cases tested

The actual full command passes API compatibility, package inspection, symbol provenance, seven private consumer graph reviews, six independent cold locked compiles, the native SQLite package consumer, strict verification and coverage comparison. Its final assessment then returns exit 1 for five explicit gaps: missing library coverage, insufficient default final line coverage, unfinished touched-file classifiers, unfinished mandatory fact bindings and unexecuted six-cache seed challenges. Build, setup, source drift or cleanup failure cannot qualify this readiness control.

The profile pins the original baseline, the qualified V1y anchor, the reviewed transition receipt and both historical executed-input manifests. The helper authenticates the original retained compilation/source aliases and all 198 executed transition inputs. It preserves the original baseline chain when using the current anchor for a repeat measurement. It does not reset a baseline from a success pointer.

Symbols and consumers use the same fresh feed as API verification. Archive hashes match their retained tested copies, and all six DLL/PDB package payload pairs match final ordinary outputs. Coverage binds the strict TRX from this invocation. Existing producers validate actual execution, package graphs, symbols, source checksums and coverage leaves; this precursor does not replace those validators.

A separate zero-argument invocation uses the exact retained compiled helper in an empty owned working directory. It returns native `evidence-integrity` rejection with exit 1, leaves that directory empty, preserves all five pinned inputs and leaves no live owned group. This verifies the malformed-argument output-ownership guard. It supplies no fabricated provider response, test row, JSON, XML or TRX.

## Tool and service failure handling

The first qualification, `qualification.fi81yk0c` / `run.N8qlhg3o`, remains FAILED. Its owned-tree scan encountered disappearing CLR diagnostic pipes and MSBuild temporary entries. The runner then forcibly terminated the consumer stage, whose native exit was 143. It did not reach readiness assessment. All 321 inputs, checkout paths and 336 ordinary DLL/PDB hashes remained unchanged, with no remaining observed owned groups or parent cleanup errors.

The correction retries an unsuccessful complete storage scan at most three times, with 20ms pauses. Every failed scan is retained; only a successful complete scan supplies the size. Exhaustion or an over-limit total rejects the run. No subtree is excluded and the 2GiB threshold is unchanged. The second run retains another genuine transient scan failure and completes its retry. These are polled rejection thresholds, not hard storage quotas.

Ordinary stages have 120-second deadlines; the compound coverage producer has a 300-second outer deadline while retaining its existing bounded child commands. Stage logs are limited to 16MiB, and newly owned evidence/scratch to 2GiB. The qualification parent has a 900-second lifetime bound. The framework helper limits individual captured files to 16MiB, input count to 2,048 and retained input bytes to 192MiB.

Each durable stage starts in its own Linux session. Cleanup enumerates every live process group in that session, including nested coverage jobs. The parent observes native stage PID markers, validates live leader ancestry/start identity, and tracks those sessions as well as its own runner session. Forced or incomplete cleanup rejects qualification. A disappearing `/proc` ancestor can cause a conservative rejection. Escaped sessions, adversarial races, crash recovery and general timeout/cancellation behavior remain unqualified; the first storage-induced termination does not certify those controls.

## Concurrency and load considerations

Root serialized all SDK/runtime operations. Independent agents reviewed source and actual evidence read-only. Concurrent final invocations and shared-output contention were not qualified. The parent permits only its single newly owned final-run directory. The command has no new public production API or runtime dependency.

The second qualification took 196.356 seconds and retained 1,131,783,412 owned bytes before parent archival. These are single-host observations, not throughput or latency guarantees. Coverage collection startup remains a separate performance investigation.

## Security and data exposure

Private workspaces use exclusive allocation and restrictive permissions. Inputs are retained and rehashed before terminal qualification. JSON rejects duplicate properties; XML disables DTDs and external resolution. Archive payload reads are bounded. Inputs and paths come from the declared local source/profile and actual producer output, not an unvalidated latest-success pointer.

Owned private projects become nonactive `.csproj.snapshot` files before relocation, with original names and hashes recorded. The first interrupted environment needed a separately recorded archival correction: all 3,028 file hashes were preserved with four project filename changes. Both qualification parent environments were retained on the same filesystem with unchanged bytes and their original outside paths absent. The successful final environment contains no active private project.

No credentials, application records, provider fixtures, seed defects or permission/approval records were introduced. Local hashes and agent verdicts are review evidence, not signatures, protected governance approval, policy waivers or release authorization. Other OS families, untrusted parent directories and hostile filesystem mutation remain outside this qualification.

## Verification commands and results

Exact command arrays, source copies, native outputs, identities and failure records are retained under `artifacts/final-verification/`.

| Check | Actual result / evidence |
| :--- | :--- |
| `bash -n eng/verify-final.sh` | Passed. |
| `bash eng/verify-final.sh artifacts/api-baselines/runs/run.5mbh8iJw/packages` | Second `run.PMtu7VZ4`: nine component stages exit 0; final assessment exits 1 for the named readiness gaps. |
| Framework helper restore/build | Empty feeds and isolated build-policy inheritance; zero warnings/errors. |
| API and package inspection | `run.1ppQTrwc`: six comparisons against authentic 1.1.1 packages and six package/symbol pairs. |
| Symbols | `run.P5nbfHkD`: six pairs and 135 local source checksums. |
| Consumers | `run.WtZyEWgY`: seven hash-only private graph reviews, six cold locked compiles and native SQLite execution. Source locks unchanged. |
| Strict gate | `run.pQDFgiJl` / `run.1e5k9FsJ`: 416 discovered results, 91 passes, 325 explicit SQL Server skips, 247 translations, zero warnings/errors. |
| Coverage | `run.0XtlECSC` / `run.lC8QVJvt`: 97 sources, 16,716 compared leaves, no regression against the qualified anchor. |
| Parent preservation | `qualification.x57a_kp2`: all 321 inputs, full checkout and 336 ordinary binaries unchanged; ten authenticated stage sessions, no forced termination or remaining live groups/errors. |
| Invalid-argument ownership | `argument-control.eynw9075`: exit 1, native rejection, empty working directory, five unchanged inputs, no live groups. |

Coverage remains **2,732/8,349 lines** and **1,460/5,320 branches** across four loaded libraries. Access and MySQL are missing from that library denominator; the translation executable is also outside the measured scope. The collector preserves all 336 ordinary binaries, all 219 test output files and its shadow output. Current coverage does not establish the required touched-file or whole-project targets. Individual branch-arm identity remains unverified.

## Remaining obligations

Complete V0.6 needs executable touched-file/layer and mandatory fact-binding classifiers, complete coverage scope and targets, and six genuine seed challenges killed by intended assertions with verified restoration. Setup/compiler failures cannot count as kills. V0.2 fixture authorization, V0.7 alternative-policy approval and the same-root policy contract remain unanswered. Broader nullable comparisons, cache boundaries/concurrency and resource ownership still require qualified evidence.

Async contracts and implementation, additional database providers, workloads, native/hosted support matrices, source retrieval, protected governance and later roadmap phases remain open. The command publishes no final-success pointer. Its current source-controlled profile depends on retained artifacts from this checkout and is not a portable hosted baseline. TEST-001 remains PARTIAL and the 43 canonical standard definitions remain intact. Independent observed and integrated Oracle reviews are CLEAR for the precursor only.
