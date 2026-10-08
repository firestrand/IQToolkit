---
title: Projection conversion ownership hardness report
version: 0.1
last_updated: 2026-10-03
status: Scoped verification; integrated Oracle CLEAR
owner: IQToolkit project owner
tags: [modernization, verification, atlas]
---

# ATLAS hardness report: projection conversion ownership

V1g in the [implementation plan](modernization-implementation-plan.md) verifies existing cleanup on a genuine conversion failure from unchanged native SQLite metadata. Production source, public APIs, dependencies, package bytes and locks remain unchanged. This is a first-GREEN Verify packet under selected ENG-STD-DOTNET-XPLAT1.1.0-draft.1, strengthening M0 ownership evidence without claiming phase or roadmap completion.

## Acquisition, independent oracle and provenance

Retained `artifacts/projection-conversion/runs/acquire.YH16jXfh` has one discovered passing acquisition control/no skips. An ordinary native connection supplies actual string/TEXT schema and first GetValue `ATOMIC_INTRINSICS=1`, matching independently captured native compile options. The actual BCL `Guid(string)` constructor receives that unchanged string and produces FormatException with message `Unrecognized Guid format.` and HResult-2146233033. These are captured observations, not hardcoded expected fixtures. Source/hash/TRX/native/runtime/BCL provenance are retained; acquisition Oracle review is CLEAR.

The installed engine remains SQLite3.53.3/source d4c0e51e...782c62; driver10.0.12 and runtime10.0.12 are captured from actual assemblies/runtime. BCL informational revision is10.0.12-servicing.26422.108+95017c711e6afc1085133d440e42b4bd78155701. The [matched runtime constructor](https://raw.githubusercontent.com/dotnet/runtime/v10.0.12/src/libraries/System.Private.CoreLib/src/System/Guid.cs) implements this operation. This independent BCL oracle avoids calling TypeHelper or FieldReader as its own expected result. Native SqliteDataReader.GetGuid follows a distinct path and is neither used nor presumed equivalent.

No invalid string, exception, result, custom failure stub or fixture mutation is fabricated. API/getter/connection-mode choices are operation configuration. Missing native source, nonstring response or missing eligible conversion failure blocks acquisition rather than supplying a substitute. V0.2 remains unanswered for separate generated-data tasks.

## Edge cases verified

Nine [tests](../IQToolkitTest/RealProvider/ProjectionConversionOwnershipTests.cs) cover the native acquisition control and eight explicit eager/deferred, borrowed-open/initially-closed, ReadGuid/ReadValue<Guid> combinations. Each case independently reacquires the actual native input/BCL error. A transparent projector records the actual string, invokes the existing getter, captures the genuine exception reference and bare-rethrows. Error type/message/HResult match the independent capture, and the caller receives that exact projected exception object.

Borrowed eager commands/readers exist before advancement; conversion fails on first MoveNext. Initially-closed eager execution fails during its existing buffering inside Execute. Deferred cases acquire nothing before advancement, then fail at first MoveNext. Actual reader IsClosed, exactly one command-disposal event/object identity and borrowed/owned connection state are asserted at the caller catch before enumerator Dispose or connection teardown can mask leaks. Native observers return base-created resources unchanged and do not clean them for the toolkit.

`artifacts/projection-conversion/runs/verify.dJ0tbERP` passes all nine on its first run. No artificial RED or implementation was created; source hashes and named-case native/error/resource observations are retained. This verifies one acquired projection-conversion error class, rather than all arbitrary projector, native read, acquisition, disposal, cancellation or transaction failures.

## Concurrency, failure handling and security

Root serializes SDK/instrumentation operations. Each case uses isolated in-memory native connections; no concurrent schedule, performance, load or allocation claim is made. Only library metadata and its actual conversion failure are retained, without business/tenant rows or credentials. Existing diagnostic-redaction and mutable-policy obligations remain open. Source/import/discovery/acquisition failures remain failures; no missing infrastructure case becomes a passing skip.

## Verification and package identity

| Check | Actual evidence |
| :--- | :--- |
| Filtered strict Release acquisition | `acquire.YH16jXfh`: one pass/no skips; native schema/value/BCL exception acquisition. |
| Filtered strict Release Verify | `verify.dJ0tbERP`: nine passes/no skips, all eight ownership cases. |
| Library input/archive identity | `artifacts/projection-conversion/library-identity-inspection.json`:137 current library/compiler inputs match API run.yD03s1yu;12 current DLL/PDB payloads match six original library/symbol archive payloads byte for byte; sole additional C# source is this test. |
| `GITHUB_ACTIONS=true bash eng/verify.sh` | `artifacts/verification/runs/run.hvvsiwY9`:zero warnings/errors,65 MSTest passes/325 unconfigured SQL Server skips,247 translation passes. |
| Existing managed coverage collector, `--no-build --no-restore` | `artifacts/projection-conversion/coverage.oi36avdq`:65 passes/325 skips,2693/8351lines (32.24764%),1436/5320branches (26.99248%); all336 captured DLL/PDB hashes restored immediately. Both report attachments have identical SHA-256. |

The [V1f package checkpoint](modernization-native-failure-hardness.md) remains the exact library/API/package/consumer evidence: six API comparisons, SQLite native execution and six independent locked public-symbol consumers bind run.yD03s1yu. This packet deliberately adds only tests/documents, so it retains that feed without repacking, regenerating locks or rerunning unchanged consumers. Direct input and payload identity checks gate that reuse. New test source is qualified separately from the historical API snapshot; no unchanged whole-worktree claim follows.

Coverage includes core/Data/SqlClient/SQLite only. Ratios improve over the preceding native-failure measurement (2683/8351lines,1432/5320branches). The private owning enumerator now reports87.5%lines/87.5%branches; broader reset/nongeneric/error/disposal paths and mandatory changed-file/full-project targets remain unqualified/unmet. No denominator exclusion or waiver was introduced.

Focused acquisition/source/Verify and final integrated Oracle reviews are CLEAR for V1g only. The reviewer independently confirmed the nine-case evidence, 250 stable strict inputs, coverage/restoration and unchanged library/feed identity. Coverage targets and the full roadmap remain open; no waiver is inferred.
