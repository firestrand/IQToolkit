---
title: SQL string literal escaping hardness report
version: 0.1
last_updated: 2026-10-03
status: Scoped verification; integrated Oracle CLEAR
owner: IQToolkit project owner
tags: [modernization, correctness, testing, atlas]
---

# SQL string literal escaping hardness report

The common SQL formatter now doubles apostrophes inside string values before writing the existing delimiters. The SQLite formatter inherits this behavior. The production change is one expression in `SqlFormatter.WriteValue`; signatures, other branches and LINQ parameterization are unchanged. This is scoped literal-formatting correctness evidence under the owner-selected **ENG-STD-DOTNET-XPLAT 1.1.0-draft.1**, not certification of every SQL dialect or a general injection finding.

## Edge cases tested

`NativeStringLiteralTests` acquires actual `sqlite_version()`, `sqlite_source_id()` and `quote(sqlite_source_id())` responses from the installed engine. The quote response is passed unchanged as a value, with an independent parameterized `SELECT @value` oracle. No test string, provider response, row or exception is fabricated. Acquisition requires nonempty native raw and quote-bearing strings; missing prerequisites fail rather than skip or substitute data.

The same five tests cover acquisition and common/SQLite formatters with raw/quoted native values. They require one TEXT/String column, one row, no second row and exact ordinal equality. RED `red.fk3wistp` records two native SQLite syntax errors and three passing controls. GREEN `green.cgw8ej9t` passes the identical test source with all five outcomes passed. Focused Oracle review is CLEAR for the acquired-data routing, causal RED and unchanged-test GREEN.

The observed engine is SQLite 3.53.3 with native source ID `2026-06-26 20:14:12 d4c0e51e4aeb96955b99185ab9cde75c339e2c29c3f3f12428d364a10d782c62`; driver Microsoft.Data.Sqlite 10.0.12 and runtime 10.0.12. This identifies the observed environment; it does not establish a support matrix.

## Tool and service failure handling

The first protecting run, `protect.ytuz8eio`, remains FAILED: its controlled PATH omitted the installed `rg`, and API verification stopped. The strict build had zero warnings/errors; all captured source and ordinary binary hashes remained unchanged. The retained retry pins the actual executable and changes only the verification environment. `protect.g3v8f3pa` passes API, package and symbol verification.

Root-owned parents retain exact commands, source copies, input/binary inventories, logs, actual exits and terminal cleanup. Each command has a 120-second deadline and 16MiB log rejection threshold. Combined polled storage bounds are 256MiB for focused tests, 512MiB for package/symbol or coverage packets, and 2GiB for CI consumers plus strict verification. These are polling/rejection thresholds, not hard storage quotas. No forced termination, remaining owned-session groups, cancellation or terminal evidence/cleanup errors were observed. Escaped sessions, adversarial filesystem races, crash recovery and timeout/cancellation branches remain unqualified.

## Concurrency and load considerations

All SDK/runtime operations were serialized by root. Independent agents reviewed source and evidence read-only. This change adds no shared mutable state. No throughput, allocation, concurrent invocation or adversarial string-length benchmark was run; apostrophe replacement may allocate a new string when replacement is needed.

## Security and data exposure

Literal contents are escaped through ordinal framework string replacement. Ordinary parameterization remains the existing execution path. The tests exercise actual formatter output against SQLite and retain original native exceptions; they do not establish safety for arbitrary raw SQL, identifier interpolation, other dialects, embedded NULs or hostile payloads.

Coverage tools authenticate DLL/portable-PDB identities, normalized PDB checksums, source documents, actual test execution and original preservation manifests. The private transition inspector retains the original descriptor and report unchanged. Its only physical aliases are ten retained old DLL/PDB files and the old formatter snapshot. Both Capture and direct source hashing resolve these aliases; current inputs use actual current files. Only Data/test compilations and the exact one-line formatter change are allowed to differ. The durable same-source collector/verifier are unchanged.

