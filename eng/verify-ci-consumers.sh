#!/usr/bin/env bash
set -euo pipefail

# Prepare only an outside-checkout copy. Reviewed source locks never change.
# Linux/GNU tooling is the initial CI profile; this is not an unlocked retry.
ci_root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd -P)"
cd -- "$ci_root"
if (( $# != 1 )); then
    printf 'Usage: bash eng/verify-ci-consumers.sh <fresh-package-directory>\n' >&2
    exit 1
fi
ci_feed="$(cd -- "$1" && pwd -P)"
ci_profiles=(IQToolkit IQToolkit.Data IQToolkit.Data.SQLite IQToolkit.Data.SqlClient IQToolkit.Data.MySqlClient IQToolkit.Data.Access)
ci_version=1.1.2-preview.1
ci_sources=(global.json .github/workflows/ci.yml eng/verify-ci-consumers.sh
    eng/prepare-consumer-lock.sh eng/verify-consumer.sh
    eng/prepare-independent-consumers.sh eng/verify-independent-consumers.sh
    eng/PackageConsumer/SQLiteConsumer.csproj.template eng/PackageConsumer/Program.cs
    eng/PackageConsumer/NuGet.config eng/PackageConsumer/packages.lock.json
    eng/IndependentConsumers/NuGet.config eng/IndependentConsumers/common.sh
    eng/IndependentConsumers/prepared-feed.sha256
    eng/IndependentConsumers/Verifier/Verifier.csproj.template
    eng/IndependentConsumers/Verifier/Program.cs eng/IndependentConsumers/Verifier/NuGet.config)
for ci_profile in "${ci_profiles[@]}"; do
    ci_sources+=("eng/IndependentConsumers/Profiles/$ci_profile/Consumer.csproj.template"
        "eng/IndependentConsumers/Profiles/$ci_profile/PublicSymbols.cs"
        "eng/IndependentConsumers/Profiles/$ci_profile/packages.lock.json")
done
mkdir -p -- "$ci_root/artifacts/ci-consumers/runs"
ci_evidence="$(mktemp -d "$ci_root/artifacts/ci-consumers/runs/run.XXXXXXXX")"
ci_shadow=
ci_captured=false
ci_completed=false
ci_interrupted=false
printf '%s\n' running > "$ci_evidence/status.txt"

capture_ci_inputs() {
    (cd -- "$ci_root" && sha256sum -- "${ci_sources[@]}")
    # Includes the actual archive name set as well as bytes, even on rejection.
    local archives=("$ci_feed"/*.nupkg)
    if [[ -f "${archives[0]}" ]]; then
        sha256sum -- "${archives[@]}"
    fi
}
finish_ci_consumers() {
    local result=$?
    trap - EXIT
    if [[ "$ci_completed" != true || "$ci_interrupted" == true ]]; then
        if (( result == 0 )); then
            result=1
        fi
    fi
    if [[ "$ci_captured" == true ]]; then
        if ! capture_ci_inputs > "$ci_evidence/inputs-after.sha256" \
            || ! cmp -s "$ci_evidence/inputs-before.sha256" "$ci_evidence/inputs-after.sha256"; then
            printf 'Original CI source or package-feed inputs changed.\n' >&2
            result=1
        fi
    fi
    if [[ -n "$ci_shadow" && -d "$ci_shadow" ]]; then
        # Existing child runners retain projects as .csproj.snapshot. Preserve
        # that disposition; never move a discoverable generated project back.
        local project project_hash
        while IFS= read -r -d '' project; do
            project_hash="$(sha256sum -- "$project")"
            if ! mv -- "$project" "$project.snapshot"; then
                result=1
            fi
            printf '%s\t%s\t%s\n' "${project#"$ci_shadow/"}" "${project#"$ci_shadow/"}.snapshot" "${project_hash%% *}" \
                >> "$ci_evidence/project-filename-mapping.tsv"
        done < <(find "$ci_shadow" -type f -name '*.csproj' -print0)
        cd -- "$ci_root"
        if ! mv -- "$ci_shadow" "$ci_evidence/environment"; then
            result=1
        fi
    fi
    if (( result == 0 )); then
        printf '%s\n' passed > "$ci_evidence/status.txt"
        printf '%s\n' "$ci_evidence" > "$ci_root/artifacts/ci-consumers/latest-success.txt.tmp"
        mv -- "$ci_root/artifacts/ci-consumers/latest-success.txt.tmp" "$ci_root/artifacts/ci-consumers/latest-success.txt"
    else
        printf '%s\n' failed > "$ci_evidence/status.txt"
    fi
    printf 'CI consumer evidence: %s\n' "$ci_evidence"
    exit "$result"
}
trap finish_ci_consumers EXIT
# Foreground pipelines settle before Bash runs this trapped signal.
trap 'ci_interrupted=true; exit 143' TERM
capture_ci_inputs > "$ci_evidence/inputs-before.sha256"
ci_captured=true
printf '%s\n' "$ci_feed" > "$ci_evidence/package-directory.txt"
archives=("$ci_feed"/*.nupkg)
if (( ${#archives[@]} != ${#ci_profiles[@]} )); then
    printf 'CI requires exactly six current toolkit packages.\n' >&2
    exit 1
fi
for ci_profile in "${ci_profiles[@]}"; do
    if [[ ! -f "$ci_feed/$ci_profile.$ci_version.nupkg" ]]; then
        printf 'Missing current CI package: %s.%s.nupkg\n' "$ci_profile" "$ci_version" >&2
        exit 1
    fi
done
ci_shadow="$(mktemp -d "${TMPDIR:-/tmp}/iqtoolkit-ci-consumers.XXXXXXXX")"
ci_shadow="$(cd -- "$ci_shadow" && pwd -P)"
case "$ci_shadow/" in
    "$ci_root/"*) printf 'CI shadow must be outside the checkout.\n' >&2; exit 1 ;;
esac
printf '%s\n' "$ci_shadow" > "$ci_evidence/temporary-directory.txt"
printf '%s\t%s\n' "$ci_shadow" "$ci_evidence/environment" > "$ci_evidence/retained-path-mapping.txt"
cp --parents -- "${ci_sources[@]}" "$ci_shadow/"
mkdir -- "$ci_evidence/source"
cp --parents -- "${ci_sources[@]}" "$ci_evidence/source/"
mkdir -- "$ci_shadow/feed" "$ci_evidence/tested-packages" "$ci_evidence/reviewed-locks" "$ci_evidence/prepared-locks"
cp -- "${archives[@]}" "$ci_shadow/feed/"
cp -- "${archives[@]}" "$ci_evidence/tested-packages/"
# Check every copied source/archive before restore starts, including immutable
# reviewed-lock copies used for comparison after preparation mutates shadow locks.
for ci_source in "${ci_sources[@]}"; do
    cmp -- "$ci_source" "$ci_shadow/$ci_source"
    cmp -- "$ci_source" "$ci_evidence/source/$ci_source"
done
for ci_archive in "${archives[@]}"; do
    cmp -- "$ci_archive" "$ci_shadow/feed/$(basename -- "$ci_archive")"
    cmp -- "$ci_archive" "$ci_evidence/tested-packages/$(basename -- "$ci_archive")"
done
cp -- "$ci_shadow/eng/PackageConsumer/packages.lock.json" "$ci_evidence/reviewed-locks/SQLite-runtime.json"
for ci_profile in "${ci_profiles[@]}"; do
    cp -- "$ci_shadow/eng/IndependentConsumers/Profiles/$ci_profile/packages.lock.json" "$ci_evidence/reviewed-locks/$ci_profile.json"
done
(
    cd -- "$ci_shadow"
    sha256sum -- "${ci_sources[@]}"
) > "$ci_evidence/copied-sources.sha256"
(
    cd -- "$ci_evidence/tested-packages"
    sha256sum -- *.nupkg
) > "$ci_evidence/tested-packages.sha256"

bash "$ci_shadow/eng/prepare-independent-consumers.sh" "$ci_shadow/feed" 2>&1 | tee "$ci_evidence/prepare-independent.log"
NUGET_HTTP_CACHE_PATH="$ci_shadow/cache/sqlite-prepare/http" \
NUGET_PLUGINS_CACHE_PATH="$ci_shadow/cache/sqlite-prepare/plugins" \
    bash "$ci_shadow/eng/prepare-consumer-lock.sh" "$ci_shadow/feed" 2>&1 | tee "$ci_evidence/prepare-sqlite.log"
ci_preparation="$(cat "$ci_shadow/artifacts/independent-consumers/latest-prepare.txt")"
ci_verifier="$ci_preparation/environment/tool/bin/Release/net10.0/Verifier.dll"
printf '%s\n' "${ci_verifier#"$ci_shadow/"}" > "$ci_evidence/verifier-relative-path.txt"
cp -- "$ci_shadow/eng/PackageConsumer/packages.lock.json" "$ci_evidence/prepared-locks/SQLite-runtime.json"
for ci_profile in "${ci_profiles[@]}"; do
    cp -- "$ci_shadow/eng/IndependentConsumers/Profiles/$ci_profile/packages.lock.json" "$ci_evidence/prepared-locks/$ci_profile.json"
done
# All seven graph reviews finish before any locked consumer build/execution.
for ci_lock in "$ci_evidence/reviewed-locks/"*.json; do
    dotnet "$ci_verifier" lock-delta "$ci_lock" \
        "$ci_evidence/prepared-locks/$(basename -- "$ci_lock")" "$ci_shadow/feed" \
        2>&1 | tee "$ci_evidence/$(basename -- "$ci_lock").delta.log"
done
bash "$ci_shadow/eng/verify-independent-consumers.sh" "$ci_shadow/feed" 2>&1 | tee "$ci_evidence/verify-independent.log"
NUGET_HTTP_CACHE_PATH="$ci_shadow/cache/sqlite-verify/http" \
NUGET_PLUGINS_CACHE_PATH="$ci_shadow/cache/sqlite-verify/plugins" \
    bash "$ci_shadow/eng/verify-consumer.sh" "$ci_shadow/feed" 2>&1 | tee "$ci_evidence/verify-sqlite.log"
# Child runners enforce their own locks, copies and source/asset provenance.
# Recheck the outer exact feed and the original checkout on EXIT as well.
for ci_archive in "$ci_evidence/tested-packages/"*.nupkg; do
    cmp -- "$ci_archive" "$ci_shadow/feed/$(basename -- "$ci_archive")"
done
ci_completed=true
