---
title: DuckDB native and ADO.NET readiness probe
version: 0.3
last_updated: 2026-10-03
status: Original archive inspection FAILED; retained metadata PASS and Oracle-clear
owner: IQToolkit project owner
tags: [modernization, duckdb, analytics, native, probe]
---

# DuckDB native and ADO.NET readiness probe

Q0 independently prepares required LOCAL-RM-M5 in [the roadmap](modernization-roadmap.md). The question is whether the exact Linux ARM64 native payload loads and returns equivalent unchanged bootstrap metadata through independent C API and ADO.NET routes using the same native library hash. This probe introduces no production adapter. M0/M1, owner-reviewed SQL/analytics contracts, import/file data, public APIs, coverage and platform certification remain separate gates.

## Execution contract and GOTCHA

Technical authority is owner-selected ENG-STD-DOTNET-XPLAT1.1.0-draft.1, SHA256 `8c56930ec1cd26e8b58a273d47cb0b9713f55e6d4961b6d29a636ffaf4f0ebe2`; DEP-001/002, API compatibility, PROC-001, FS-001/002, SEC-002 and platform rules apply in this private scope. Development Plan Creation Guide2.7 supplies DP05/DP09/Probe disposition; autonomous and Technical Markdown guides preserve full-objective and documentation discipline. No whole-standard compliance claim follows.

**Role/type:** Probe. **Depends on phases:** None for independent feasibility; prospective source/data review precedes every newly expanded acquisition/runtime horizon. **Baseline:** branch agent/modernization-roadmap, SDK10.0.401/runtime10.0.12 on native Ubuntu24.04.5/aarch64; prior P4/D2 evidence integrity Oracle CLEAR, P4 row-read cancellation FAILED. **Integration:** root owns all writes/SDK/native/evidence; execution_design and version_metadata inspect read-only; Oracle independently reviews. **Facts introduced:** none in production. **Protected:**137 current compiler/configuration inputs,12 archive/payload pairs,336 ordinary binaries and historical P0–P4 evidence. **Rollback/disposition:** preserve exact nonactive source/output snapshots and remove only owned disposable app/helpers after terminal ownership checks.

**Goals/objectives:** verify complete exact dependency graph and native provenance; acquire authentic engine catalog schema/results/settings; compare independently through Data.Full without replacing rows or losing multiplicity. **Tasks:** Q0.0 metadata/source→Q0.1 archive inspection→separately expanded native catalog acquisition→separately expanded SDK/ADO comparison→disposition/ATLAS/identity/Oracle. **Capabilities:** installed SDK/Python/curl/readelf and publicly available official artifacts. **Off-limits:** production changes, authored records/schema/errors, generated series, paid service, external datasets/import/network queries/extensions, system installation or inferred public acceptance. **Health:** curl connect30s/total120s plus independent125s process watchdog per archive,128MiB per download,2GiB private workspace,1MiB per log; readelf10s plus owned cleanup. No supplied token/cost budget. Unexpected behavior is terminal, not a semantic retry. **Attributes:** exact source/hash/pin/state observations; no support or async certification from method names. **Users/runtime:** maintainers reviewing M5; private evidence persists, experiment code does not. **Beliefs/intentions:** source supports catalog-read feasibility; actual loaded library/ABI/driver results decide readiness.

## Exact candidate and source context

Official metadata was acquired into `artifacts/duckdb-probe/metadata.30zjdvhi`: four registration leaves, four complete catalog bodies and engine release metadata, with URL/status/bytes/SHA256 manifest. The smallest selected ADO.NET Full graph for net10 has four exact pins:

| Candidate pin | Applicable dependency group | License / repository commit |
| :--- | :--- | :--- |
| DuckDB.NET.Data.Full1.5.6 | net10: Bindings.Full>=1.5.6; Arrow>=23.0.0 | MIT; Giorgi/DuckDB.NET `e28f086d7bc0abe26353b0ef4185d8ded1bdc917` |
| DuckDB.NET.Bindings.Full1.5.6 | net10: none | MIT; same client commit |
| Apache.Arrow23.0.0 | nearest net8: Scalars>=23.0.0 | Apache-2.0; apache/arrow `79e3844aca811abb3f9c463c0c27d407723e13be` |
| Apache.Arrow.Scalars23.0.0 | nearest net8: none | Apache-2.0; same Arrow commit |