The transition receipt is published only after terminal input equality succeeds. Parent success additionally requires clean terminal process/evidence checks. Local receipts and agent reviews are verification evidence, not signatures, policy exceptions or release approval. Metadata, paths and test names are retained; no credentials or application records were introduced.

## Verification commands and results

Exact argument arrays and controlled environments are retained under `artifacts/sql-string-literal/`. Observed command families are:

| Check | Result / evidence |
| :--- | :--- |
| Strict analyzer build and five focused tests | RED: two genuine native failures/three controls; same-source GREEN: five passes, zero warnings/errors. |
| `bash eng/verify-api.sh <retained-1.1.1-baseline>` | Fresh `run.zAMzDw7M`: six API validations, unchanged reference closure. |
| `dotnet msbuild eng/VerifyPackages.proj -p:PackageDirectory=<fresh-feed> -warnaserror` | Six manifests/documents/assembly and symbol packages verified against current inputs. |
| `bash eng/verify-symbols.sh <fresh-feed>` | `run.dk1pVCJD`: six DLL/PDB pairs and 135 local source checksums; all 12 payloads match current ordinary binaries. |
| `bash eng/verify-ci-consumers.sh <fresh-feed>` | `run.9PLU3Ex4`: seven Toolkit-hash-only shadow lock reviews before six cold locked consumer builds and real SQLite execution; source locks unchanged. |
| `GITHUB_ACTIONS=true bash eng/verify.sh` | `run.kbUZfd3o` / `run.jq614PHs`: 91 passes, 325 explicit SQL Server skips, 247 translations, zero warnings/errors. |
| Copied-output native collection and private source-transition comparison | `coverage.zzhhnhb6`: 97 measured sources, 16,716 class/method leaves; every old covered leaf and branch/condition result preserved. |

The actual collector executes the same complete suite as the fresh strict run. It preserves all 336 ordinary binaries and 219 original output files, and restores the entire shadow output. Its ten commands exit zero, private helper builds have zero warnings/errors, and all 682 reviewed inputs remain unchanged. Collection takes 1.817 seconds and comparison 0.547 seconds before cleanup in this single observation. Owned coverage evidence/scratch totals 88,023,689 bytes before disposition.

Measured coverage moves from **2,728/8,349 lines and 1,459/5,320 branches** to **2,732/8,349 lines and 1,460/5,320 branches**. All leaf keys, denominators and branch-condition structure remain identical. Repaired line 781 is covered at both class level and `WriteValue(object)` method level; its historical hit count was zero. Coverage alone does not prove string semantics or individual branch-arm identity.

The original baseline descriptor SHA256 is `92ec8ed76f6319b09793a7098125cb3ded36acc98aa0d5959fbd39528de834c0`; current descriptor SHA256 is `173f9ce97c74f21f4c739f0afb74ce1abb596ed8e4ab7befd7280d849a940f10`. The current native report SHA256 is `ca75edd5f6dfd65037e8d0f85f8d446d73fca671c9b70ab0d683618d0d0188bc`. The explicit transition receipt accompanies this measurement; it does not replace or waive the old baseline.

## Remaining work

Required project coverage thresholds remain unmet and unwaived. Full final-gate integration, broader strings/dialects, hosted OS/architecture qualification, true async execution, new production providers and pending workload/public/data/policy decisions remain open. This checkpoint verifies one M0 correctness repair; it does not complete M0 or the roadmap. Final inspection verifies 137 production/configuration paths (only the formatter differs from V1v), 12 fresh archive payloads, seven consumer lock reviews, 336 current ordinary binaries, all coverage leaves and the unchanged 43 canonical standard definitions. TEST-001 remains PARTIAL. Seven private environments were retained with identical bytes on the same filesystem, with two helper project files made nonactive. The first checkpoint inspector source and its failed metadata receipt are preserved: its lock-review assertion expected a JSON status where the real tool emits a completion sentence; only that inspection expectation was corrected. No SDK check was weakened or repeated because of it. Independent integrated Oracle review is CLEAR for V1y only, including the final 300-path fence and retained dispositions; the wider roadmap and policy gates remain open.
