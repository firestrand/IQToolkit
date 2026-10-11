# Modernization resumption handoff

Updated: 2026-10-11. The owner resumed the roadmap implementation. This is the current operational handoff; detailed historical evidence remains in [AGENT_STATE.md](../AGENT_STATE.md), the [implementation plan](modernization-implementation-plan.md), and [BLOCKED.md](../BLOCKED.md).

## Repository baseline

- Branch: `agent/modernization-roadmap`
- Resumption baseline: `45c1163` (`Align MDB0 implementation review gate`); branch and origin were aligned and clean at that checkpoint before this resumed work. This reviewed documentation checkpoint follows that baseline.
- Verified code/build commit: `e9c9ab83efaddb76eb761bbd1468f16b83623867` (`Modernize project build, tests, and roadmap`)
- The earlier stable MDB0.1a checkpoint `0a22b06e2f8e50d850612a8789291c47457f11b6` remains in history. The initial execution baseline `60ddef6` is historical; `e9c9ab8` remains the latest verified .NET code/build baseline. Check Git state before each action.
- Outcome-integrated plan/GOTCHA: draft 0.201 / 1.40. Executable plan0.195/GOTCHA1.37 received Round84 unconditional PASS before controls. The isolated RED and four corrected/static controls are complete, and Round85 source review returned CLEAR for its exact Round4 manifest. Round87 accepted plan0.199 for its exact candidate and closed MDB0.2-PLAN-001. Round88 reviewed plan0.200 and returned CHANGES_REQUIRED for MDB0.2-PLAN-002/003; plan0.201 corrects the current revision label and keeps changing next-action status in detached candidate state. Round85 source CLEAR is limited to its exact source/evidence epoch. MDB0.2 requires current planning PASS and source CLEAR before any separately authorized packet preparation. Consult the highest candidate-round state file for the active next action.

The committed revision passed the locked Release build with warnings treated as errors, the commit-bound `eng/verify.sh` profile, package and symbol/Source Link inspection, six independent consumer checks, SQLite package runtime validation, and the v1.1.1 API comparison. The full test run discovered 435 tests: 110 passed and 325 SQL Server integration tests were explicitly skipped because their environment was absent; 247 translation tests passed. These checks do not establish compatibility with absent SQL Server environments or certify any new provider.

## Current workstream

The implementation source review Round3 for candidate Round85 returned `CLEAR` for its exact Round4 manifest (SHA256 `71e934a82317f4f44318dfcb3f58c3e61479ad32b82a74a3426e118000089577`), with all 27 identities matching and MDB0.1b-SRC-001 closed for that source/evidence epoch. Its immutable report SHA256 is `67787a099ec58400b7616db24690972a5de75b6756b229096f4cd5778b53b4ab`; provenance SHA256 is `666470f3f28569338bab138285d1d0749506da11ddfe769246b3a3f5c2826928`. Round86/87 clarified and accepted Roadmap0.3 as current and Roadmap0.2 as historical for plan0.199. Round88 reviewed plan0.200 and returned CHANGES_REQUIRED (MDB0.2-PLAN-002/003); plan0.201 corrects the revision and status wording. MDB0.2 requires current planning PASS, then fresh source CLEAR, before separately authorized packet preparation. Any dispatch needs distinct exact-packet CLEAR and explicit owner authorization. The active next action is recorded in the highest candidate-round state file with its immutable report and provenance.

All earlier MDB0.1a missing-prerequisite and baseline RED files remain immutable. Round67 implementation evidence is preserved as an earlier source epoch and is not final acceptance. The retained stale attempt has not been accessed. No preparation, rebind, dispatch, network, package, archive, SDK or database operation is authorized. The separate V1aj.4 stop and its approval boundary remain unchanged.

The earlier preliminary preflight records remain immutable historical evidence because their command vectors omitted the script path and Python flags; do not overwrite them. Round85 source CLEAR closes MDB0.1b-SRC-001 for the reviewed correction epoch; it does not accept any future source/plan/packet changes. The retained stale attempt remains off limits. MDB0.2 must pass its own fresh review gates before preparing a new packet; no dispatch, network, package, archive, SDK or database action is authorized here.

MDB0.1a evidence (all four files are mode 0400 and retained under `artifacts/mariadb-package-discovery/`):

| Result | Bytes | SHA256 | Exit | Observed result |
| --- | ---: | --- | ---: | --- |
| `mdb0.1a-round53-missing-prerequisite-normal.json` | 1,158 | `ae4566e090be563891e131a1b9eec1c87ef602995a5f1d6d36ba654d129ce232` | 2 | Exact diagnostic; no assertions |
| `mdb0.1a-round53-missing-prerequisite-optimized.json` | 1,176 | `c93af7b6a4014b9a1931f79711e6200eb0696f59a8f24cbb29f8411f454af4e0` | 2 | Exact diagnostic; no assertions |
| `mdb0.1a-round53-red-normal.json` | 6,136 | `ffaba741ec726e7f0a83bb81dd230f49f05f81223c1c67509de04cea3f76e56e` | 1 | Exactly three expected RED cases |
| `mdb0.1a-round53-red-optimized.json` | 6,154 | `bd22499ad67d39ed1cfb1b35a1473b97244836c157ae07851046856e33f775de` | 1 | Exactly three expected RED cases |

The two preflight records contain empty stdout, exact `MISSING_PREREQUISITE: source.py` stderr and `behavior_assertions_run:false`. Each baseline record reports three tests, the three expected failure names, no errors or setup failures, completed temporary-fixture cleanup, and `live_attempt_access.touched:false` with no opened paths. The accepted Round60 candidate binds all four exact identities alongside Round54 predecessor acceptance and prior review evidence.

| Workstream | Verified position | Next step and boundary |
| --- | --- | --- |
| MDB0.1a–MDB0.2 MariaDB controller acceptance | MDB0.1a records remain immutable. Round84 PASS accepted plan0.195/GOTCHA1.37 before controls. The late-failure RED ran six tests with only the expected post-inventory descriptor-close failure; its private fixture was unchanged. Function-wide `finally` cleanup is implemented. Corrected normal/-O each pass six tests; static normal/-O each pass eight checks. Round85 source CLEAR applies to its exact Round4 manifest only. Round87 accepted plan0.199 and closed MDB0.2-PLAN-001. Round88 found MDB0.2-PLAN-002/003 in plan0.200; plan0.201 corrects those points. Current planning PASS and fresh source CLEAR are prerequisites to separately authorized packet preparation. Consult the highest candidate-round state file for the active next action; do not access or reuse the stale attempt.
| V1aj.4 final assessment | The fresh chain's nine producer stages passed, but assessment rejected evidence integrity before fact classification. The authentic `ps` witness has leading spaces before PID `574814`; the current strict parser rejects that representation. The 3/3 integration artifact-repair budget remains exhausted. Details and retained result hash are in [BLOCKED.md](../BLOCKED.md). | A proposed one-time bounded recovery is awaiting explicit owner approval. Do not edit the parser, rebuild, rerun producers, or dispatch an assessment until that approval is recorded. If approved, follow only its specified witness normalization, bounds, and verification steps. Otherwise keep V1aj.4 stopped; do not infer a budget reset. |
