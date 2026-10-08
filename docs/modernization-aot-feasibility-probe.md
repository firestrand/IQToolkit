---
title: IQToolkit trimming and NativeAOT feasibility probe
version: 0.2
last_updated: 2026-10-04
status: Core source and analyzer discovery verified; observed Oracle CLEAR
owner: IQToolkit project owner
tags: [modernization, discovery, dotnet, trimming, nativeaot]
---

# IQToolkit trimming and NativeAOT feasibility probe

This independent probe addresses `LOCAL-RM-DISCOVERY` in the [roadmap](modernization-roadmap.md). It investigates compatibility; it does not select NativeAOT as the product architecture or promise support. V1aj's failed final assessment and exhausted repair budget remain unchanged.

## Source findings

The inspected profile is SDK 10.0.401, runtime 10.0.12, `net10.0`, Linux ARM64. The installed runtime's `.version` identifies `dotnet/dotnet` commit `95017c711e6afc1085133d440e42b4bd78155701`. Acquired source and current toolkit hashes are retained in `artifacts/aot-feasibility/source.4jt4zk59/` and `source-inspection-r1.json` (SHA256 `508c1fb12b65706dc01171e8b790675150e77f1a78edf00122783fd0e293d020`). No SDK or compiler ran during source acquisition.

| Existing path | Confirmed mechanism | Probe implication |
| :--- | :--- | :--- |
| [EntityProvider.Execute](../IQToolkit.Data/EntityProvider.cs) | Builds and compiles an expression plan for ordinary/cached execution. | Do not equate `Compile()` with an unavoidable native-code-generation failure. |
| [PartialEvaluator](../IQToolkit/PartialEvaluator.cs) | Compiles evaluable subexpressions; conditional `NOREFEMIT` branch exists. | Investigate the ordinary profile first; changing compilation strategy is separate work. |
| [QueryCompiler.GetInvoker](../IQToolkit/QueryCompiler.cs), [StrongDelegate](../IQToolkit/StrongDelegate.cs) | Reflected lookup and generic method construction. | Candidate trim/AOT diagnostics; actual compiler output must establish the findings. |
| [QueryProvider](../IQToolkit/QueryProvider.cs), [TypeHelper](../IQToolkit/TypeHelper.cs) | Constructs generic types from runtime types and activates reflected types. | Candidate metadata and native-instantiation requirements. |
| [ExpressionEvaluator](../IQToolkit/ExpressionEvaluator.cs) | The conditional alternate also uses reflected generic evaluator construction. | `NOREFEMIT` alone is not evidence of trim/AOT compatibility. |
| [DbEntityProvider.From](../IQToolkit.Data/DbEntityProvider.cs), mapping | Activates connections/providers and inspects mapped members reflectively. | Relational/provider analysis remains a later scope; core-only results cannot certify it. |

