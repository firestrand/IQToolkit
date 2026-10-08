---
title: Current MySQL provider against MariaDB feasibility probe
version: 0.5
last_updated: 2026-10-05
status: Readiness and source investigation; no engine or SDK execution
owner: IQToolkit project owner
tags: [modernization, mariadb, mysql, compatibility, probe]
---

# Current MySQL provider against MariaDB feasibility probe

The roadmap requires verifying a named MariaDB version against the existing MySQL provider before proposing another package. This probe preserves `IQToolkit.Data.MySqlClient`, its public contract and MySql.Data26.7.0. Shared protocol and a compilable driver do not establish SQL/type/key/resource compatibility. This document is the GOTCHA scope for readiness and source acquisition; a fully expanded and independently accepted plan/packet must precede native or SDK execution.

## GOTCHA goals and measurable objectives

Resolve which supported MariaDB release/distribution can be acquired authentically on Linux ARM64 and which current provider behavior can be tested with genuine engine metadata. Bind the existing provider/driver/profile/license/source identities, record available local prerequisites, identify actual transport/authentication and catalog/type/parameter/translation/resource questions, and preserve every original acquisition failure. Success at this stage is actionable authenticated readiness/source evidence, not a live compatibility or provider-support result.

## Tasks, trigger and termination

The accepted vector compiler/document checkpoint leaves required MariaDB discovery open. Fresh local readiness and official version/installation investigation may run read-only in parallel; root integrates them and owns metadata acquisition, source inspections, scoped controls and plan/state updates. Terminate readiness after a reproducible exact input inventory and a candidate-specific audit/review, or a preserved unavailable prerequisite. A future live packet must declare exact commands, engine/data/client/profile/resource ownership, finite inputs and bounds before dispatch. No source suspicion is promoted to an observed runtime defect.

## Capabilities and off-limits actions

Current capabilities: read local code/metadata, official HTTPS documentation/catalog/checksum/source responses, preserve inactive source/package bytes, and write owned documentation and ignored evidence. No SDK/build/restore, installing/running native binaries, server/client calls, application/provider data/DDL/mutations, production dependencies/public APIs, new provider package, credential acquisition, paid service, publication or exhausted-packet retry is authorized by this readiness stage. Available tooling does not expand the stage. Any later engine/compiler/runtime operation has a separate accepted task and concrete packet gate.

## Health, bounds and monitoring

Official metadata/source responses use a 30-second socket timeout, a 16 MiB response limit and cumulative deadline checks between reads at 240 seconds per stage, with at most 64 HTTPS transport attempts, including redirects, and eight followed redirect hops per stage in the revised tools. Intermediate redirect bodies are closed without draining. These checks are cooperative; they do not establish a hard wall-clock watchdog. Reuse authentic retained installed package bytes where useful; acquire a fresh archive only when its identity/size/origin policy is settled. No installer/server archive is executed. Each distinct failed acquisition permits at most three evidence-driven corrections, retaining original outcomes; no repeat unchanged failure. The owned readiness/source area is `artifacts/mariadb-discovery`, bounded to 512 MiB before a later measured native expansion. Hash/length/path/response records and real tool exits identify outcomes. No user token/cost budget was supplied; all current network reads are public/free.

## Attributes and constraints

Root is sole writer of plan/state/probe/controls and Git index/SDK operations. Workers write only their allocated handoffs, have no recursive delegation, and remain read-only for subjects. Maximum four active participants includes root. Preserve the entry source/index/336 ordinary binary hashes,34 historical fact bytes/95-method registration,111 Proposed behavior/16 process records/12 deferred phase contracts and all previous failed/closed/exhausted native evidence. No previous native cap, current-final profile0.110, BLOCKED record or unanswered owner/data/coverage/public/cache decision is reset.

The selected Cross-Platform C#/.NET Engineering Standard1.1.0-draft.1 and its scoped legacy adoption govern .NET work; Development Plan Guide2.7/autonomous, Technical Markdown and agent-tooling govern this deliverable. Any new ignored stdlib command tools use Python2026-09-29 on the observed local3.12.3 interpreter, without an unrelated Python application/package/toolchain migration. Graphify/Context7 are unavailable; use live Serena where it functions and rg/official version-matched source otherwise. Whole standards compliance is unclaimed.

## Users, runtime and decision loop

