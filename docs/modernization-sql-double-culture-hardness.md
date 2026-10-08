---
title: SQL floating-literal culture hardness
version: 0.1
last_updated: 2026-10-03
status: Scoped verification; integrated Oracle CLEAR
owner: IQToolkit project owner
tags: [modernization, globalization, sql, verification]
---

# ATLAS hardness report: native Double SQL literal culture

V1u corrects one machine-formatting defect under DOTNET-GLOB-001 of the owner-selected .NET standard. Common and SQLite formatters previously emitted a comma inside a native Double literal under `fr-FR`, turning a single numeric value into two SQL columns. The shared Single/Double conversion now explicitly uses `CultureInfo.InvariantCulture`; its existing `.0` suffix rule and all other branches remain unchanged. No public API, dependency or project configuration is changed by this packet.

## Edge cases tested

[NativeFloatingLiteralTests](../IQToolkitTest/RealProvider/NativeFloatingLiteralTests.cs) acquires the installed SQLite source ID/check-in timestamp and its actual `julianday` REAL result. Each formatter case binds the unchanged Double into an independent direct-driver parameter command as its oracle. The installed comma-decimal culture is operation configuration, not generated data. No expected numeric value, bit pattern, SQL literal or timestamp is embedded as a fixture.

Acquisition `artifacts/sql-double-culture/acquire.e0ptmiue` passes one case and records actual SQLite3.53.3/Microsoft.Data.Sqlite10.0.12/native source identity. The actual fractional Double and parameter oracle match exactly. Source/actual acquisition and prospective behavioral reviews are Oracle CLEAR.

Semantic RED `red.8nkipefi` retains two planned column-count failures and three passing acquisition/invariant controls. Both comma-culture formatters produce two native columns (INTEGER and REAL), whereas the oracle has one REAL Double. GREEN `green.o1ns6d_b` passes all five unchanged tests with the same acquired inputs/oracle bits. The sole production change is provider-aware conversion; source snapshots and both outcomes are retained. Focused Oracle review is CLEAR.

The evidence qualifies this actual Double through both public formatter entry points, under invariant and installed `fr-FR` cultures. Single is not independently qualified even though its shared conversion changes. Integral/exponent/nonfinite/negative values, Decimal, other numeric branches, wider cultures and other provider dialects remain separate requirements. The `.0` mechanics are preserved, not certified for every possible floating literal.

## Tool and service failures

The two RED failures are semantic evidence, not setup failures. Restore/builds and focused GREEN complete without warnings/errors. Native acquisition prerequisites require an actual finite fractional Double and available comma-decimal culture; an unavailable prerequisite must fail rather than substitute data. Original evidence is retained. The first acquisition TRX was moved unchanged into its unique run directory; the relocation note records its original location. Subsequent runs use unique result paths.

## Concurrency and security

Tests are marked `DoNotParallelize`, assign only the current thread's `CurrentCulture` and restore the original culture in `finally`, including RED paths. No default-thread or UI culture is changed. Root serializes all SDK work; agents review read-only. This test serialization is not a library concurrency or load guarantee.

The SQL input is actual non-sensitive engine source metadata and an unchanged native numeric response. Test SQL has no caller-supplied strings, business records or credentials. This narrow formatting fix does not establish general SQL injection protection or certify other literal branches.

## Verification

Strict focused builds pass with zero warnings/errors. Fresh API `artifacts/api-compatibility/runs/run.dZ0zbZ5m` completes all six comparisons against the pinned1.1.1 baseline, resolving reference closure without suppression. Package inspection `packages.aglakryb` validates six library/symbol manifest pairs. Symbol `artifacts/package-symbols/runs/run.C1vDvSNK` checks six pairs and their commit-bound Source Link/checksums. Dirty source metadata does not prove clean remote source retrieval or artifact disclosure approval.

Explicit independent preparation `run.Hjhs174s` and SQLite preparation `run.KzfFikFn` refresh seven actual locks against that exact feed. `consumer-lock-review.json` uses the existing strict verifier: only feed-bound Toolkit contentHash leaves may change; every node, edge, version, external hash and the core-only one-node graph is preserved. App/test locks are unchanged. CI consumers `artifacts/ci-consumers/runs/run.n330EmZ1` pass seven shadow lock reviews, six cold locked public-symbol compiles and the native SQLite executor. No CI guard is weakened.

Fresh ordinary `artifacts/verification/runs/run.g5qrT3M8` passes82tests/325unconfiguredSQLServer skips/247translations with zero warnings/errors and262stable checkout inputs. Fresh collector `artifacts/sql-double-culture/coverage.l2fhyvht` measures2724/8349lines(32.62666%) and1449/5320branches(27.23684%), compared with V1t2717/8349 and1447/5320. Only core/Data/SqlClient/SQLite load; no workflow coverage target or exception is satisfied by this limited increase. All336captured DLL/PDB hashes restore immediately and match the ordinary outputs.

`python3 -I artifacts/sql-double-culture/validate-checkpoint.py` passes: same actual RED/GREEN sources/inputs/restorations, precisely one production conversion change,137current library/configuration inputs,12current archive payloads,135symbol source checksums, seven exact hash-only locks, consumers, strict gate and restored coverage are bound to current source/binaries. Later documentation changes are limited to plan/state/this report. Final integrated Oracle review is CLEAR for V1u: the minimal repair, actual unchanged RED→GREEN, fresh package/consumer/strict/coverage evidence and current provenance are independently verified. Previous V1t payload/coverage identities are historical following this production edit. Full workflow coverage targets, hosted/native matrix, pending data/policy/public decisions and the complete roadmap remain open.
