#!/usr/bin/env bash
set -euo pipefail
[[ "$#" -eq 3 && ( "$1" == red || "$1" == green ) ]] || exit 2

# Native exits, provenance and terminal process disposition are supervisor-owned.
/usr/bin/python3 - "$1" "$2" "$3" <<'PY'
import json
import sys
from pathlib import Path

mode, outcome_path, exit_path = sys.argv[1:]
path = Path(outcome_path)
outcome = json.loads(path.read_bytes())
native_exit = int(Path(exit_path).read_text().strip())
if mode == "red":
    if not (native_exit == 1 and outcome.get("Status") == "rejected"
            and outcome.get("Operation") == "evidence-integrity"
            and outcome.get("Reason") == "Final input identity differs."
            and not (path.parent / "active-descriptor.txt").exists()):
        raise SystemExit("Old ordinary preflight did not produce the required authentic rejection.")
    raise SystemExit("Ordinary final verification must bind current compilation and diagnostic evidence.")
expected = "/home/firestrand/Projects/IQToolkit/artifacts/command-initialization/coverage.gw5awfx_/collection/descriptor.json"
if not (native_exit == 0 and outcome.get("Status") == "verified"
        and outcome.get("Operation") == "transition-continuity"
        and outcome.get("ActiveDescriptor") == expected
        and outcome.get("FinalReadinessVerified") is False
        and (path.parent / "active-descriptor.txt").read_text() == expected + "\n"):
    raise SystemExit("Ordinary preflight did not qualify the exact current descriptor.")
print("Ordinary current compilation continuity is verified in scope.")
PY
