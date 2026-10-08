---
title: IQToolkit .NET standard adoption register
version: 0.1.0
last_updated: 2026-10-02
status: Partial adoption
owner: IQToolkit project owner
tags: [standards, dotnet, modernization]
---

# IQToolkit .NET standard adoption register

This register implements the documentation portion of Task V0.9a in the [modernization implementation plan](../modernization-implementation-plan.md). The [rule catalog](dotnet-rules.json) records all 43 declarations from the owner-selected `ENG-STD-DOTNET-XPLAT`, version `1.1.0-draft.1`. It records inspected controls and unmet obligations; the project does not claim whole-project compliance.

The selected source is `/home/firestrand/Downloads/Cross-Platform-DotNet-Engineering-Standard-1.1.0-draft.1.md`. A [byte-identical review snapshot](source/Cross-Platform-DotNet-Engineering-Standard-1.1.0-draft.1.md) makes catalog inspection possible from the repository without relying on that local path, with SHA-256 `8c56930ec1cd26e8b58a273d47cb0b9713f55e6d4961b6d29a636ffaf4f0ebe2`. The snapshot is preserved without reformatting or normative edits; update it only when the owner selects a replacement revision. Its illustrative override examples are source documentation, not live registry records. The owner explicitly selected this revision on 2026-10-02. That resolves the project's convention choice; corporate publication or effective-date approval is not asserted or requested by this register.

## Catalog interpretation

Each record contains the canonical ID, exact source title, source lines, parent group, owner, declaration status, named clauses, required enforcement, adoption verdict, evidence and next action. Group source ranges extend to the next group or top-level section. Individually declared rules retain precedence within their parent group: BASE-001/002/003 belong to BASE-004, and API-001/002 belong to API-003. The source declares one named clause, `platform-analyzer-protection`, under PLAT-002.

`status: declared` means the rule is present in the selected source. It does not mean an enforcement gate exists. Names beginning with `gate:` or `test:` describe required controls; their implementation and evidence must be established separately. Evidence references identify actual source, configuration or retained local verification artifacts, with the scope stated in each record. Ignored local artifacts are historical observations and will not be available in a fresh checkout without rerunning their documented producer.

Verdicts follow the standards skill's audit vocabulary: PASS, PARTIAL, FAIL, N/A and UNVERIFIED. PASS is limited to the inspected obligation at this checkpoint. N/A explains a current boundary rather than deleting a roadmap requirement. A legacy violation remains a violation; a disposition or reviewer verdict never approves an exception.

## Existing controls and evidence

The [foundation hardness report](../modernization-foundation-hardness.md) records actual builds, tests and package checks. The verified foundation includes:

- Separate SDK and TFM inputs, stable SDK roll-forward, strict warnings, deterministic builds and portable symbols.
- Basic editor preferences for UTF-8/LF and C# conventions, with explicit platform-analyzer warnings. Full CI formatting/security/naming enforcement remains incomplete.
- Central versions, actual app/test and engineering restore locks, public feed mapping, a project package cache, and high/critical transitive dependency auditing.
- Explicit CI build properties, real rejection of warning-relaxation configurations, and the unused-private-member analyzer in the ordinary gate.
- Six inspected library/symbol pairs, static API comparisons against the pinned release, preserved binary identity and an unreleased preview package version.
- Recorded local and CI command profiles, visible integration skips, source/package provenance, and a measured coverage baseline with durable binary-restoration manifests.

These observations do not establish protected hosted enforcement, production artifact promotion, runtime package-consumer behavior or native provider certification. The four OS/architecture jobs are declarations awaiting execution evidence.

## Findings and remaining delivery work

