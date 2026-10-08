---
title: Native captured-array membership hardness report
version: 0.1
last_updated: 2026-10-03
status: Scoped Verify; integrated Oracle CLEAR
owner: IQToolkit project owner
tags: [modernization, arrays, atlas]
---

# ATLAS hardness report: native captured-array membership

V1a in the [implementation plan](modernization-implementation-plan.md) verifies existing nonempty array membership through the actual C# compiler, PartialEvaluator normalization, SQLite SQL binding and ordinary query re-enumeration. Both compiler-produced and explicit enumerable membership preserve A→B→A capture replacement, with and without QueryCache. This is first-GREEN verification of existing behavior; no production repair, null-boundary qualification or full M0 completion is claimed.

## Native acquisition and compiler binding

`artifacts/native-array/runs/acquire.xQZp3kbb` records one pass/no skips. Installed SQLite3.53.3/source d4c0e51e...782c62 and Microsoft.Data.Sqlite/runtime10.0.12 provide51 unchanged compile options. Independent ordinary-driver schema/rows confirm compile_options is System.String/TEXT. Existing public NativeProviderCacheTests mapping types describe that native column; no entity records are constructed by this test.

The first three actual native options select parameterized operations. A is the actual two-row response for ATOMIC_INTRINSICS=1 and COMPILER=gcc-13.3.0; B is the actual one-row response for DEFAULT_AUTOVACUUM. Arrays copy those returned sequences without changing strings. The arrays are nonempty and disjoint. Independent direct-driver parameterized IN requests produce each expected multiset. Parameters, native responses, schema, source/helper/mapping/library/project/lock hashes and TRX are retained. Selection of native values and A→B→A assignment are operation configuration; Oracle DP05 routing/acquisition reviews are CLEAR and pending V0.2 is not consumed.

The actual compiler-produced predicate binds to generic MemoryExtensions.Contains<string>(ReadOnlySpan<string>,string), bool/two arguments. Its first argument calls ReadOnlySpan<string>.op_Implicit(string[]); the second reads the mapped Name member. The explicit control actually binds to Enumerable.Contains<string>(IEnumerable<string>,string). Both signatures/declaring assemblies are retained before toolkit evaluation. No manually substituted expression tree or fallback control hides a different compiler shape.

## Edge cases and execution results

`artifacts/native-array/runs/verify.kdkmiww0` records five discovered passes/no skips: acquisition and four independent Verify methods. Exact source/hashes/TRX, actual SQL/native oracles and per-case captures accompany verify-observations.json.

| Ordinary execution case | Actual result |
| :--- | :--- |
| Compiler-produced membership, uncached | Same query returns A's two rows, B's one row, then A's two rows. |
| Explicit Enumerable control, uncached | Same A→B→A native result multisets. |
| Compiler-produced membership, fresh QueryCache | Same A→B→A native result multisets; observed cache counts1→2→2. |
| Explicit Enumerable control, fresh QueryCache | Same result multisets and observed counts1→2→2. |

Each case builds its query once, replaces the captured array reference and restores the original array without mutating either array. All twelve actual enumerations match complete independent native multisets. Actual generated SQL contains the mapped native table and WHERE; initial connection is Closed and returns Closed after every execution. Real AttributeMapping table/column names are asserted. SQL logs are retained per enumeration. No Count1, exact entry-history or array scalar-parameterization contract is imposed; counts are diagnostic. Focused Oracle review is CLEAR for this first-GREEN source/result checkpoint.

No QueryCompiler.Compile or predicate Compile call is made. These results qualify ordinary query re-enumeration with current captures, independently of explicit compiled-delegate snapshot semantics or pending same-root policy replacement. Null arrays, empty arrays, null elements, comparer overloads, nullable comparisons and arbitrary boundaries remain unqualified.

## Tool failures, concurrency and security

The new test file and private helpers are the only C# changes in this packet. No production, project, dependency, package or lock change is made. All acquisition/query I/O and SDK commands are serialized by root, using isolated native in-memory connections without shared concurrent I/O. No load, performance or general provider-concurrency claim follows.

Missing native data, unexpected compiler shape, empty discovery, skips or result mismatches fail rather than manufacture inputs or passing evidence. Any actual semantic mismatch would require a separately reviewed Test→Implement packet before library edits; none occurred. Native metadata and its SQL parameters contain no business, tenant or credential data. These tests do not authorize broader generated fixtures or weaken pending standards/coverage gates.

## Protecting gates and residual limits

| Protecting check | Actual evidence |
| :--- | :--- |
| `GITHUB_ACTIONS=true bash eng/verify.sh` | artifacts/verification/runs/run.nNY6RuFY:77passes/325unconfigured SQL Server skips,247translations, zero warnings/errors and256 stable in-run inputs. No disposable Probe method remains. |
| Managed collector, `--no-build --no-restore --collect 'Code Coverage' --settings eng/coverage.runsettings` | artifacts/native-array/coverage.jsoz2o89:77passes/325skips;2717/8351lines (32.53503%),1447/5320branches (27.19925%). Same denominators;8 additional covered lines/9 branches versus V1k. Four loaded core/Data/SqlClient/SQLite libraries only. Two byte-identical XML copies;336 fresh binary hashes restored immediately and still current. |
| Ordinary binary delta | artifacts/native-array/ordinary-binary-identity.json: full336 path set retained; only six IQToolkitTest DLL/PDB/ref/refint files differ from V1k. Production/other binaries are byte-equal. |
| Library/archive/consumer identity | artifacts/native-array/library-identity-inspection.json:138 library/compiler/config inputs unchanged from V1f API run.yD03s1yu,12 DLL/PDB payloads byte-equal to their six original library/symbol archive payloads; both retained consumer input sets and app/test/engineering locks unchanged. |

Test assembly changes are intentional. Exact unchanged production/archive/consumer identities support retaining [V1f API/feed/consumer evidence](modernization-native-failure-hardness.md); no repackage, lock refresh or new consumer run occurred. This packet does not claim whole-worktree/all-binary identity with [V1k](modernization-cold-initialization-hardness.md). Report/status-only post-gate deltas and current binaries are qualified in artifacts/native-array/final-input-inspection.json. No denominator exclusion, whole-project/touched target achievement or coverage waiver is claimed. Final integrated Oracle review is CLEAR for V1a only. The reviewer independently confirmed exact test/source fidelity,77tests/247translations0warnings,256stableinputs, no-regression coverage with unchanged denominators,336restored/current binaries and the six legitimate test-assembly deltas plus unchanged library/archive/consumer identities. Historical state labels were corrected.

Mandatory changed/full coverage targets, full array fact, broader ownership/cache/policy contracts, backend/OS certification and complete roadmap remain open. Passing first-GREEN Verify is evidence for this nonempty native case, not a new source fix or authorization answer.
