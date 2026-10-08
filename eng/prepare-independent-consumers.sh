#!/usr/bin/env bash
set -euo pipefail

task_root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
source "$task_root/eng/IndependentConsumers/common.sh"
run_independent_consumers prepare "$@"
