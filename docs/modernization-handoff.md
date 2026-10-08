# Modernization resumption handoff

Updated: 2026-10-08. This is the current operational handoff; detailed historical evidence remains in [AGENT_STATE.md](../AGENT_STATE.md), the [implementation plan](modernization-implementation-plan.md), and [BLOCKED.md](../BLOCKED.md).

## Repository baseline

- Branch: `agent/modernization-roadmap`
- Verified code/build commit: `e9c9ab83efaddb76eb761bbd1468f16b83623867` (`Modernize project build, tests, and roadmap`)
- At the start of this documentation refresh, branch and remote `origin/agent/modernization-roadmap` both pointed to that commit; this handoff will be committed on top of it.
- The worktree was clean when this handoff began. The documentation update is the only work authorized in this pause; commit and push are authorized after review.
- The plan is now draft 0.148. Its initial execution baseline `60ddef6` is historical; use the commit above as the current checkout baseline.

The committed revision passed the locked Release build with warnings treated as errors, the commit-bound `eng/verify.sh` profile, package and symbol/Source Link inspection, six independent consumer checks, SQLite package runtime validation, and the v1.1.1 API comparison. The full test run discovered 435 tests: 110 passed and 325 SQL Server integration tests were explicitly skipped because their environment was absent; 247 translation tests passed. These checks do not establish compatibility with absent SQL Server environments or certify any new provider.

## Choose the workstream before acting

The owner is pausing. No implementation, assessment, acquisition, or external operation is authorized by this handoff. On resumption, confirm which workstream to continue and honor its gate independently.

| Workstream | Verified position | Next step and boundary |
| --- | --- | --- |
| MDB0.1 MariaDB controller acceptance | Candidate Round30 returned planning `PASS` and source `CLEAR` for plan 0.147 and its exact review inputs. Planning report SHA256: `b2073c2648b5b160d7321649e3ba835dcc7d602b0a4a5c4fff1757135aa05f45`. Source report SHA256: `556639545a18a90363ec024f4cc46a821a2afa4e6d0174e834c1a243ce716a62`. These scoped verdicts close the reviewed controller-fixture/source findings only. | The old packet/attempt is historical and must not be reused or rebound. This handoff changes plan bytes to 0.148, so Round30 does not constitute acceptance of the current plan bytes or any new packet. Before preparing another packet, obtain fresh planning/source acceptance of the current exact inputs; obtain exact-packet review before any dispatch. Package/network acquisition, extraction, SDK, engine, provider, or database work requires its own explicit authorization and accepted receipts. |
| V1aj.4 final assessment | The fresh chain's nine producer stages passed, but the assessment rejected evidence integrity before fact classification. The authentic `ps` witness has leading spaces before PID `574814`; the current strict parser rejects that representation. The 3/3 integration artifact-repair budget remains exhausted. Details and retained result hash are in [BLOCKED.md](../BLOCKED.md). | A proposed one-time bounded recovery is awaiting explicit owner approval. Do not edit the parser, rebuild, rerun producers, or dispatch an assessment until that approval is recorded. If approved, follow only its specified witness normalization, bounds, and verification steps. Otherwise keep V1aj.4 stopped; do not infer a budget reset. |

## Round30 evidence availability

The Round30 JSON reports and provenance are in `artifacts/mariadb-package-discovery/candidate-round-30/`. They are ignored by `.gitignore` and are not included in this commit. The hashes above identify the reports but do not carry their contents into a fresh clone. A new agent in this preserved workspace can inspect them; an agent in a fresh clone must retrieve the exact preserved artifacts or commission new reviews before relying on their detailed findings. Do not describe Round30 as review of plan 0.148.

## Resume sequence

1. Read this handoff, [AGENT_STATE.md](../AGENT_STATE.md), [BLOCKED.md](../BLOCKED.md), and the applicable task section of [the implementation plan](modernization-implementation-plan.md); verify `git status` and the current commit before acting.
2. Resolve the pending V1aj.4 approval explicitly. If it remains unanswered, leave that track stopped.
3. For MDB0.1, treat Round30 as scoped historical evidence against plan 0.147. Get fresh review of current plan/source inputs before any new packet preparation, then review the exact packet before dispatch. Preserve all acquisition and runtime authorization gates.
4. Update this handoff and the execution ledger with new evidence and exact hashes before handing work to another agent. Never infer phase closure, provider certification, or a retry-budget reset from scoped review results.

No raw MariaDB archive acquisition, extraction, provider execution, or database operation is recorded as having occurred. The full modernization roadmap remains open.
