---
title: Core settings dependency cleanup hardness
version: 0.1
last_updated: 2026-10-03
status: Scoped verification; integrated Oracle CLEAR
owner: IQToolkit project owner
tags: [modernization, packaging, dependencies, verification]
---

# ATLAS hardness report: unused core settings

V1t removes the empty internal core settings class, its empty `.settings` file and core's ConfigurationManager package reference. Data's public `DbEntityProvider.FromApplicationSettings()`, its Provider/Connection/Mapping keys, its explicit ConfigurationManager reference and the central dependency version are preserved. This partially advances the roadmap's legacy-settings dependency separation; it does not move the public factory or complete M0.

## Contract and dependency changes

The core-only consumer now resolves IQToolkit alone, rather than ConfigurationManager plus EventLog and ProtectedData. Applications that directly use those incidental transitive APIs must declare their own package reference. Provider consumers retain the settings dependencies through Data or their driver. No package version or public API is changed. These are unpublished local preview artifacts; no release or package publication is authorized or certified.

The durable assertion in [VerifyPackages.proj](../eng/VerifyPackages.proj) rejects a core ConfigurationManager dependency and requires Data's existing explicit dependency. It does not prohibit all possible future core dependencies.

| Actual graph | Before | After | Reviewed delta |
| :--- | :--- | :--- | :--- |
| Core-only consumer | 4 nodes | 1 node | Core settings edge and its three external nodes removed. |
| Data/provider and SQLite executor consumers | Existing node sets | Same node sets | Core settings edge removed; toolkit archive hashes refreshed. |
| Two application/test locks | 46 and 44 nodes | Same counts | Only core project settings dependency metadata removed. |

## Edge cases tested

Acquisition `artifacts/core-settings/acquire.ye432mg5` retains 31 unchanged inputs, exact old source/archive/lock copies, six actual manifest dependency records and the four-node core graph. Direct/reflection-name and friend-assembly searches find no settings callers. Oracle prospective and acquisition reviews are CLEAR.

Semantic RED `artifacts/core-settings/red.umciu50o` exits1 at `IQToolkit core must not depend on legacy ConfigurationManager.` before the source removal. GREEN `green.twdqo7vw` qualifies all six new package manifests using the same assertion source. Exact assertion bytes are retained in the RED directory. RED's runner snapshots checkout files, not the ignored feed; archive provenance is bound to acquisition-retained and independently inspected old bytes rather than an invented RED-specific archive manifest.

Framework metadata `assemblies.a2qwsul4` inspects both genuine old/new core and Data DLLs: old core settings/reference present, new core settings/reference absent; Data's reference and public static factory present in both. Archive and DLL hashes, reader source and outside framework-only environment are retained. Static API comparisons, rather than this narrow metadata reader alone, protect the complete public surface.

## Tool and service failures

Initial deliberate restore `app-lock-update.qo1v_0cw` generates new assets but retains stale core-edge application lock metadata because Data keeps the aggregate dependency closure. The lock review rejects this. The saved prospective command was corrected to explicit `--force-evaluate`, and `app-lock-update-force.tn671odf` generates the intended actual edge changes. This is deliberate dependency maintenance outside CI, not an automatic unlocked retry. No CI comparator or hash-only exception is widened.

The initial API `run.0K27Mo7E` was dispatched before successful lock review. Its comparisons pass, but it is historical after the forced lock update. Fresh API `run.e3e31qE1` requalifies the reviewed source and locks. Original attempts and snapshots remain retained. An ignored integrated inspector also initially read the outer runner pointer instead of the actual coverage directory; its source and note are retained, and a distinct collector pointer fixes the inspection without rerunning SDK work. Unexpected graph/API changes stop affected work rather than broadening allowed deltas.

## Concurrency and security

Root serializes restore/build/pack/consumer/coverage operations. Consumer verification uses outside cold caches and exact tested archive bytes; no database server, synthetic rows or shared connection is introduced. This cleanup adds no concurrency guarantee. Removed source is internal and unused; no application configuration data or credentials are recorded. Data's configuration keys and public factory are unchanged. Hosted platform and backend qualification remain unobserved.

## Verification

Actual JSON reviews `app-lock-review.json` and `consumer-lock-review.json` compare all nine locks with acquired originals. Only the intended core edge/node reduction and toolkit content hashes change; all external versions, hashes and other edges remain. Explicit preparations are `artifacts/independent-consumers/prepare-runs/run.J6b9wM8A` and `artifacts/package-consumer/lock-preparation-runs/run.jUhRXliC`. Their feed binding is retained.

Fresh API `artifacts/api-compatibility/runs/run.e3e31qE1` completes six comparisons against the pinned release, with baseline references resolved and no suppression. Package assertion GREEN checks the exact fresh six library/symbol pairs. Symbol `artifacts/package-symbols/runs/run.EWqdiA9i` validates all six identities/checksums/commit-bound Source Link records. Dirty-checkout metadata still maps to the release commit; it does not establish source retrieval or correct source bytes at that remote commit, disclosure approval or release certification.

Fresh `bash eng/verify-ci-consumers.sh <fresh-feed>` in `run.DCJrkySb` passes seven unchanged hash-only shadow reviews, six cold locked public-symbol compiles and the real SQLite executor. `GITHUB_ACTIONS=true bash eng/verify.sh` in `run.H35m0edF` passes 77 tests/325 unconfigured SQLServer skips/247 translations, zero warnings/errors and 260 stable inputs. All SDK sessions are terminal.

Fresh coverage `artifacts/core-settings/coverage.v4uhr5s3` measures 2717/8349 lines (32.54282%) and 1447/5320 branches (27.19925%). Two unused settings lines leave the denominator; covered counts are unchanged from V1a. Only core/Data/SqlClient/SQLite are loaded. All 336 actual DLL/PDB hashes restore immediately and still match the ordinary outputs. This satisfies no-regression in the measured scope; workflow coverage targets remain unmet and no waiver or exclusion is introduced.

`python3 -I artifacts/core-settings/validate-checkpoint.py` passes after the pointer correction. `integrated.json` binds 137 current library/config inputs to the fresh API snapshot, 12 archive payloads to ordinary binaries, all nine intended lock deltas, exact source/old-archive acquisition copies, same assertion RED/GREEN, four genuine assembly records, six symbol pairs and 135 document checksums to current source files, seven consumers, the strict gate and fresh restored coverage. Seven deliberate consumer-lock changes followed API comparison; all library compilation inputs remain equal. Later changes are limited to plan/state/this report. Full integrated Oracle review is CLEAR for this bounded checkpoint, including the real manifest pair, minimal source removal, nine actual lock deltas, fresh gates and restored coverage/provenance. Prior V1s unchanged-library/payload/runtime coverage qualifications are historical following this production change. Full coverage targets, supported hosted-matrix validation, fixture/policy/public async decisions and the complete roadmap remain open. No waiver or full-project compliance claim follows from this packet.
