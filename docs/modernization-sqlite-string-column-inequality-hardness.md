---
title: SQLite string-column inequality behavior checkpoint
version: 0.2
last_updated: 2026-10-04
status: Observed partial checkpoint
owner: IQToolkit project owner
tags: [modernization, sqlite, null-semantics, atlas]
---

# Hardness report: SQLite string-column inequality

Scope: M0 tasks V1ai.0–3 and the strict/API/symbol/package-consumer/coverage portion of V1ai.4. The owner-selected [.NET standard](standards/source/Cross-Platform-DotNet-Engineering-Standard-1.1.0-draft.1.md), especially DOTNET-TEST-001 and process/storage requirements, and Development Plan Creation Guide 2.7 govern this checkpoint. The finite coverage transition is qualified; fact registration, current-final assessment and broader V1ai closure remain required.

The actual query defect is corrected: a mapped string-column inequality filter previously discarded the genuine native NULL row that C# selected. The unchanged four tests now pass, including the existing client projection and null/nonNULL controls. This is a scoped native SQLite result on Linux Arm64; it does not certify generalized collation/coercion/null policy or other providers/platforms.

## Edge cases and change

The tests acquire SQLite's existing pragma_table_xinfo metadata through two distinct nonpooled connections. They capture complete buffers, schema, DBNull flags, native identities and loaded driver paths/hashes before assertions. No rows, schema or replacement metadata records are authored. The native default column retains its actual BLOB/System.Byte[] schema and DBNull cell; explicit CLR string mapping materializes that cell as null. Hidden-argument binding and full materialization pass before the comparison assertion. [The acquisition checkpoint](modernization-nullable-column-acquisition-hardness.md) remains unchanged and separate from this toolkit behavior evidence.

The authentic pre-repair run has one predicate assertion failure: expected native-derived CLR selection 1, actual toolkit selection 0. The translated WHERE retains nonlifted Boolean System.String.op_Inequality over the two mapped string columns and executes <>. Three controls pass: literal-null/full projection, ordinary client-side Boolean projection, and actual null/nonNULL self/comparison controls. [RED observations](../artifacts/nullable-column/red.fi5vs3wi/observed-tests.json) and [TRX](../artifacts/nullable-column/red.fi5vs3wi/results/behavior.trx) preserve the source frame and complete native evidence.