| Priority / rules | Inspected finding | Required roadmap work |
| :--- | :--- | :--- |
| Disclosure and diagnostics: SEC-002, OBS-001 | [LogParameters](../../IQToolkit.Data/DbEntityProvider.cs) writes parameter values when a log sink is supplied; diagnostics are preformatted TextWriter output. No classification/redaction control or structured event contract is present. This is source evidence; no secret or fabricated leak was generated. | M0/M1: regression-led redaction defaults, explicit sensitive-diagnostic opt-in, safe event/property contracts and optional logging integration. Preserve the existing synchronous surface. |
| Machine formatting: GLOB-001 | [SqlFormatter](../../IQToolkit.Data/Common/Language/SqlFormatter.cs) now uses an explicit invariant provider in the shared Single/Double conversion. Actual native Double tests reproduce two comma-culture column-shape failures before repair and pass five unchanged cases afterward, including both public formatter entry points and invariant controls. Real relational Min also reproduces a Turkish-culture native unknown-function failure before the default aggregate keyword normalization changes to ToUpperInvariant; the same four aggregate cases then pass, including invariant Min and Turkish Max controls. Single, wider numeric/aggregate/dialect/data-collation behavior, identifier/type and mapping conversions remain unqualified. | See the bounded [culture-literal](../modernization-sql-double-culture-hardness.md) and [relational aggregate](../modernization-sql-aggregate-culture-hardness.md) reports. Continue qualifying remaining invariant/ordinal machine boundaries with approved real inputs or named generated-input authorization. Preserve intentionally culture-sensitive LINQ and user-data conversion behavior. |
| Test evidence: TEST-001 | Existing suites and static package checks pass, but nonempty-discovery enforcement, runtime consumers, cache/array/resource regressions and the extended final gate are incomplete. Measured coverage is limited to four loaded libraries. | V0.2/V0.6/V0.7 and M0: resolve exact fixture/coverage decisions and implement meaningful tests and sensitivity checks. Native engine evidence remains separate. |
| Governance: GOV-001/003/004 | Catalog metadata exists; protected catalog/override enforcement, trusted independent approval, fresh issue evidence and expiration checks do not. No exception registry record is approved by this work. | V0.9b: expand ordered test/implementation packets, obtain scope-bound fixture authorization and qualify trusted review/CI controls. Source retrieval, scanning and release acceptance remain in the final inventory. |
| Portability: BASE-006, PLAT-001/002/003, FS-001 | Managed builds and local SQLite execution cover Linux ARM64. Hosted native jobs, Windows Access selection, native assets, private-file creation and baseline newline/casing contracts need broader qualification. | Run the declared native matrix and actual provider cases; document support exclusions, trusted paths and caller ownership. Package assets alone do not certify native behavior. |
| Execution and hosting: CFG-001, OBS-002, TIME-001 | Legacy settings remain; typed options, tracing/metrics and date/offset round-trip contracts are incomplete. | Preserve optional adapters and provider boundaries in M1/M1q; select and prove complete behavior rather than adding hosting dependencies to core. |
| Servicing and engineering: BASE-001/004, DEP-001/002/003/004, REPO-004, GOV-002 | Current controls are inspected, but automated updates, full style tooling, protected feed/cache policy and clean commit-specific source retrieval remain incomplete. | Add scoped controls and retain release evidence before compliance/promotion claims. Legacy adoption does not authorize disabling diagnostics or inventing exceptions. |
| Performance and evidence lifecycle: PERF-001, FS-002 | Timed routines are not a current workload regression gate. Engineering workspaces retain evidence without an established local size/lifetime policy. | Select an authorized workload and reproducible measurements; define bounded evidence retention and safe cleanup. Do not infer improvements from source or erase user-owned evidence. |

## Conditional boundaries

The active libraries do not launch .NET child processes, ship a production application container, or implement named-zone scheduling. Those specific PROC-001, CTR-001 and TIME-001 clauses are currently outside their runtime boundary. M1 instrumentation remains required even though the toolkit is not itself a distributed service. SQLite driver file creation still requires ownership/permission qualification; the inherited missing-file test intentionally opens with ReadWrite and creates no database.

This existing repository is not required to migrate its legacy solution merely because REPO-001 requires `.slnx` for new repositories. Public namespace, test-project naming and nullable/style changes require scoped adoption and compatibility review; they are not a reason for an unrelated repository-wide rewrite.

## Approval and verification boundaries

The current generated-fixture request covers V1.1b, V1.3 and V1.5 only and remains unanswered. Governance, culture, resource, async and later provider tests require named authorization amendments or approved real-source evidence before consuming controlled generated inputs. The legacy coverage policy also remains an unresolved human decision. No pending question is treated as an approval.

GOV-004 requires independent approval of a narrow exception, protected owner review, trusted issue status and expiration/scope validation. This catalog and register contain no `approved_by` record, waiver or fabricated approval identity. Oracle review verifies document/source fidelity and does not substitute for those controls.

Catalog validation for this checkpoint parses the actual selected source and JSON, verifies the byte hash, all 43 definitions/titles/ranges, parent precedence, the sole named clause and evidence paths. The saved output belongs under `artifacts/standard-adoption/`. This is a completed inspection of actual artifacts, not a persistent CI gate or proof that its rejection boundary cases have been tested. V0.9b remains open until that stronger contract is implemented and qualified.