The acquired [runtime implementation](https://github.com/dotnet/dotnet/blob/95017c711e6afc1085133d440e42b4bd78155701/src/runtime/src/libraries/System.Linq.Expressions/src/System/Linq/Expressions/LambdaExpression.cs) selects IL compilation when dynamic code is available and otherwise creates an interpreter delegate. Microsoft's NativeAOT documentation likewise states that expression trees use interpretation. Therefore, the presence of `Compile()` alone does not prove incompatibility. [NativeAOT deployment](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/)

`MethodInfo.MakeGenericMethod` carries trimming and dynamic-code requirements in the documented API contract. Its acquired CoreCLR implementation also declares trimming requirements. That identifies risk, not a measured failure for every instantiation. [MethodInfo.MakeGenericMethod](https://learn.microsoft.com/en-us/dotnet/api/system.reflection.methodinfo.makegenericmethod?view=net-10.0)

The installed SDK selects `Microsoft.NET.ILLink.Tasks` 10.0.12 for `net10.0`. Its publish targets request that pack when either `EnableTrimAnalyzer` or `EnableAotAnalyzer` is enabled. Exact NuGet catalog bodies confirm the version and an empty `net10.0` dependency group; archive authenticity, actual restore graphs and analyzer loading are now verified in the private core profile described below. Analysis can be enabled without declaring a library trimmable. [Prepare libraries for trimming](https://learn.microsoft.com/en-us/dotnet/core/deploying/trimming/prepare-libraries-for-trimming)

## AP0.1: isolated core analyzer probe

**Question:** Does an unchanged ordinary core compilation pass while strict trim/AOT analysis identifies source-bound compatibility risks?

**Scope:** Root owns ignored `artifacts/aot-feasibility/`, the owned private SDK parent, this document, plan/state and all runtime operations. Workers review read-only. Copy exactly the 28 current core C# files (298,513 bytes), their project, shared build properties/targets, CPM configuration, `.editorconfig`, SDK and NuGet settings at their original relative hierarchy. Exclude ordinary `bin`/`obj` and deleted settings files. Use independent control and analysis directories, caches, locks and outputs. Do not edit copied C# source or production configuration. Preserve the existing conditional analyzer/dead-code settings.

**Profiles:** Both are Release/`net10.0`, CI enabled, warnings as errors, no shared compiler/build servers and unchanged `NOREFEMIT` disposition. Ordinary control retains default analyzer settings. Analysis sets only `EnableTrimAnalyzer=true` and `EnableAotAnalyzer=true`; do not set `IsTrimmable`, `IsAotCompatible`, `PublishTrimmed` or `PublishAot`. No RID/runtime/native compiler pack is requested for this compile-only scope.

**Sequence:** Save/review exact source, controller, commands and allocation before SDK dispatch. Deliberately generate each new private profile's lock graph once; this is new-profile preparation, not fallback from failed locked restore. Review generated graphs before subsequent locked restores and forced Release rebuilds. Control rebuild must pass with zero warnings/errors. Analysis rebuild must retain genuine trim and AOT diagnostics as failures under the same strict warning policy. Require compiler SARIF and invocation evidence of the exact ILLink analyzer binaries; a nonzero build exit alone is insufficient.

**Acceptance:** At least one genuine IL2xxx and one IL3xxx diagnostic from the unchanged core source, with exact file/line/column/message identities. Every analysis error must belong to those declared families. Deduplicate build-summary repetitions using native compiler/SARIF records. Ordinary compile errors, audit/restore failures, analyzer load errors, AD0001/CS8032, missing analyzer evidence, incremental no-op, timeout/cancellation or preservation failure leave the probe unqualified. If actual analysis contradicts the predicted rejection, retain that observation and re-review the hypothesis; never manufacture diagnostics.

**Bounds and disposition:** Ordinary SDK commands at most 120 seconds, whole owned probe at most 900 seconds, 16 MiB per log/input, 2 GiB complete owned evidence/cache/scratch. Terminal preservation and retirement each at most 30 seconds. Retain all source/config/lock/assets/compiler evidence and exact native exits. Source/index and all 336 ordinary DLL/PDB identities must remain unchanged. Archive private active source/project/runtime names and retire the original owned parent after all command sessions stop. Three evidence-driven executable repair attempts maximum for this independent probe; no retry of V1aj or reset of any earlier budget.

**Data and authority:** Compiler input is existing source, not generated provider/test data. No database, service, credential, authored diagnostic, suppression, new public API, production dependency, package publication or performance/compatibility claim is introduced. Full publish/native execution and relational/provider coverage require separately expanded profiles and actual evidence; selecting a supported architecture remains an owner decision.

## GOTCHA and verification limits

**Goal:** Establish a bounded, source-grounded feasibility result for the required trimming/NativeAOT discovery item. **Objectives:** Distinguish interpreter fallback from reflection/instantiation hazards; observe genuine analyzer activation and diagnostics on unchanged code. **Trigger:** No trim/AOT probe or support profile exists in current configuration. **Tasks:** Exact source acquisition → source/packet review → private profile preparation and graph review → ordinary/analyzed rebuilds → preservation, ATLAS and independent observed review. **Capabilities:** Existing SDK and official ILLink pack; local isolated compilation. **Health:** The bounds above, no warning relaxation, no untouched-project writes. **Constraints:** Core-only, ordinary compilation symbols, Linux ARM64 host; no runtime/publish support inference. **Users:** Maintainers deciding whether a future AOT architecture is practical. **Intent:** Resolve an empirical roadmap question without weakening existing gates.

Two acquisition attempts used the wrong runtime source filename and received HTTP 404. Both originals remain retained; the initial failure-attribution receipt also remains, with its explicit correction. Official directory discovery identified `RuntimeMethodInfo.CoreCLR.cs`, and the third bounded acquisition completed. The registration response is gzip-encoded and retained as original bytes. The linked catalog entry uses identity encoding; its original bytes and separately normalized JSON/provenance record are also retained. These are source-acquisition mechanics, not native compatibility results.

Source inspection and the private core analyzer experiment are complete in scope. Ordinary rebuild passes with zero warnings/errors; strict analyzed rebuild rejects 45 unique source-bound errors (25 trimming / 20 AOT), corroborated by SARIF and actual analyzer activation. Both profiles have the same 29 informational notes. [Observed ATLAS](modernization-aot-feasibility-hardness.md) retains exact identities, two failed runner attempts, final attempt3of3, independent Oracle CLEAR and preservation limits. Full publishing, trimming correctness, native query execution, relational/provider analysis, performance and any supported platform claim remain unverified. The selected technical standard is `ENG-STD-DOTNET-XPLAT` 1.1.0-draft.1; its restore-profile, warning and deployment rules govern this probe. The development/autonomous guides govern packet ownership, reviewed execution and retained failure evidence.
