---
title: Native implicit mapping identity hardness report
version: 0.1
last_updated: 2026-10-02
status: Scoped implementation; phase closure open
owner: IQToolkit project owner
tags: [modernization, verification, atlas, mapping]
---

# ATLAS hardness report: implicit mapping identity

This report retains the mapping checkpoint and its original feed/consumer/coverage identities. The later combined batch/eager checkpoint is in the [eager ownership report](modernization-eager-ownership-hardness.md); the mapping implementation and focused proof remain unchanged.

V1m in the [implementation plan](modernization-implementation-plan.md) fixes an observed type-only cache in [BasicMapping](../IQToolkit.Data/Common/Mapping/BasicMapping.cs). A second table with the same CLR projection previously reused the first table's entity and SQL. The cache now uses CLR type and resolved table ID, resolving the existing default before lookup and preserving canonical identity for repeated pairs. Public signatures are unchanged; null CLR types remain rejected.

## Actual source and cases tested

The [six regression tests](../IQToolkitTest/RealProvider/ImplicitMappingIdentityTests.cs) acquire actual installed `pragma_function_list` and `pragma_module_list` reader schemas before capturing rows. Both schemas supply a string `name` column. Direct SQL controls and ORM execution share the tested connection; duplicate native function names remain in the complete result multisets. SQLite can register pragma modules during preparation, so both schemas are acquired before either oracle is captured. No entity records or replacement rows are constructed.

Native SQLite is 3.53.3 with source ID `2026-06-26 20:14:12 d4c0e51e4aeb96955b99185ab9cde75c339e2c29c3f3f12428d364a10d782c62`; recorded observations contain 192 function names and 13 module names. The same-source pair retains four semantic failures and two passing controls at `artifacts/implicit-mapping/runs/red`, then six passes at `runs/green`. Wrong TableId, both table acquisition/execution orders and omitted-ID default resolution reproduce the defect. Single-table execution and repeated canonical requests remain green. All native provenance/schema/row observations agree across the pair. Oracle focused source/pair review is CLEAR.

The omitted-ID public overload invokes the existing resolver configured with the actual native module identifier. Empty-string and null-Type boundary fixtures were not created; their error paths remain unqualified. A reported covered method does not establish those boundary contracts.

## Integration and coverage

| Check | Observed result |
| :--- | :--- |
| Strict CI-profile gate | Final `artifacts/verification/runs/run.0gNSTn47`: locked restore, strict rebuild with zero warnings/errors, 39 MSTest passes/325 SQL Server skips and 247 translation passes; 242 stable in-run inputs. Subsequent report/status bookkeeping does not change compiled inputs. Earlier mapping run.oxvsAkvr also passes. |
| Static API compatibility | `artifacts/api-compatibility/runs/run.dqoq1XYU`: six fresh SDK package comparisons against the rebuilt pinned v1.1.1 baseline; stable in-run compilation/reference/package identities. Separate consumer locks are intentionally prepared afterward for those exact archives. |
| Package inspection | `artifacts/implicit-mapping/package-inspection.log`: all six library/symbol pairs, manifests, bundled documents and assemblies pass. |
| Managed coverage | `artifacts/implicit-mapping/coverage._c45ry4i`: 39 passes/325 skips; 2595/8307 lines (31.23871%), 1405/5312 branches (26.44955%). All 336 captured DLL/PDB hashes agree immediately before and after collection. |

Coverage loads core/Data/SqlClient/SQLite, excluding unloaded providers and the translation executable. Scoped ratios exceed the preceding deferred run's 2554/8305 lines and 1386/5312 branches. The collector reports `GetEntity(Type, string)` at 100% of its instrumented lines/branches, but BasicMapping overall remains 29.57746% lines/22.22222% branches. Full-project and touched-file targets remain unmet; no waiver is approved.

## Failure, concurrency and security boundaries

Unavailable or indistinguishable native metadata fails acquisition instead of generating a fallback. Setup failures do not count as semantic RED. No error injection, provider simulator, business data or credentials enter evidence. Source/package/snapshot changes fail the engineering gates.

The existing dictionary concurrency model is unchanged. No concurrent mapping, tenant isolation, constructor selection, mutable policy or arbitrary identifier/security certification follows from these native identity tests. Eager/batch reader ownership, errors, cancellation, generated array/cache evidence, protected governance and later roadmap slices remain required. Final independent Oracle review of the integrated implementation, retained evidence, reports and state is CLEAR. That verdict supplements executable checks; this report closes neither M0 nor the full objective.

The native schema requests are grounded in [SQLite pragma documentation](https://www.sqlite.org/pragma.html#pragma_module_list) and [version3.53.3 pragma definitions](https://raw.githubusercontent.com/sqlite/sqlite/version-3.53.3/tool/mkpragmatab.tcl). Live installed schemas and results supply the actual test evidence.
