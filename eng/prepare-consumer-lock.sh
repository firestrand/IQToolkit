#!/usr/bin/env bash
set -euo pipefail

task_root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
cd -- "$task_root"
if (( $# != 1 )); then
    printf 'Usage: bash eng/prepare-consumer-lock.sh <tested-package-directory>\n' >&2
    exit 1
fi
consumer_feed="$(cd -- "$1" && pwd)"
consumer_template="$task_root/eng/PackageConsumer"
mkdir -p -- "$task_root/artifacts/package-consumer/lock-preparation-runs"
consumer_evidence="$(mktemp -d "$task_root/artifacts/package-consumer/lock-preparation-runs/run.XXXXXXXX")"
consumer_temp="$(mktemp -d "${TMPDIR:-/tmp}/iqtoolkit-consumer-lock.XXXXXXXX")"
printf '%s\n' running > "$consumer_evidence/status.txt"
finish_preparation() {
    local exit_code=$?
    if (( exit_code != 0 )); then
        printf '%s\n' failed > "$consumer_evidence/status.txt"
    fi
    cd -- "$task_root"
    if [[ -f "$consumer_temp/SQLiteConsumer.csproj" ]]; then
        if ! mv -- "$consumer_temp/SQLiteConsumer.csproj" "$consumer_temp/SQLiteConsumer.csproj.snapshot"; then
            exit_code=1
        fi
    fi
    if mv -- "$consumer_temp" "$consumer_evidence/environment"; then
        printf '%s\n' "$consumer_evidence/environment" > "$consumer_evidence/retained-environment.txt"
    else
        exit_code=1
    fi
    if (( exit_code == 0 )); then
        printf '%s\n' prepared > "$consumer_evidence/status.txt"
    else
        printf '%s\n' failed > "$consumer_evidence/status.txt"
    fi
    printf 'Lock preparation evidence: %s\n' "$consumer_evidence"
    exit "$exit_code"
}
trap finish_preparation EXIT
capture_inputs() {
    local source_path
    for source_path in "$consumer_template/SQLiteConsumer.csproj.template" "$consumer_template/NuGet.config" \
        "$task_root/global.json" "$task_root/eng/prepare-consumer-lock.sh" "$consumer_feed"/*.nupkg; do
        git hash-object -- "$source_path"
        printf '%s\n' "$source_path"
    done
}
capture_inputs > "$consumer_evidence/inputs-before.txt"
printf '%s\n' "$consumer_feed" > "$consumer_evidence/package-directory.txt"
printf '%s\n' "$consumer_temp" > "$consumer_evidence/temporary-directory.txt"
mkdir -- "$consumer_temp/feed"
cp -- "$consumer_feed"/*.nupkg "$consumer_temp/feed/"
cp -- "$consumer_template/SQLiteConsumer.csproj.template" "$consumer_temp/SQLiteConsumer.csproj"
cp -- "$consumer_template/NuGet.config" \
    "$task_root/global.json" "$task_root/eng/prepare-consumer-lock.sh" "$consumer_temp/"
while IFS= read -r expected_hash && IFS= read -r source_path; do
    if [[ "$source_path" == "$consumer_feed/"* ]]; then
        copied_path="$consumer_temp/feed/$(basename -- "$source_path")"
    elif [[ "$source_path" == "$consumer_template/SQLiteConsumer.csproj.template" ]]; then
        copied_path="$consumer_temp/SQLiteConsumer.csproj"
    else
        copied_path="$consumer_temp/$(basename -- "$source_path")"
    fi
    if [[ "$(git hash-object -- "$copied_path")" != "$expected_hash" ]]; then
        printf 'Lock preparation copied different input bytes: %s\n' "$source_path" >&2
        exit 1
    fi
done < "$consumer_evidence/inputs-before.txt"
printf '%s\n' 'SQLiteConsumer.csproj.template -> outside SQLiteConsumer.csproj -> retained SQLiteConsumer.csproj.snapshot (identical bytes)' > "$consumer_evidence/project-filenames.txt"
cd -- "$consumer_temp"
dotnet restore SQLiteConsumer.csproj --configfile NuGet.config --packages "$consumer_temp/packages" \
    -p:Configuration=Release -p:ContinuousIntegrationBuild=true -warnaserror \
    2>&1 | tee "$consumer_evidence/restore.log"
cp -- packages.lock.json obj/project.assets.json "$consumer_evidence/"
cd -- "$task_root"
capture_inputs > "$consumer_evidence/inputs-after.txt"
if ! cmp -s "$consumer_evidence/inputs-before.txt" "$consumer_evidence/inputs-after.txt"; then
    printf 'Source or package inputs changed during lock preparation.\n' >&2
    exit 1
fi
# This explicit preparation command writes the actual resolved lock. Review its
# dependency delta before invoking the separate locked verification command.
cp -- "$consumer_temp/packages.lock.json" "$consumer_template/packages.lock.json"
