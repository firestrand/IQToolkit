---
title: CI package consumer integration hardness
version: 0.1
last_updated: 2026-10-03
status: Scoped verification; integrated review CLEAR
owner: IQToolkit project owner
tags: [modernization, packaging, ci, verification]
---

# ATLAS hardness report: CI package consumers

V1q brings existing isolated package consumers into Linux CI. It does not change dependencies, reviewed locks, templates, runtime libraries or public APIs. The full modernization goal and M0 gates remain open.

## Contract and implementation

[verify-ci-consumers.sh](../eng/verify-ci-consumers.sh) copies exact sources and six fresh unsigned packages into an outside-checkout shadow. It explicitly prepares seven locks there, checks every delta before any locked consumer build/execution, and retains the shadow plus original input identities on success or failure. Unique HTTP/plugin caches supplement the existing unique SQLite package caches. No prepared lock or feed manifest is copied back into the checkout.

[Verifier](../eng/IndependentConsumers/Verifier/Program.cs)'s `lock-delta` operation compares complete JSON trees, ignoring object-property order. Only `dependencies/net10.0/<known-toolkit-id>/contentHash` may differ; each prepared toolkit hash must equal SHA512 of its exact unsigned feed archive with matching nuspec ID/version. Frameworks, properties, nodes, requested/resolved versions, dependency edges and external logical hashes remain identical. Duplicate properties reject. External signed packages retain their NuGet logical hash; it is not replaced by their whole-archive hash.

[CI](../.github/workflows/ci.yml) invokes this repository command after fresh package inspection on Linux runners. Evidence upload includes source snapshots, reviewed/prepared locks, tested packages, statuses and provenance logs, excluding package caches. Existing Windows/macOS gates remain; no hosted-run claim follows from local execution.

## Edge cases tested

- `artifacts/ci-consumers/runs/run.tfpmYPiE`: first semantic GREEN. All seven lock comparisons, six cold locked public-symbol compilations and the native SQLite executor pass. Preparation and verification build the framework-only tooling with zero warnings/errors.
- Actual core-only reviewed lock versus that run's actual prepared SQLite lock: comparator exits 1 with `Lock properties changed at dependencies/net10.0.` Both inputs are actual artifacts, without fabricated mutations; this proves sensitivity to that graph mismatch, not every possible property change.
- `artifacts/ci-consumers/runs/run.jKoOtUFj`: actual pinned release feed rejected for containing seven archives instead of the six expected current packages. Exit 1 and failed status retained. This exercises early prerequisite rejection and the EXIT path, not SDK/lock-review error injection.
- Original source/lock/manifest/feed snapshots equal before/after on both wrapper runs; the failed run does not replace the successful-run pointer.

## Tool and service failure handling

Strict shell pipe failure propagation stops on preparation, comparator, locked restore, build or runtime failure. There is no unlocked retry branch. The EXIT handler checks original input identity and retains failure artifacts. Owned generated projects retain exact bytes under `.csproj.snapshot` before entering the checkout; child runners already do this and the outer handler covers remaining owned projects. Network outage, interrupted process and cleanup I/O faults have not been injected or qualified.

## Concurrency and load

Root serialized SDK work. Each wrapper run owns a unique outside shadow and artifact directory; each consumer owns its own cold package cache. Concurrent wrapper scheduling and hosted matrix duration remain unmeasured. The wrapper does not change provider concurrency semantics. CI currently has a twenty-minute job timeout; actual hosted duration still needs observation.

## Security

No new feeds, secrets, runtime dependencies or public payloads. Existing explicit local/public mapping, exact archive provenance, native metadata and source checks remain active. The baseline graph must already be reviewed; accepting a bound local hash does not approve a dependency update. Logs contain public package identities/hashes and unchanged engine metadata. No application data is introduced.

## Verification and remaining gates

`bash -n eng/verify-ci-consumers.sh` passes. Fresh `dotnet pack IQToolkit.sln -c Release --no-build --no-restore -p:ContinuousIntegrationBuild=true -warnaserror -o artifacts/ci-consumers-input/packages` and `dotnet msbuild eng/VerifyPackages.proj -nologo -p:PackageDirectory=artifacts/ci-consumers-input/packages` pass all six library/symbol contracts. `bash eng/verify-ci-consumers.sh artifacts/ci-consumers-input/packages` passes all seven consumers. The two real-artifact rejection commands above return the expected nonzero status.

Ordinary protecting gate `artifacts/verification/runs/run.8Pg0CECf` passes: 77 tests, 325 unconfigured SQL Server skips, 247 translations, zero warnings/errors and 259 stable in-run inputs. `artifacts/ci-consumers/inspection/integrated.json` independently checks 138 unchanged library/config inputs, 12 fresh-and-retained archive payloads against current binaries, all 336 ordinary binary hashes equal V1a, fresh consumer source/copy/lock/feed identities and both success/rejection snapshots. No new API comparison or coverage collection is needed for unchanged library/test inputs; the retained evidence remains qualified in its original scope. Final integrated Oracle review CLEAR for the V1q checkpoint; reports and the three post-gate documentation deltas were independently verified. Earlier independent-consumer runs are historical after the verifier change; the new run qualifies current tooling. Library/config/payload identity has been rechecked; the retained API comparison remains qualified through those unchanged inputs/payloads. Existing coverage measures runtime libraries only; the comparator is outside that measurement. No new comparator coverage percentage, target waiver, full M0/roadmap completion or other backend/OS support is claimed.