The sole production insertion adds eight lines to [SQLiteFormatter.GetOperator](../IQToolkit.Data.SQLite/SQLiteFormatter.cs). It selects IS NOT only for NotEqual over two string ColumnExpression operands with the observed System.String.op_Inequality method. The unobserved method-null alternative is excluded. Existing concatenation and fallback bytes, common literal-null handling, client projection, other providers, public signatures and dependencies are unchanged. SQLite documents that IS NOT handles NULL operands while preserving ordinary nonNULL comparison behavior. [SQLite expression operators](https://www.sqlite.org/lang_expr.html).

[GREEN observations](../artifacts/nullable-column/green.8do5vhwy/observed-tests.json) show the repaired filter selecting the same one row as CLR and all four methods passing. Test source and all six compiled Test DLL/PDB/reference outputs remain byte-identical between RED and GREEN. Only six SQLite implementation-output copies change; reference outputs remain unchanged. The subsequent whole-solution strict gate propagates the exact repaired DLL/PDB to the two remaining legacy Test/bin dependency copies.

## Verification and protection

| Gate | Actual outcome / retained evidence |
| :--- | :--- |
| Focused RED / GREEN | Native exits0/0/0/1 then0/0/0/0; build 0 warnings/errors;three passing controls and one named failure, then four passes/no skips. Both source/operator/observed reviews are Oracle CLEAR. |
| Strict | 435 definitions, 110 passes, 325 explicit SQL Server skips and 247 translations; zero warnings/errors. All 431 inherited method/category/outcome entries remain unchanged; exactly four additions. [Producer](../artifacts/verification/runs/run.u1bnzy7T/) / [review](../artifacts/nullable-column/strict.6n49v58w/observed-oracle.json). |
| API / archives | Six comparisons against pinned 1.1.1 pass;six nupkg and six snupkg payloads exactly match current DLL/PDB bytes. [Producer](../artifacts/api-compatibility/runs/run.zY7GAX2N/) / [review](../artifacts/nullable-column/api.7559imzu/observed-oracle.json). |
| Symbols | Six package/PDB pairs and source checks pass. [Producer](../artifacts/package-symbols/runs/run.eKK6tZdL/) / [review](../artifacts/nullable-column/symbols.6szwrruo/observed-oracle.json). |
| Independent consumers | Six public compilations/provenance confirmations, seven graph reviews and the complete package-only native SQLite witness pass against the same fresh feed. [Producer](../artifacts/ci-consumers/runs/run.nvlwAtiy/) / [review](../artifacts/nullable-column/consumers.ck5z00n0/observed-oracle.json). |
| Coverage | All 16,724 inherited entries/conditions are nonregressing; 12 actual PDB-bound additions execute. Same 97 sources; 2,968/8,359 lines and 1,602/5,336 branches. [Collection](../artifacts/nullable-column/coverage.843ik22m/observed-oracle.json) / [qualification](../artifacts/nullable-column/qualify.26k5mzn2/observed-oracle.json). Permanent baseline extended without reset; targets remain unmet and unwaived. |

## Failures, bounds and ownership

No behavioral SDK setup or protecting gate fails. Pre-dispatch Oracle source reviews catch and correct result-publication/session-settlement checks, native TRX identity/frame linkage, and the two strict dependency-copy destinations. Original controller versions and review findings remain retained. These are source-preparation revisions; the acquisition's prior CS8602 failure remains unchanged. The later qualification preflight failure and repair are recorded below; combined artifact repairs are 2 of 3.

SDK processes run serially under root with 120-second ordinary leaves, a 300-second collection leaf, 900-second parents, 16 MiB logs and an independently enforced 2 GiB newly owned storage cap. Native commands have 10-second timeouts and 1,024-row guards. Session settlement is observed, then independently rescanned before same-filesystem retirement. Retained inventories, inactive code/project/PE names, source/input/index and 336-binary fences verify. All ten behavior/protecting/coverage environments, including the rejected preflight, are preserved and absent at their original paths; source leases and native sessions are terminal.

The consumer run finishes in 40.63 seconds with 906,705,030 owned bytes. This exceeds its original 880,292,956-byte planning estimate by 26,412,074 bytes, while remaining below the separately enforced 2 GiB cap. The original estimate was not a valid upper bound. Original packet/closure/receipts remain unchanged. Oracle qualifies the actual bounded outcome with this limitation; future allocation reviews must include the measured complete consumer tree and retention allowance. No replay or receipt relabeling is used.


## Coverage transition and recovered preflight

Fresh strict [run.EeJwUVqF](../artifacts/verification/runs/run.EeJwUVqF/) binds the tested draft0.105 source snapshot and repeats 435 definitions, 110 passes, 325 explicit skips, 247 translations and zero warnings, with all 336 binaries unchanged. The collector preserves the same 219 test-output files and shadow. The final comparison retains 409 physical inputs totaling 62,474,255 bytes, below its unchanged 128 MiB/2,048-input limits; its 954 seals, source/index and ordinary binaries remain exact. All four native qualifier commands exit0 and the build reports zero warnings/errors.

Seven fixed anchors and all 195 V1ag retained origins/snapshots authenticate the permanent TFv2→V1ag→current chain. The source recipe inserts exactly 413 bytes/eight lines at original byte18,996. Every inherited formatter line at502 or later shifts eight lines; every other key keeps its identity. All inherited branch denominators, condition identities/order and covered status/counts remain nonregressing. Core/Data/SqlClient pairs stay exact; only SQLite/Test pairs change, with the one formatter checksum and one new Test PDB document. All 431 previous test identities remain exact and four compiled methods/source locations are authenticated.

Actual additions are six class entries and six matching method entries at lines502–506 and508. The compiled PDB identifies `GetOperator(System.Linq.Expressions.BinaryExpression)` and the executed `IS NOT` return at508. The native guard reports16/16 branches; retain that aggregate without claiming independent arm adequacy or target compliance. The collection completes in6.77seconds/230,927,707ownedbytes and retires274 files; the qualifier completes in4.16seconds/184,360,066ownedbytes and retires51 files. All observed session handles settle before independently checked atomic retirement.

The original [qualify.pi7xzvfk](../artifacts/nullable-column/qualify.pi7xzvfk/result.json) fails its mandatory input-seal guard before any SDK or comparison command: the current descriptor itself was omitted from reviewed inputs. All fences hold and its four private files are retired inactive. The reviewed preparation repair explicitly seals the descriptor and uses a separate retry pointer. The same helper, controller, raw reports and assertions run in the fresh qualified packet. Original failure and [Oracle disposition](../artifacts/nullable-column/qualification-preflight-failure-oracle.json) remain retained; no failure is relabeled and no semantic RED is manufactured.

## Concurrency, security and remaining work

Tests own separate nonpooled connections and dispose them; native requests and root SDK operations are sequential. The source worker owns only the new test file until its ended handoff. Root owns production/shared/configuration/profile/controllers/evidence/runtime/index. Oracle is read-only. Inputs are engine metadata and installed/package assets; no application records, credentials, resolver overrides or service provisioning are introduced. No throughput, shared-connection load, hosted matrix or production database claim is made.

SQLITE.STRING.NULL.NE.v1 remains Proposed in the 34-row ledger until separately reviewed evidence registration/current-final binding; all 33 prior rows remain exact. Historical fact-binding metadata covers 33 rows. The qualified coverage transition extends the authenticated V1ag 97-source/16,724-leaf/431-test chain to 435 tests through five exact aliases, the insertion recipe and actual new PDB-bound entries. Core/Data/SqlClient pairs stay unchanged; only SQLite/Test pairs differ from that predecessor. The existing permanent baseline and qualification receipts remain immutable.

Current-final/durable transition adoption, checked consumer-lock/feed refresh, coverage policy/targets, remaining M0/resource work, future public/provider/workload/data decisions and the full modernization roadmap stay open. This report records observed behavior, protecting and coverage progress. Formal evidence registration/current-final binding, durable transition adoption and checked consumer-lock/feed refresh remain required before whole V1ai.4 closure; M0 and roadmap completion are still open. No commit, push, tag or publication follows from this checkpoint.
