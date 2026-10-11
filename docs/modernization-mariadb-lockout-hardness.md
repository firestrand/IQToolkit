# MariaDB stale-attempt lockout and reservation accounting

## Hardness Report: MDB0.1b

### Result

The later-stage directory descriptor leak in `read_reconciled_reservation()` is corrected. After the function successfully opens the attempt directory, all remaining checks and reads now run under one `try/finally` that closes the directory descriptor on return or refusal. Individual evidence-file descriptors retain their existing `finally` cleanup.

The accepted plan was plan0.195/GOTCHA1.37, unconditional PASS in Round84. The isolated RED reproduced only the intended close failure. The corrected normal and optimized suites each pass six tests, and both static runs pass all eight checks. Round85 source review returned CLEAR for the exact result/source/docs epoch and closed MDB0.1b-SRC-001. These scoped results do not establish broader archive, provider, or database behavior.

### Edge cases tested

- The existing `LockoutTests` class remains byte-identical: 4,020 bytes, SHA256 `3e973aa75094af17bf88df2bb39bfc9a7e4da6dfbd2e3956cc130190084812ae`.
- The isolated late-stage fixture uses the exact allowlisted names and authentic detached journal bytes, with mismatched `prepared-packet-original.json` bytes. It reaches `Rejected("reconciled-file-identity")` after inventory admission. Its six-test RED has one failure, the expected directory-descriptor-close assertion; it has zero errors, unchanged fixture snapshots, completed cleanup, and no retained-attempt access.
- The corrected normal and optimized runners each pass six tests. They reject all 88 individually mutated identity fields, all 186 runtime/unknown marker cases, and the late-stage identity refusal. Directory descriptors are closed on every refusal, and private fixture snapshots remain unchanged.
- A direct `admitted_attempt()` test passes the stale-attempt path string with filesystem helpers patched as traps. It returns `Rejected("stale-attempt-disabled")` without filesystem calls.
- Both static runs pass all eight checks: omitted CLI rebind choice; direct rebind refusal; early direct admission refusal; `main()` routing; supervisor and worker guards; acceptance refusal before I/O; and history accounting-helper wiring.

### Failure and evidence handling

All result records are mode 0400 and immutable. The historical `mdb0.1b-green-*` and `mdb0.1b-static-*` records remain untouched as evidence from the prior source epoch. The new Round78 output names are unique and were absent before execution. The expected RED is also retained unchanged.

No retained-attempt file was opened, copied, renamed, modified, or cleaned up. Tests used detached evidence and private temporary fixtures. No network, package, archive, SDK, database, preparation, rebind, run, or worker operation was invoked.

### Verification commands and results

- `/usr/bin/python3.12 -I -B artifacts/mariadb-package-discovery/verify_mdb0_stale_lockout.py --mode corrected --evidence artifacts/mariadb-package-discovery/mdb0.1b-late-failure-red-normal.json` → expected exit 1; six tests, only `ASSERT_LATE_STAGE_DIRECTORY_DESCRIPTOR_CLOSED` failed.
- `/usr/bin/python3.12 -I -B artifacts/mariadb-package-discovery/verify_mdb0_stale_lockout.py --mode corrected --evidence artifacts/mariadb-package-discovery/mdb0.1b-round78-green-normal.json` → exit 0; six passed.
- `/usr/bin/python3.12 -I -B -O artifacts/mariadb-package-discovery/verify_mdb0_stale_lockout.py --mode corrected --evidence artifacts/mariadb-package-discovery/mdb0.1b-round78-green-optimized.json` → exit 0; six passed.
- `/usr/bin/python3.12 -I -B artifacts/mariadb-package-discovery/verify_mdb0_stale_lockout.py --mode static --evidence artifacts/mariadb-package-discovery/mdb0.1b-round78-static-normal.json` → exit 0; eight checks passed.
- `/usr/bin/python3.12 -I -B -O artifacts/mariadb-package-discovery/verify_mdb0_stale_lockout.py --mode static --evidence artifacts/mariadb-package-discovery/mdb0.1b-round78-static-optimized.json` → exit 0; eight checks passed.

Controller `package_acquisition.py`: 128,915 bytes, mode 0600, SHA256 `07c5bd6df61001eecf3889e6603ce1b72e4de1380f4eb22c6ad1479094d9fb41`.

Verifier `verify_mdb0_stale_lockout.py`: 46,069 bytes, mode 0664, SHA256 `796960e9e594c627a3650a324a2fd329f483022e63ae2bb8dd462a88796effa7`.

The five immutable evidence identities are:

| Result | Bytes | SHA256 |
| --- | ---: | --- |
| Late-stage RED, normal | 267,049 | `6953768723922cf8972cd9dd9f1047927fddbf9e0df5ec0809ecb2f7e83d50ef` |
| Corrected, normal | 266,341 | `083dbb3d55c78c825098bec8f7c7f7ac5c4ad17fccb41d7e70e449b6ea0df12a` |
| Corrected, optimized | 266,359 | `55cc6419abe5fccc2fb6943c49c2b7cc86f65277e307b0c9b70c9c3676a51177` |
| Static, normal | 1,542 | `1ccf6b5350c3e75f78bea24528c022a724d911d215657186bc8859ebad56ce95` |
| Static, optimized | 1,560 | `48e0f2026bc5736173c2707eb5c3b0d7d666a59347c3a03119b9d0ce77b1e75b` |

### Remaining gate

Round85 source review Round3 returned `CLEAR` for the exact 27-input manifest `artifacts/mariadb-package-discovery/candidate-round-85/source-review-inputs-round-4.json`, SHA256 `71e934a82317f4f44318dfcb3f58c3e61479ad32b82a74a3426e118000089577`. The immutable report is `source-review-round-4.json`, 2,821 bytes, mode 0400, SHA256 `67787a099ec58400b7616db24690972a5de75b6756b229096f4cd5778b53b4ab`; provenance is 955 bytes, mode 0400, SHA256 `666470f3f28569338bab138285d1d0749506da11ddfe769246b3a3f5c2826928`. It closed MDB0.1b-SRC-001 for this exact source/evidence epoch. The review confirmed all inputs, including the post-inventory refusal fixture and descriptor cleanup, and observed no retained-attempt access or operational commands.

MDB0.1b is complete for this reviewed epoch. MDB0.2 is the next roadmap task and still requires its own fresh plan/source acceptance before preparing a new packet. The stale allocated attempt remains historical and must not be read, reused, or rebound. No acquisition, package, archive, SDK, or database action is authorized by this review.