Maintainers need evidence before advertising MariaDB support or changing the existing provider. Working state is local inspected code and public metadata; persisted state is exact inactive bytes, provenance and bounded handoffs. A version or source identity mismatch, missing engine/build prerequisite, inaccessible installation route, incompatible authentication boundary or unresolved data requirement triggers recorded replanning. Missing access/data does not permit fabricated fixtures, guessed APIs or implicit approval. The discovery may identify a real-data acquisition route; it cannot certify it before execution.

## Beliefs and intent at entry

Verified locally: the provider consumes MySql.Data26.7.0; its retained assets select net10.0 and its nuspec lists GPL-2.0-only WITH Universal-FOSS-exception-1.0 but no source repository URL/commit. No mariadb/mariadbd/mysql/mysqld executable is available from PATH. Current provider source has MySQL-specific parameter mappings, LAST_INSERT_ID/ROW_COUNT expressions, buffered reader policy and its own formatter; each needs scoped actual-engine qualification rather than assumed parity. Official release/distribution/source claims and licensing/build readiness remain acquisition outputs.

Intent: test the existing adapter first. A later experiment should acquire real catalog/native observations, then separately qualify the unchanged typed driver and public provider queries, parameters and resource ownership against independent engine results. CRUD/generated-key/type/transaction support needs its own accepted real-data contract; source-only or read-only catalog evidence cannot close those claims. No public API or package selection is adopted by this document.

## Observed source and readiness outcome

