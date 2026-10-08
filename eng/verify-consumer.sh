#!/usr/bin/env bash
set -euo pipefail

task_root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
cd -- "$task_root"
if (( $# != 1 )); then
    printf 'Usage: bash eng/verify-consumer.sh <tested-package-directory>\n' >&2
    exit 1
fi
consumer_feed="$(cd -- "$1" && pwd)"
consumer_template="$task_root/eng/PackageConsumer"
if [[ ! -f "$consumer_template/packages.lock.json" ]]; then
    printf 'Prepare and review the consumer lock explicitly before locked verification.\n' >&2
    exit 1
fi
mkdir -p -- "$task_root/artifacts/package-consumer/runs"
consumer_evidence="$(mktemp -d "$task_root/artifacts/package-consumer/runs/run.XXXXXXXX")"
consumer_temp="$(mktemp -d "${TMPDIR:-/tmp}/iqtoolkit-consumer.XXXXXXXX")"
printf '%s\n' running > "$consumer_evidence/status.txt"
finish_consumer() {
    local exit_code=$?
    if (( exit_code != 0 )); then
        printf '%s\n' failed > "$consumer_evidence/status.txt"
    fi
    if [[ -f "$consumer_temp/obj/project.assets.json" ]]; then
        cp -- "$consumer_temp/obj/project.assets.json" "$consumer_evidence/project.assets.json"
    fi
    # Retain the owned environment as evidence and release its temporary path.
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
        printf '%s\n' passed > "$consumer_evidence/status.txt"
        printf '%s\n' "$consumer_evidence" > "$task_root/artifacts/package-consumer/latest-success.txt.tmp"
        mv -- "$task_root/artifacts/package-consumer/latest-success.txt.tmp" "$task_root/artifacts/package-consumer/latest-success.txt"
    else
        printf '%s\n' failed > "$consumer_evidence/status.txt"
    fi
    printf 'Consumer evidence: %s\n' "$consumer_evidence"
    exit "$exit_code"
}
trap finish_consumer EXIT

printf '%s\n' "$consumer_feed" > "$consumer_evidence/package-directory.txt"
printf '%s\n' "$consumer_temp" > "$consumer_evidence/temporary-directory.txt"
capture_consumer_inputs() {
    local source_path
    for source_path in "$consumer_template/SQLiteConsumer.csproj.template" "$consumer_template/Program.cs" \
        "$consumer_template/NuGet.config" "$consumer_template/packages.lock.json" \
        "$task_root/global.json" "$task_root/eng/verify-consumer.sh" "$consumer_feed"/*.nupkg; do
        git hash-object -- "$source_path"
        printf '%s\n' "$source_path"
    done
}
capture_consumer_inputs > "$consumer_evidence/inputs-before.txt"
mkdir -- "$consumer_temp/feed"
cp -- "$consumer_feed"/*.nupkg "$consumer_temp/feed/"
cp -- "$consumer_template/SQLiteConsumer.csproj.template" "$consumer_temp/SQLiteConsumer.csproj"
cp -- "$consumer_template/Program.cs" \
    "$consumer_template/NuGet.config" "$consumer_template/packages.lock.json" \
    "$task_root/global.json" "$task_root/eng/verify-consumer.sh" "$consumer_temp/"
cp -- "$consumer_temp/SQLiteConsumer.csproj" "$consumer_evidence/SQLiteConsumer.csproj.snapshot"
cp -- "$consumer_temp/Program.cs" \
    "$consumer_temp/NuGet.config" "$consumer_temp/packages.lock.json" \
    "$consumer_temp/global.json" "$consumer_temp/verify-consumer.sh" "$consumer_evidence/"
while IFS= read -r expected_hash && IFS= read -r source_path; do
    if [[ "$source_path" == "$consumer_feed/"* ]]; then
        copied_path="$consumer_temp/feed/$(basename -- "$source_path")"
    elif [[ "$source_path" == "$consumer_template/SQLiteConsumer.csproj.template" ]]; then
        copied_path="$consumer_temp/SQLiteConsumer.csproj"
    else
        copied_path="$consumer_temp/$(basename -- "$source_path")"
    fi
    if [[ "$(git hash-object -- "$copied_path")" != "$expected_hash" ]]; then
        printf 'Copied consumer/package bytes differ from the captured inputs: %s\n' "$source_path" >&2
        exit 1
    fi
done < "$consumer_evidence/inputs-before.txt"
printf '%s\n' 'SQLiteConsumer.csproj.template -> outside SQLiteConsumer.csproj -> retained SQLiteConsumer.csproj.snapshot (identical bytes)' > "$consumer_evidence/project-filenames.txt"
printf '%s\n' 'All copied consumer/feed identities match the pre-copy snapshot.' > "$consumer_evidence/copied-input-inspection.txt"
cd -- "$consumer_temp"
dotnet --info > "$consumer_evidence/dotnet-info.txt"
dotnet restore SQLiteConsumer.csproj --configfile NuGet.config --packages "$consumer_temp/packages" \
    --locked-mode -p:Configuration=Release -p:ContinuousIntegrationBuild=true -warnaserror \
    2>&1 | tee "$consumer_evidence/restore.log"
if ! cmp -s packages.lock.json "$consumer_evidence/packages.lock.json"; then
    printf 'Consumer restore modified the approved lock.\n' >&2
    exit 1
fi
dotnet build SQLiteConsumer.csproj -c Release --no-restore -t:Rebuild \
    -p:ContinuousIntegrationBuild=true -warnaserror 2>&1 | tee "$consumer_evidence/build.log"
dotnet run --project SQLiteConsumer.csproj -c Release --no-build --no-restore \
    2>&1 | tee "$consumer_evidence/runtime.log"
cd -- "$task_root"
capture_consumer_inputs > "$consumer_evidence/inputs-after.txt"
if ! cmp -s "$consumer_evidence/inputs-before.txt" "$consumer_evidence/inputs-after.txt"; then
    printf 'Consumer source or package inputs changed during verification.\n' >&2
    exit 1
fi
