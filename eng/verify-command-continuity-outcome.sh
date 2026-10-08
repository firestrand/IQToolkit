#!/usr/bin/env bash
set -euo pipefail

# The owning supervisor authenticates native exits, sources and process disposition.
if [[ "$#" -ne 3 || ( "$1" != red && "$1" != green ) ]]; then
    printf '%s\n' 'Expected red|green, retained outcome and native exit file.' >&2
    exit 2
fi
/usr/bin/python3 - "$1" "$2" "$3" <<'PY'
import json
import sys
from pathlib import Path

mode, outcome_path, exit_path = sys.argv[1:]
outcome_file = Path(outcome_path)
outcome = json.loads(outcome_file.read_bytes())
native_exit = int(Path(exit_path).read_text().strip())
if (outcome_file.parent / "active-descriptor.txt").exists():
    raise SystemExit("Scoped control must not publish an active descriptor.")
if mode == "red":
    if not (native_exit == 1 and outcome.get("Status") == "rejected"
            and outcome.get("Operation") == "evidence-integrity"
            and outcome.get("Reason") == "Final input identity differs."):
        raise SystemExit("Old helper did not produce the required authentic rejection.")
    raise SystemExit("Current command-initialization continuity must bind without changing permanent anchors.")
expected = {"Status": "verified", "Operation": "command-compilation-continuity",
            "SourceFiles": 97, "ComparedLeaves": 16724,
            "ComparedInheritedLeaves": 16716, "AddedLeaves": 8, "RetainedInputs": 587,
            "FinalReadinessVerified": False, "DurableFinalProfileAccepted": False,
            "FullGoalComplete": False}
if native_exit != 0 or any(outcome.get(key) != value for key, value in expected.items()):
    raise SystemExit("Current command transition outcome does not match qualified scope.")
print("Current command-initialization continuity is verified in scope.")
PY