The selected .NET standard is [Cross-Platform C#/.NET Engineering Standard 1.1.0-draft.1](/home/firestrand/Downloads/Cross-Platform-DotNet-Engineering-Standard-1.1.0-draft.1.md), SHA256 `8c56930ec1cd26e8b58a273d47cb0b9713f55e6d4961b6d29a636ffaf4f0ebe2`. This stage changes documentation and ignored local acquisition tools only. DOTNET-GOV-005 scopes adoption; DOTNET-BASE-006 separates one observed native host from product certification, and DOTNET-SEC-002 governs diagnostic disclosure. No new .NET project, package, runtime feature or compliance exception is introduced.

The [current source profile](../artifacts/mariadb-discovery/current-source-profile.json) records these observations:

| Subject | Observed identity | Limit |
| :--- | :--- | :--- |
| Existing driver | MySql.Data 26.7.0; retained archive size 4,925,030 bytes and raw SHA512 match the acquired NuGet catalog. | Published archive identity is established; repository metadata omits a URL/commit, and the exact `26.7.0` Git tag request returned 404. Source commit remains unavailable. NuGet lock contentHash is a distinct hash domain. |
| Candidate engine | MariaDB 12.3.3; annotated tag resolves to commit `83e909fc2a0dbc394b4b683fb3fa2d7dcf26cc5e`. | GitHub reports signature verification false, reason `unknown_key`. This is an investigation pin, not an owner-selected support promise. |
| ARM64 distribution | Ubuntu Noble repository Packages file: 67,933 bytes, SHA256 `410862bbb5b4582ad0cdf9a61dd302a14d3b4b6400b7c4758248d651ca313238`; both published SHA256 and SHA512 match. Six driver/client/server/common package identities and sizes are retained. | HTTPS acquisition and digest matching establish consistency with the published checksum lists. No independent release signing-key trust validation or binary/ELF qualification has occurred. |
| Runtime prerequisites | Installed package inventory shows the server's declared runtime libraries except `liburing2`; build headers are separately absent. | Package metadata is insufficient to prove an executable's actual shared-library closure or startup behavior. |
| Acquisition route | Individually checksummed `.deb` files have concrete filenames and sizes in the verified index. | The acquired repository checksum lists do not include the larger bundle's digest. The bundle is not qualified; no package download, extraction, installation or engine execution occurred. |

The two independent readiness/version tasks completed with [actual harness provenance](../artifacts/mariadb-discovery/readiness-handoff-provenance.json). Root then made 19 top-level public acquisitions across four stages. Historical receipts do not measure the redirect-inclusive wire request count. The first stage retained four successful responses and three failures: two checksum redirects rejected by the initial origin policy and one missing driver tag. A separate original local gzip decoding failure is retained. Observing the redirect origin, restricting subsequent requests to exact public CDN paths, and decoding the actual compressed registration response resolved the checksum/catalog acquisition gaps without relabeling the failures. The [metadata readback](../artifacts/mariadb-discovery/metadata-readback.json) verifies all 19 retained local captures against their recorded size and hash. The two rejected redirects produced zero-byte local captures before response-body acquisition; remote body lengths were not observed.

Transient CDN signed query strings were initially persisted in five Stage 2 receipts. They were removed; the public paths, original URL digests, original receipt digests and actual HTTP classifications remain in the [redaction record](../artifacts/mariadb-discovery/metadata-location-redaction.json). Full original receipt bytes are intentionally not retained. All 86 original review inputs are preserved in a hash-addressed source epoch. The follow-up tool's executed source is also preserved separately from its first redaction repair. Subsequent revisions add bounded redirect closure, stage-wide transport counting, sanitized URL/error fields and per-attempt failure captures, including controlled HTTP protocol exceptions and bounded `IncompleteRead.partial` bytes; their checks are separate from the historical HTTP executions. Acquisition tools are inactive local scripts, not a reusable or independently qualified native controller.

The [preservation readback](../artifacts/mariadb-discovery/preservation-readback.json) confirms that the 14 entry subjects, all 336 ordinary DLL/PDB files and the Git index are unchanged. The accepted implementation-plan 0.122 checkpoint is historical and remains unchanged; this source-stage document does not supersede its acceptance or authorize new SDK/native work.

The gzip/JSON decode boundary classifies truncated/corrupt gzip (`EOFError`, `zlib.error`) and excessive JSON recursion as controlled decode failures, preserving the acquired raw body and stage outcome. These paths are source-inspected; malformed fixtures or HTTP failures were not generated for this stage.

## Next experiment boundary

The next prerequisite is an expanded, independently accepted acquisition/extraction packet covering the six exact MariaDB package identities and four Ubuntu dependency packages. The [independent dependency handoff](../artifacts/mariadb-discovery/dependency-handoff.json) checks 179 declared dependency constraints and identifies `liburing2`, `libpcre2-posix3`, `mysql-common` and `libconfig-inifiles-perl`: 73,558 bytes, or 14,237,418 bytes with the six MariaDB archives. This is declared package metadata closure, not an ELF/ABI or bootstrap result. The [signature check](../artifacts/mariadb-discovery/ubuntu-signature-verification.json) verifies the three cached Ubuntu InRelease signatures against the installed Ubuntu archive keyring; keyring distribution was not independently verified and index freshness is not a production patch certification. It must define exact origins, byte limits, downloaded archive hashes, safe extraction rules and allowed destinations, native-library inspection, owned storage/cleanup and failure handling. Extract package payloads privately; do not run maintainer scripts, install system packages, change services or require privileged Docker access. Missing runtime dependencies pause startup work rather than prompting a system installation.

A separate live packet must follow qualified binary/dependency evidence. It must bind literal SDK/native commands, current strict package/profile/audit inputs, hard process and output/storage bounds, per-attempt and cumulative limits, exclusive root SDK ownership and independently reviewed cleanup. Neither unused reservations nor stopped packets from earlier probes grant another attempt. No command in this document is an executable dispatch authorization.

The first live comparison should use genuine engine metadata: acquire version/configuration and `INFORMATION_SCHEMA.COLUMNS` observations through native commands, then compare typed MySql.Data parameters and unchanged IQToolkit query translation/execution against those observations. Use the actual `ICreateExecutor` provider boundary to preserve specialized parameter handling. Compare filters, ordering, projections and bounded pagination, caller-open versus provider-owned connections and buffered reader disposal. Record authentication, collation, SQL/type/resource differences as scoped results. Existing source concerns are hypotheses until observed against the engine.

Read-only catalog observations cannot qualify CRUD, generated keys, affected-row counts, writable transactions or the full type matrix. Those behaviors remain under `PROBE.MARIADB.v1` in the [future obligation ledger](modernization-future-facts.md), with approved real writable data and separate expanded tasks required. No Tier-1 behavior fact or existing evidence binding is activated by this stage. Full-roadmap completion, integration verification and release remain open.

## Revision history

| Version | Change |
| :--- | :--- |
| 0.1 | Define source/readiness GOTCHA before acquisition. |
| 0.2 | Record actual package/tag/index identity, preserved failures and redaction, missing runtime dependency and the next experiment boundary. |
| 0.3 | Correct the first Oracle findings; distinguish local captures and counting domains, bound redirect handling and include complete declared dependency closure/signature results. |
| 0.4 | Handle HTTP protocol/read exceptions at each acquisition boundary and preserve bounded available exception partial bytes without changing historical outcomes. |
| 0.5 | Preserve gzip/JSON decode failure classifications and raw-body receipts after the third Oracle review. |
