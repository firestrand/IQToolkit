---
title: DuckDB private SDK readiness probe
version: 0.2
last_updated: 2026-10-03
status: SDK stages PASS; integrated Oracle-clear
owner: IQToolkit project owner
tags: [modernization, duckdb, sdk, probe]
---

# DuckDB private SDK readiness probe

Q0.3 asks whether the exact DuckDB.NET dependency graph restores and compiles on the observed Linux ARM64 host. It enables required LOCAL-RM-M5; production M5 retains M0/M1, public SQL/workload/data/security and platform acceptance. The [native checkpoint](modernization-duckdb-native-hardness.md) already observed 159 authentic settings rows, schema/nulls, controls, version and platform. This packet does not execute ADO or implement a provider.

## GOTCHA and execution contract

**Goals/objectives:** Obtain a genuine NuGet lock for a sole-direct DuckDB.NET.Data.Full1.5.6 consumer; review its exact four-node graph; then restore from a second empty cache in locked mode and build with zero warnings/errors. Verify acquired archive identity in each cache and copied Linux ARM64 native identity. Generated lock metadata is real tool output, not generated provider data.

**Tasks:** Save exact project/config/helper and sealed input snapshots → prospective source/data/code Oracle review → one lock-generation restore → observed graph/hash review → separately seal cold inputs including the actual reviewed lock → prospective cold review → one locked restore/build → observed review/disposition/ATLAS and new integrated protection. Each failed stage is terminal; no automatic retry or unlocked recovery. No consumer dispatch follows a build pass.

**Capabilities/ownership/users:** Maintainer-only Technical Probe. Root owns all writes/runtime/shared plan/state; execution_design and version_metadata inspect immutable source read-only, Oracle independently reviews. Maximum three workers, no recursive delegation. No production dependency/lock/API/code, service, account, installation or remote Git change.

**Health/constraints:** SDK version command15s; restore120s; build120s; each command includes terminal checks before acceptance, then at most5s owned-group termination/reaping. Merged stdout/stderr logs and reports each≤1MiB; DuckDB evidence area plus disposable app<2GiB, polled with terminal checks. These are rejection thresholds with possible polling overshoot, not strict allocation caps. No whole-process RSS bound is claimed. Core dumps disabled. Source hashing/traversal is outside SDK command budgets. All ordinary writers remain stopped through before/after checkout snapshots.

**Attributes/runtime/beliefs/intentions:** SDK10.0.401/runtime10.0.12, net10.0/Release/framework-dependent/no apphost, no requested RID; runtime assets are inspected for native linux-arm64 only. Exact SDK global.json disables roll-forward and prerelease selection. Only the disposable project's outputs and private caches may change. Preserve the original archive inspectionFAILED, retained metadataPASS, nativePASS and P4FAILED; historical checkers are unchanged. Disposable code is preserved as nonactive snapshots and removed after terminal ownership verification. Retained caches have an explicit cleanup decision at checkpoint closeout.

**Authority:** Owner-selected Cross-Platform .NET Standard1.1.0-draft.1 (SHA2568c56930ec1cd26e8b58a273d47cb0b9713f55e6d4961b6d29a636ffaf4f0ebe2), especially DEP-002/003/004, PROC-001, FS-001/002 and SEC-002. Development Plan Guide2.7 DP-05/DP-09 and Technical Probe requirements govern data and decisions; autonomous/agent-tooling/Technical Markdown guides govern execution and documentation. No full compliance or public acceptance claim.

## Exact inputs and trust profile

Owned0700 app `/tmp/iqtoolkit-duckdb-sdk.xx78fqjw`; evidence `artifacts/duckdb-probe/sdk.cjkgmfg9`. Files: DuckDBProbe.csproj, Program.cs, global.json, NuGet.config and three empty Directory.Build/Packages files blocking parent imports; helper sdk-probe.py. Program only compiles a typeof(DuckDBConnection) reference and an empty Main; no application binary is executed. PackageReference uses exact `[1.5.6]`, sole-direct Data.Full. Transitive graph expected from actual nuspecs: Bindings.Full1.5.6, Arrow23.0.0 and Scalars23.0.0. No authored lock/content hash or fabricated provider rows.

The existing four verified archives under acquisition.k152y57s/feed are the sole download source. Mapping names exactly these four IDs; no catch-all or public package fallback. Every cold cache must match acquired archive SHA256 plus original catalog SHA512/size. NuGet logical lock contentHash is a separate value produced by restore; archive SHA512 is not substituted for it.

