#!/usr/bin/env bash
set -euo pipefail

# External integration skips remain visible. Only a fully successful run may
# replace the latest-success pointer; failed attempts retain their own evidence.
task_root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
cd -- "$task_root"
ci_properties=()
if [[ "${GITHUB_ACTIONS:-false}" == 'true' ]]; then
    ci_properties=(-p:ContinuousIntegrationBuild=true)
fi
verification_root="$task_root/artifacts/verification"
mkdir -p -- "$verification_root/runs"
verification_output="$(mktemp -d "$verification_root/runs/run.XXXXXXXX")"
printf '%s\n' running > "$verification_output/status.txt"
finish_verification() {
    local exit_code=$?
    if (( exit_code != 0 )); then
        printf '%s\n' failed > "$verification_output/status.txt"
    fi
    printf 'Verification evidence: %s\n' "$verification_output"
}
trap finish_verification EXIT
git rev-parse HEAD > "$verification_output/revision.txt"
git branch --show-current > "$verification_output/branch.txt"
git status --porcelain=v1 > "$verification_output/worktree.txt"

# Hash tracked and nonignored untracked files, including dirty sources. Do not
# evaluate filenames as shell text or rely on HEAD alone as a tested snapshot.
capture_snapshot() {
    while IFS= read -r -d '' source_path; do
        if [[ -f "$source_path" ]]; then
            git hash-object -- "$source_path"
            printf '%s\n' "$source_path"
        fi
    done < <(git ls-files --cached --others --exclude-standard -z)
}
capture_snapshot > "$verification_output/snapshot.txt"

# Keep compiler/MSBuild processes within this verification invocation.
build_properties=(--disable-build-servers -p:UseSharedCompilation=false -nr:false)
dotnet restore IQToolkit.sln --locked-mode -p:Configuration=Release "${ci_properties[@]}" \
    "${build_properties[@]}" -warnaserror 2>&1 | tee "$verification_output/restore.txt"
dotnet build IQToolkit.sln -c Release --no-restore -t:Rebuild \
    "${ci_properties[@]}" "${build_properties[@]}" -p:CheckDeadCode=true -p:EnforceCodeStyleInBuild=true \
    -p:AnalysisLevelStyle=10.0-none -p:RunAnalyzersDuringBuild=true \
    -warnaserror 2>&1 | tee "$verification_output/build.txt"
dotnet test IQToolkit.sln -c Release --no-build --no-restore "${ci_properties[@]}" "${build_properties[@]}" \
    --logger 'trx;LogFileName=tests.trx' \
    --results-directory "$verification_output" 2>&1 | tee "$verification_output/tests.txt"
dotnet run --project Test -c Release --no-build --no-restore "${ci_properties[@]}" \
    --disable-build-servers -p:UseSharedCompilation=false 2>&1 | tee "$verification_output/translation.txt"
bash eng/verify-test-results.sh "$verification_output/tests.trx" "$verification_output/translation.txt" \
    2>&1 | tee "$verification_output/test-results.txt"
capture_snapshot > "$verification_output/snapshot-after.txt"
if ! cmp -s "$verification_output/snapshot.txt" "$verification_output/snapshot-after.txt"; then
    printf 'Source files changed during verification; run the gate against a stable snapshot.\n' >&2
    exit 1
fi

printf '%s\n' passed > "$verification_output/status.txt"
printf '%s\n' "$verification_output" > "$verification_root/latest-success.txt.tmp"
mv -- "$verification_root/latest-success.txt.tmp" "$verification_root/latest-success.txt"
