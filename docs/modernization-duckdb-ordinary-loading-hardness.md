---
title: DuckDB ordinary native loading hardness report
version: 0.1
last_updated: 2026-10-03
status: Private ordinary-loading checkpoint integrated Oracle-clear; broader gates open
owner: IQToolkit project owner
tags: [modernization, duckdb, native-loading, atlas]
---

# DuckDB ordinary native loading hardness report

The [Q0.5 probe](modernization-duckdb-ordinary-loading-probe.md) passed ordinary SDK native resolution for the observed Linux ARM64 deployment. It loaded the reviewed package library without explicit native loading or resolver registration. The same six independently acquired native results and actual-name parameter comparison passed. This supplies finite package feasibility evidence for LOCAL-RM-M5; broader implementation and support gates remain open.

## Edge cases and loading boundary

The source removed the previous explicit resolver, module handle and load call. No consumer ALC, startup hook, preload or loader-path override was supplied. This statement describes the reviewed consumer/runner and retained source paths, not exhaustive absence from all upstream code. The fresh child saw zero DuckDB mappings before ServerVersion. Before Open, every DuckDB mapping matched the owned copied runtimes/linux-arm64/native/libduckdb.so, whose size62,945,312bytes and SHA256 `23686593dba4ec22c6e581bb1c843b3964e84c4bc4d0063d8b4daddd2e20e30a` matched the independently acquired native asset.

Actual ServerVersion v1.5.6, runtimeIdentifier linux-arm64, framework .NET10.0.12 and owned base directory were recorded. Owned deps-file and native-search-directory listing observations were true; the runtime's acceptance depended on actual mapped-file identity rather than those supplementary booleans. The SDK deps file declares the pinned ARM64 asset in this framework-dependent no-RID deployment. Other included assets do not establish other OS/architecture support, publish/trimming/AOT compatibility or universal default probing behavior. Loading and native initializers occur before database configuration; the parent watchdog covers that boundary. No manual unload or native destructor census was attempted.

Controls were acquired first and all 13 typed nonnull values matched. Actual159ordered catalog rows, three DESCRIBE rows/nine genuine NULL cells, version/platform and independent count matched exactly. Native enum1/5/17 and CLR bool/long/string observations agreed with typed getters/GetValue/IsDBNull. Order and multiplicity were preserved without authored provider payloads. The actual Calendar parameter value came from the native catalog and returned its unchanged row; independent C API parameter equivalence remains unqualified.

All seven readers recorded IsClosed after disposal, and command Dispose calls completed. Connection State and the captured borrowed SafeHandle recorded closed after disposal; the borrowed handle was not independently disposed. These managed states do not prove every native allocation was freed or error/abandonment behavior. Large-result streaming and genuine async/cancellation were not exercised.

## Tool failures, concurrency and security

The exact four-package reviewed lock SHA256 `1867c8d9f292a6fe2b4c075b169ba2dd40f6e95dcc1045dcd851711af10f47f2` and proven51-file cache were unchanged. Fresh tooling/audit locations retained enabled/all/low audit and strict warnings; reuse is not a new cold extraction or signature-validation proof. SDK10.0.401/net10.0/Release/no-apphost/no-RID and compiled runtime assets were sealed before execution. Four immutable host/PInvoke/RID sources at installed runtime commit95017c711e6afc1085133d440e42b4bd78155701 total107,257bytes; their predicted probing path is separate from actual runtime evidence.

Prospective SDK review bound68inputs/64snapshots; the subsequent actual compiled runtime review bound86inputs/71snapshots and13outputs. All four commands exited zero, with stable sealed inputs, ordinary checkout and51cache files. The runtime app added only actual.json. No cancellation, forced termination, live group members, cleanup or terminal evidence errors occurred. Root serialized SDK/runtime and writes; workers reviewed read-only.

Private owned directories, controlled absolute tools/environment, exclusive bounded reports, disabled build servers/core dumps and no inherited credentials were retained. Parent limits were15seconds SDK identity/120seconds restore and build/30seconds runtime plus≤5seconds owned cleanup. The child25-second elapsed check ends after cleanup and before report serialization; parent bounds complete execution. Hashing/traversal lies outside these command timings. Output/cell/cardinality checks and storage polling are rejection thresholds with potential overshoot, not hard disk/RSS quotas. Native rounded memory rendering is not exact internal/process memory. Command flags do not prove statement timeout or large-result streaming enforcement.

Actual controls confirmed external access, automatic known-extension installation/loading and persistent secrets disabled, with configuration locked before queries. These specific flags do not establish that all extension initialization is disabled: retained engine sources default load_extensions to true and call LoadAllExtensions during startup. Linked-extension initialization and host filesystem reads remain outside this claim. In-memory READ_ONLY was not used. No imports, services, accounts, user records, OS installation or production dependency changes occurred. Historical Q0.1/P4 failures, prior successes and their inspectors remain unchanged.

## Verification and disposition

Historical commands, each invoked once after its own prospective Oracle CLEAR:

```text
/usr/bin/python3 -I artifacts/duckdb-probe/ordinary-loader.tm17v9sl/ordinary-probe.py sdk
/usr/bin/python3 -I artifacts/duckdb-probe/ordinary-loader.tm17v9sl/ordinary-probe.py runtime
```

Locked restore exit0/1.212831757seconds; strict Release build exit0/1.546513376seconds/zero warnings/errors. Complete runtime operation exit0/0.334552098seconds, including owned-group checks0.365931292seconds. Child post-cleanup/pre-publication elapsed0.1825659seconds. These are single observed timings, not a performance guarantee.

Evidence is retained under artifacts/duckdb-probe/ordinary-loader.tm17v9sl. Observed Oracle CLEAR preceded disposition. Ten source/input snapshots plus actual report and13compiled outputs are preserved. Output retention used an owned same-filesystem rename with no duplicate copying; terminal pre-disposition combined storage2,116,167,383bytes fit the2GiB bound, without a measured-peak claim. Child PID was absent; the outside app was removed and runner renamed to a nonactive snapshot. The existing controlled cache stays owned for later reviewed work or relevant goal closeout.

## Remaining obligations

The new protection inspector passed127retained source hashes,137unchanged production inputs,12archive/payload identities and336ordinary binaries, with exactly two new ordinary-loading documents. Integrated Oracle review is CLEAR for this checkpoint only. Success qualifies only the observed ordinary Linux ARM64 profile and finite metadata/parameter/cleanup comparisons. Transactions/writes, broader types/parameters, file/import data policy, real adapter/public contracts, async/cancellation, large results, coverage and platform/support matrices still require their original gates. The full roadmap remains active. Any next probe needs a separate saved and reviewed packet before dispatch.
