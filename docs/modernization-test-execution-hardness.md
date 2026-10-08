---
title: Observed test execution hardness report
version: 0.1
last_updated: 2026-10-03
status: Scoped verification; Oracle CLEAR
owner: IQToolkit project owner
tags: [modernization, testing, verification, atlas]
---

# Observed test execution hardness report

V1x adds an execution check to the ordinary verification gate. A successful test-runner exit must now be accompanied by populated, consistent MSTest results and a completed translation suite. The translation harness itself returns failure when no eligible methods execute. This implements the unexpectedly empty-run requirement in DOTNET-TEST-001 of the owner-selected .NET standard; it does not establish whole-project compliance.

## Edge cases tested

The installed runner produced an authentic zero-result TRX and exited0 after a filter selected the actual Test.TestHarness type outside the MSTest assembly. The unchanged private helper invoked the legacy translation harness with a reflection-selected empty method set; it also exited0 and printed ALL tests passed. These receipts, helper source and14 original compiled outputs are preserved under artifacts/test-execution/acquire.jmtt2iqz.

The new framework-only verifier accepted the retained populated report:86 actual Passed results,325 actual NotExecuted results and247 translations. It rejected the authentic empty MSTest report, empty translation output and an existing authentic failed MSTest report with specific diagnostics. All325 skips carry the SQL Server integration category. Actual result records govern skip counts because the installed adapter reports aggregate notExecuted=0 despite those skips.

After the narrow harness repair, the same helper source was rebuilt against the current Test assembly. Its single separately reviewed invocation exited1 and printed Total tests run:0 and No tests executed, with neither a success banner nor an unhandled exception. Runtime inputs and all336 current ordinary binary hashes remained unchanged.

Run-level errors, unsupported counters, linked identities, duplicate results, translation ordering and terminal completion have explicit source checks. Malformed XML, duplicate-report and every parser branch were not separately exercised with generated reports; no fabricated report evidence is claimed.

## Tool and service failure handling

The first verification attempt stopped before SDK compilation because its temporary directory was inside the checkout. The corrected attempt returned all expected command exits and its strict child passed, but the parent failed because cleanup required terminating a remaining owned process group. That process's identity was not recorded. Both FAILED results remain retained as verify.1u54_1z7 and verify.vw8rgqj3.

The third repair disables build servers and shared compilation. Restore/build/test also disable MSBuild node reuse; run receives only its supported options. Prospective review caught the run argument difference before dispatch. The exact installed SDK10.0.401 [run command definition](https://raw.githubusercontent.com/dotnet/sdk/v10.0.401/src/Cli/Microsoft.DotNet.Cli.Definitions/Commands/Run/RunCommandDefinition.cs) and [parser implementation](https://raw.githubusercontent.com/dotnet/sdk/v10.0.401/src/Cli/dotnet/Commands/Run/RunCommand.cs) substantiate that distinction.

The parent retains forced termination as failure, records PID/start time/executable before future termination, and restricts cleanup to owned groups. It does not globally stop build servers. The passing repair and helper runtime needed no forced termination, left no live owned group members and recorded no cancellation, cleanup or terminal evidence errors.

The protecting inspector required an explicit normalization between historical relative and current absolute binary paths. Source enumeration then exposed the existing TestResult* ignore rule hiding the verifier directory. A narrow .gitignore directory exception makes its three source files visible to Git; general build/evidence ignores remain. Both failed inspector sources are retained. This tracking-only correction leaves the already compiled executable sources unchanged; the final hash fence and Git enumeration qualify it without implying another SDK execution.

## Concurrency and load considerations

Root serialized writes, SDK work and helper execution; workers reviewed read-only. Existing project/test/dependency inputs were preserved. No concurrent-invocation or high-volume report qualification is claimed. Shared latest-success pointers are not a concurrency contract.

Parent deadlines are120seconds for the strict gate and60seconds for the helper build/runtime, with bounded owned cleanup. Logs and reports are limited to16MiB; combined task/helper storage is checked against512MiB. Storage polling is a rejection threshold with possible overshoot, not a disk quota. Only this local Linux ARM64 profile was exercised; the hosted OS/architecture matrix remains unobserved.

## Security and data exposure

The verifier uses bounded XML parsing with DTDs prohibited and no XML resolver. It requires the declared MSTest assembly/adapter/class identities, unique linked definitions and executions, supported outcomes and nonzero SQLite/RealProvider passes. Translation cases must be unique and contiguous, precede the summary and agree with its nonzero total; the success marker must be terminal.

Original and copied report/source bytes are bound before and after use. Failed attempts retain their evidence and do not replace the tool's success pointer. Compilation occurs in a private owned directory outside the checkout, with empty package feeds and framework-only references. The probe environment used controlled tool paths, no inherited credentials and disabled core dumps. Reports can include test names and local paths; no application records, authored provider payloads or new generated fixtures were introduced.

## Verification commands and results

- `bash -n eng/verify.sh eng/verify-test-results.sh`: passed.
- The four authentic result controls: expected exits0/1/1/1, each strict tool compilation with0warnings/errors; retained in verify.vw8rgqj3. The enclosing five-command attempt remains FAILED for the later lifecycle failure.
- `bash eng/verify.sh`: final passing run.rxihrkbf,86passes/325explicit SQL Server skips/247translations and0warnings/errors; source snapshots identical. Parent operation12.424463268seconds, including cleanup12.473115487seconds.
- The unchanged EmptyHarness Release rebuild: exit0,0warnings/errors; operation1.367044410seconds.
- The sealed EmptyHarness invocation: expected exit1 and actual empty-suite diagnostic; operation0.105308422seconds, including cleanup0.129896908seconds.

The reviewed repair bound34inputs/34snapshots; the separately reviewed runtime bound56inputs/56snapshots and14 actual rebuilt outputs. These are observed single-run timings, not performance guarantees.

## Outcome and remaining limits

The nonempty gate and empty-harness failure are verified in their stated scope. Current protecting inspection passed127 upstream source hashes,137 unchanged production inputs,12 archive/payload identities and all336 binaries with four intentional Test changes. Independent integrated Oracle review is CLEAR for this scoped checkpoint. Observed runtime Oracle CLEAR preceded disposition:14 original and14 rebuilt outputs are retained;42 source/output/controller records were rehashed. The owned outside workspace was moved on the same filesystem into ignored evidence, with source/controllers renamed to nonactive snapshots. Its original path is absent.

Library/configuration inputs and library package payloads did not change. Exactly four Test DLL/PDB files differ from the previous V1v qualification; the final repair and runtime left all336 current hashes stable. Retained packages preserve their earlier API and library payload evidence; their bundled README has not been requalified against the changed current README.

This packet does not freshly measure coverage. Earlier V1v coverage restoration is historical; required targets remain unmet and unwaived. Coverage tooling, provider certification, public API/data decisions and full roadmap completion remain open. No new commit, push or publication is part of this checkpoint.

The standards catalog retains43 canonical rule definitions and every other adoption record. TEST-001 remains PARTIAL; only its adoption observation/evidence/next action and catalog version0.1.2 reflect the new nonempty execution evidence. This is traceability, not protected enforcement or a compliance claim.