The config clears feeds, fallback folders, mappings, audit sources and signers. Official NuGet audit source remains enabled, all transitive dependencies audited, warnings treated as errors. SignatureValidationMode=require trusts only the current nuget.org repository certificate SHA2561f4b311d9acc115c8dc8018b5a49e00fce6da8e2855f9f014ca6f34570bc482d with allowUntrustedRoot=false. Actual validation must occur during cold restore; signature entry presence alone proves nothing. The retained official repository-signatures body binds certificate provenance (SHA2565f45cbf2d7f8a4b57fea6e43bb603d29a0682058a71eb61d900db74860d210b0); no chain, timestamp or vulnerability outcome is claimed before execution. Network failure is a failed stage, never disabled auditing/verification. See [official NuGet configuration](https://learn.microsoft.com/en-us/nuget/reference/nuget-config-file).

The parent uses direct absolute tool argv, validated ownership/modes, controlled PATH/LANG/LC_ALL and explicit SDK home/cache/tmp environment only. No ambient HOME, credentials, proxies, plugins or shell configuration is forwarded. NuGet signature verification is explicitly enabled; NuGet scratch and temporary paths are isolated. Build servers, shared compilation and MSBuild node reuse are disabled. Commands use the same Release restore profile; build uses --no-restore. Process groups settle before acceptance; cancellation is checked immediately before launch and after each operation. Primary failure survives cleanup/terminal publication failures.

## Dispatch and acceptance

Only after sealed source review: `/usr/bin/python3 -I artifacts/duckdb-probe/sdk.cjkgmfg9/sdk-probe.py generate`. It invokes SDK version and deliberate unlocked lock generation against an empty owned cache. A graph mismatch or warning terminates this stage. Source receipt excludes nonexistent lock/output files and includes current project/helper/docs, selected standard, source provenance and four archives.

Cold dispatch is prohibited until the actual lock and graph edges/content hashes/cache provenance are independently reviewed and lock-review.json records CLEAR bound to its SHA256. Then a separate receipt includes the actual lock and review. The saved `cold` mode must use --locked-mode in a second empty cache, preserve the lock exactly and compile with -warnaserror. Output manifest binds actual managed/native bytes; no SDK availability alone establishes ADO, async, import or provider correctness.

## Remaining horizons

ADO equivalence requires separately expanded source/code/data/runtime review using unchanged actual native rows/schema/nulls/controls. Preserve quoted empty temp_directory in the raw connection string; avoid builder reserialization and descriptor enumeration. SDK build does not establish these semantics. Public D1/D4, coverage, generated fixture, support and all other roadmap gates remain open. Final scoped closeout requires authentic outputs or truthful failure, owned cleanup, ATLAS, unchanged production identities and integrated Oracle review.

## Observed checkpoint

Prospective generate review CLEAR bound 43 inputs/39 snapshots; actual generation PASS produced lock SHA2561867c8d9f292a6fe2b4c075b169ba2dd40f6e95dcc1045dcd851711af10f47f2. Independent actual graph/cache/asset review CLEAR preceded cold review of 49 inputs/45 snapshots. Cold locked restore and first Release build PASS, zero warnings/errors, unchanged lock/checkout/source and clean terminal owned groups. Observed Oracle CLEAR; no application/native/ADO run.

Both executed command shapes above are historical. Exact nine helper/project/lock source snapshots and 13 compiled outputs are retained; the owned outside app is removed and helper is now sdk-probe.py.snapshot. Generate/cold caches remain owned offline provenance until the next reviewed SDK/ADO checkpoint or goal closeout; cleanup stays within sdk.cjkgmfg9. Native/production artifacts and historical checker bytes remain unchanged. [SDK ATLAS](modernization-duckdb-sdk-hardness.md) records qualifications and final integrated review.

Actual hash inspection corrected a source-only assumption: cache .nupkg.sha512 equals the full signed archive/catalog hash, while lock/assets/cache metadata contentHash is a distinct logical hash. The first ad-hoc read-only assertion failed; corrected inspection and independent review passed. Genuine SDK stages remained PASS. Eleven additional immutable driver primitive/parameter/native-startup sources were retained and independently verified for a later ADO packet; no runtime dispatch follows from source preparation.