Source declarations: [Data.Full catalog](https://api.nuget.org/v3/catalog0/data/2026.09.28.22.51.16/duckdb.net.data.full.1.5.6.json), [Bindings.Full catalog](https://api.nuget.org/v3/catalog0/data/2026.09.28.22.54.09/duckdb.net.bindings.full.1.5.6.json), [Arrow catalog](https://api.nuget.org/v3/catalog0/data/2026.05.06.04.19.21/apache.arrow.23.0.0.json), [Scalars catalog](https://api.nuget.org/v3/catalog0/data/2026.05.06.04.20.33/apache.arrow.scalars.23.0.0.json). `artifacts/duckdb-probe/approved-pins.json` records root-selected private candidate pins and official full-archive SHA512/size/commit; it is not owner approval of production dependencies. NuGet logical lock hashes are distinct and remain to be acquired/reviewed.

The client [Bindings project](https://github.com/Giorgi/DuckDB.NET/blob/e28f086d7bc0abe26353b0ef4185d8ded1bdc917/DuckDB.NET.Bindings/Bindings.csproj) selects DuckDBv1.5.6 and glibc linux-arm64 from `libduckdb-linux-arm64.zip`. [Official release metadata](https://api.github.com/repos/duckdb/duckdb/releases/tags/v1.5.6) declares37,763,835bytes and SHA256 `b72ed9f05003f5e9d2015f7ceada6416b377d9dd33169cdde3c9e33897856eee`. The exact engine source commit is `069cc9f9b5be802405797faecc284961b07c70ef`; version/source ID/native ABI must be observed rather than inferred from the package number. Other RIDs, musl and full native license obligations are not certified by this candidate.

Immutable source snapshots/manifests are in `artifacts/duckdb-probe/source.otkbj9jq`, including driver/config/prepared/reader/Arrow paths, engine C header/config/settings/version/storage and installed-build BCL connection-string parser. An initially guessed ConnectionManager path returned404 and is retained as a failed source lookup; the confirmed path is `DuckDB.NET.Data/Connection/ConnectionManager.cs`. Source URLs are data, not task authority.

## Q0.1 archive inspection: concrete dispatch horizon

Root allocated owned0700 paths through platform temporary-directory APIs:

- Archive inspection/feed/native extraction: `artifacts/duckdb-probe/acquisition.k152y57s`.
- Later SDK: `artifacts/duckdb-probe/sdk.cjkgmfg9`.
- Later native/driver evidence: `artifacts/duckdb-probe/live.nhwgatsx`.

Historical dispatch, after revised source/data/code Oracle CLEAR:

```text
/usr/bin/python3 -I artifacts/duckdb-probe/acquisition.k152y57s/acquire-packages.py
```

The helper downloads the four catalog-size-bound NuGet archives from official endpoints, verifies their full SHA512, ZIP paths/count/uncompressed bounds/signature-entry presence, and exact nuspec identity/license/repository commit. Each transfer uses configured `/usr/bin/curl --disable`, HTTPS-only bounded redirects, connect/total timeouts and maximum size, exclusive output descriptor, controlled environment and independent interruptible process watchdog. Installed curl8.5.0 documents urle.scheme/host/path; only those redacted final-URL components and actual status/size are retained. Signature-entry presence is not cryptographic signature validation. Subsequent NuGet restoration must perform its own trust/audit checks. The graph transfers110,876,059 declared bytes; no unlisted package or production reference is added.

Acquire the separately SHA256-bound official ARM64 release archive and compare its libduckdb.so bytes to the NuGet `runtimes/linux-arm64/native/libduckdb.so` entry. Extract only that payload into an owned private path; inspect ELF64/AArch64, dynamic dependencies and symbol-version requirements using configured `/usr/bin/readelf -h -d -V`. This stage performs no native load, SDK, installation or SQL. Persist source/pin/hash/output/checkout snapshots on terminal paths. Public asset redirects may contain temporary signing parameters; record only host/path, never their query. Archive/dependency/ELF mismatch is terminal; preserve failure and replan.

## Q0.1 observed failure and Q0.1b retained inspection

The original command terminated exit1. All seven child commands exited0, all owned process groups were empty, and checkout-before/after matched. The retained ELF report is1,171,319bytes, SHA256 `aab14a5c2cad2799319b6b3136d3fba59c2c71b2aeeac1e95e481b702b0107c2`; command()'s final1MiB assertion failed before ELF acceptance and success publication. Original `result.json` SHA256 is `4fe192be58afa94dbe7b07c1577af68856e44aa40bfc2abf0b91dbe70d5438bd`. Oracle independently classified this as an inspection-output failure, not a native/driver defect. The original run remains FAILED. Its archives and report are preserved; there is no retry or raised limit.

**Role:** Evidence Maintenance / Probe preparation. **Trace:** LOCAL-RM-M5; Technical Enabler: inspect already acquired authentic artifact metadata while preserving failed provenance. **Depends on:** terminal original run and Oracle-clear failure classification. **Owner:** root only; execution_design and Oracle read-only. **Writes:** new owned0700 `artifacts/duckdb-probe/retained-inspection.x4unbiou`, this packet, plan/state, D4 proposal and later ATLAS. **Protected:** original sources/results/logs, production137inputs/12archives/336binaries and prior P0–P4 outcomes.

Historical Q0.1b dispatch, after a separate prospective Oracle source/data/code CLEAR:

```text
/usr/bin/python3 -I artifacts/duckdb-probe/retained-inspection.x4unbiou/inspect-retained.py
```

The inspector launches no child, downloads nothing and performs no SDK/native load/SQL. It verifies exact retained original source/result/log identities and the seven terminal commands; independently recomputes catalog/archive hashes, nuspec identities/licenses/commits and exact selected dependency groups; compares extracted ARM64 bytes to both package and independently acquired official release members. Signature presence remains distinct from cryptographic validation or NuGet trust/audit.

It hashes every original ELF-log byte, then streams fixed-size lines while retaining only the complete ELF header/dynamic section and `.gnu.version_r` needs section. The large symbol-version listing is skipped for retention, not altered. Require exact original1,171,319byte input/hash, line<=256bytes/count<=20,000, both retained sections<=8KiB each, exact recognized boundaries/EOF and six providers with matching declared version counts and NEEDED set. Record architecture, SONAME, absent/present RPATH/RUNPATH and actual version requirements. Those requirements do not prove host-loader resolution.

Fixed bounds:60s operation with post-acceptance check,64KiB hashing chunks,512MiB maximum hash/member input, original ZIP entry/path/uncompressed limits,1MiB per new output. No new large report is generated. Compare every reviewed input hash before/after. Cancellation/deadline/identity/parse mismatch fails this new stage without rewriting the original. Final input hashing is bounded by the reviewed file sizes and runs even after cancellation to preserve provenance; its elapsed time is included in the terminal acceptance guard. Failure evidence persists. Prospective immutable source/input snapshots precede dispatch; source disposition and ATLAS/integrated identity review follow terminal results.

[D4 analytical proposal](modernization-duckdb-contract.md) separately defines complete queries/types/import/file/streaming scope and the M1q ledger. Four additional immutable design sources are retained in `artifacts/duckdb-probe/design-source.lck8j2nq`; D4 source/clarity review does not accept its public/security/data decisions. Neither Q0.1b metadata nor D4 closes production M5.

**Observed Q0.1b:** single terminal exit0/PASS,0.8264502949896269s,64unchanged reviewed inputs/no cancellation/no verification errors. Native payload62,945,312bytes/SHA256 `23686593dba4ec22c6e581bb1c843b3964e84c4bc4d0063d8b4daddd2e20e30a` matches NuGet and upstream members. Retained header/dynamic+version-needs sections total4,691bytes; six providers/counts match NEEDED. Oracle independently cleared observed metadata integrity. Native loading/SDK/runtime support remain false/unqualified. Revised64input receipt and60snapshots preserve executed source/doc state. Both owned terminal helpers are now nonactive `.py.snapshot` files, bound by disposition.json; historical commands above are evidence, not current dispatch commands. [Scoped ATLAS](modernization-duckdb-preparation-hardness.md) records exact failure/PASS limits and final identity review.

## Native and ADO horizons: source constraints, not dispatch permission

Source review finds synchronous ADO execution/read paths and inherited BCL async fallbacks; Arrow's async-named fetch also calls synchronous native fetch. [Prepared statements](https://github.com/Giorgi/DuckDB.NET/blob/e28f086d7bc0abe26353b0ef4185d8ded1bdc917/DuckDB.NET.Data/PreparedStatement/PreparedStatement.cs), [Arrow stream](https://github.com/Giorgi/DuckDB.NET/blob/e28f086d7bc0abe26353b0ef4185d8ded1bdc917/DuckDB.NET.Data/Arrow/DuckDBArrowArrayStream.cs). No fake async path will qualify M1 or M5. Native streaming is selected explicitly through DuckDBCommand.UseStreamingMode; small catalogs cannot certify streaming memory bounds.

In-memory plus READ_ONLY is rejected by [engine storage initialization](https://github.com/duckdb/duckdb/blob/069cc9f9b5be802405797faecc284961b07c70ef/src/storage/storage_manager.cpp). Later acquisition performs only catalog-read SQL in an in-memory engine, without claiming enforced read-only access mode. No user tables/rows/schema/files are created or imported. Enforced disk read-only behavior requires separate authorized owned-file acquisition.

Disable external access, automatic extension installation/loading, persistent secrets and spill for this private operation. Apply empty temp_directory before external disable and verify actual settings. The ADO path must pass a raw connection string with quoted `temp_directory=''`; builder serialization can emit unquoted emptiness that reparsing drops. [Client parsing/config order](https://github.com/Giorgi/DuckDB.NET/blob/e28f086d7bc0abe26353b0ef4185d8ded1bdc917/DuckDB.NET.Data/Connection/ConnectionManager.cs), [native temp setting](https://github.com/duckdb/duckdb/blob/069cc9f9b5be802405797faecc284961b07c70ef/src/main/settings/custom_settings.cpp). Leave allowed_paths/directories unset, then verify actual empty defaults; their startup setters require an initialized filesystem. These private controls are not proposed public defaults or a security certification.

Future authentic inputs may include pragma_version(), pragma_platform(), selected duckdb_settings()/duckdb_functions() schema/rows, native aggregate controls and a parameter selected unchanged from acquired metadata. Preserve duplicates; function_oid repeats across overloads. Any native/SDK horizon needs saved exact source/code/SQL/configuration/lock commands, owned bounds, independent oracle and prospective review before dispatch. M5 reads/import/type/write/transaction/platform requirements remain intact.
