---
title: Canonical mapping and table publication hardness report
version: 0.1
last_updated: 2026-10-04
status: Observed scoped checkpoint
owner: IQToolkit project owner
tags: [modernization, mapping, cache, concurrency, atlas]
---

# Hardness report: canonical mapping and table publication

Date: 2026-10-04. Scope: roadmap M0, tasks V1ag.C1–C4 and V1ag.1. Technical standard: the owner-selected [ENG-STD-DOTNET-XPLAT 1.1.0-draft.1](standards/source/Cross-Platform-DotNet-Engineering-Standard-1.1.0-draft.1.md), especially DOTNET-TEST-001. Workflow: Development Plan Creation Guide 2.7 and the autonomous execution contract.

`AttributeMapping.GetEntity` and `EntityProvider.GetTable` now return the objects retained by their concurrent dictionaries. Previously, competing creation could return a losing object while subsequent lookup returned the winner. The repair assigns the existing `GetOrAdd` return values at [AttributeMapping.cs](../IQToolkit.Data/Mapping/AttributeMapping.cs) and [EntityProvider.cs](../IQToolkit.Data/EntityProvider.cs). Public signatures and factory construction policy remain intact. The exact source delta includes final EOF newlines; [the repair record](../artifacts/canonical-publication/production-repair.json) retains both originals.

## Edge cases tested

[Four tests](../IQToolkitTest/RealProvider/CanonicalPublicationTests.cs) use unchanged existing CLR mapping attributes and genuine SQLite compile-option metadata. Test-only subclasses pause the first actual base factory result while another call publishes. Both actors settle before SQL executes. Each callback preserves the actual base result; no rows, attributes, tables or errors are fabricated.

RED produced two intended reference-identity failures and two passing sequential controls. GREEN passed all four unchanged tests. Both observations independently acquired 51 native rows and verified all 10 sequential projections. The tests record schema/version/source, callback counts, call windows, thread IDs, actual object identities and native results. Missing prerequisites, timeouts or unsettled actors cannot qualify as semantic RED. See the [qualified RED](../artifacts/canonical-publication/red.wptrw9ln/observed-oracle.json) and [GREEN](../artifacts/canonical-publication/green.zk1rnuil/observed-oracle.json).

## Tool and service failure handling

Three evidence-driven harness corrections retain their original failed receipts:

- RED's outer binary allowlist omitted two newly changed Test reference assemblies. Independent review qualified the observed causal failures without SDK replay; the original outer result remains failed.
- The first API environment lacked `rg`. A separately sealed retry supplied the actual installed binary and passed; the original partial package run remains failed and its packages are not promoted.
- Consumer archival exceeded 30 seconds after native success. Separate reviewed recovery authenticated 563 retired files, 414 unchanged partial-copy files and original-parent absence. The existing native complete assertion passed. No SDK replay or before-pointer preservation is claimed; the original supervisor remains failed.

These consume the declared three artifact repairs. Historical V1k's exhausted unforced-overlap attempts are preserved separately. There is no attempt reset.

Root serialized SDK commands. Ordinary leaves were bounded to 120 seconds, coverage collection to 300 seconds, protecting parents to 900 seconds, logs to 16 MiB and newly owned storage to 2 GiB. Storage scans allowed three retries with 20 ms pauses. Forced or incomplete process settlement prevents acceptance. Coverage restored all 336 ordinary binaries immediately and preserved 219 test outputs. Qualifier and collector parents were then separately retired by authenticated same-filesystem moves within 30-second bounds, preserving 51 and 274 files with inactive mapped code/project/PE names. See [qualifier retirement](../artifacts/canonical-publication/qualify.55018kq5/parent-retirement.json) and [collector retirement](../artifacts/canonical-publication/coverage.3w2as6e8/parent-retirement.json).

## Concurrency and load considerations

The exercised guarantee is retained-winner publication at these two APIs under competing creation with immutable mapping configuration. Factories may still construct multiple objects. This evidence does not establish exactly-once construction, shared native-connection thread safety, general cache concurrency, mutable-policy isolation or throughput. Native I/O is sequential after actor settlement. Tests use bounded 10-second waits and release/cancel/join cleanup.

## Security and data exposure

The fixture source is installed engine metadata, with no credentials or application records. The repair adds no dependency, public API or network service. This packet establishes no tenant-security or all-provider certification. Generated-data, service/cost and protected-governance decisions remain unanswered.

## Verification commands and results

Exact argv, environment, source seals, native logs and process outcomes are retained in each packet; these are observed results, not commands inferred from exit status alone.

| Check | Observed result | Evidence |
| :--- | :--- | :--- |
| Focused `dotnet test`, `FullyQualifiedName~CanonicalPublicationTests` | RED 2 intended failures/2 controls; unchanged-test GREEN 4 passes | [GREEN packet](../artifacts/canonical-publication/green.zk1rnuil/packet.json) |
| `eng/verify.sh` | 431 definitions, 106 passes, 325 explicit SQLServer skips, 247 translations; 0 warnings/errors | [Strict review](../artifacts/canonical-publication/strict.4aqisdfi/observed-oracle.json), native `run.Rj3XBXVk` |
| `eng/verify-api.sh` | Six comparisons against authentic 1.1.1 source-built baselines; 12 archive payloads match current DLL/PDBs | [API review](../artifacts/canonical-publication/api.uvs17p1m/observed-oracle.json), native `run.DQp6QH3w` |
| `eng/verify-symbols.sh` | Six package/symbol pairs pass | [Symbols review](../artifacts/canonical-publication/symbols.no32fdfc/observed-oracle.json), native `run.DnxRUSSa` |
| `eng/verify-ci-consumers.sh` and separate complete assertion | Six independent package compiles, seven shadow graph reviews and genuine SQLite runtime pass; failed outer archival separately recovered | [Consumer review](../artifacts/canonical-publication/consumers.pz26t5_u/observed-oracle.json), native `run.OciNd597` |
| Unchanged collector plus private transition comparison | 97 sources / 16,724 inherited leaves and all branch denominators/condition identities preserved without regression; 427 inherited cases plus four additions | [Collection](../artifacts/canonical-publication/coverage.3w2as6e8/observed-oracle.json), [comparison](../artifacts/canonical-publication/qualify.55018kq5/observed-oracle.json) |

The comparator uses six fixed historical input aliases and two exact source recipes, retaining line numbering. Current source/PDB/test identities are independently checked. All generic 2,048-input / 128 MiB retention / 16 MiB per-input bounds and the inherited comparison loop are preserved. Actual qualification retains 195 inputs / 27,546,690 bytes. It records 2,749 / 8,353 covered lines, one gained line, and unchanged 1,480 / 5,320 branches. The durable CoverageVerifier is unchanged; no baseline or coverage-floor waiver is introduced.

## Summary and follow-ups

The local canonical-return repair and protecting checks pass in the observed scope. Original harness failures remain recorded. Independent integrated review is recorded in [integrated-oracle.json](../artifacts/canonical-publication/integrated-oracle.json), separate from checked document hashes.

M0 and the full roadmap remain open. Broad coverage floors, missing provider/platform evidence, optimized sensitivity, wider ownership and owner decisions remain unresolved. Consumer graph changes were validated in owned shadows; checked consumer locks and feed pin are not refreshed here. Previous current-final classification and saved V1af verifier are historical after these production changes; this packet does not execute or adopt a new final classifier. The nullable-column investigation remains prospective, with no runtime failure or repair observed. No publication is inferred.
