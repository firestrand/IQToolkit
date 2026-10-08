#!/usr/bin/env bash
set -euo pipefail
set +m
umask 077
ulimit -c 0

final_root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd -P)"
cd -- "$final_root"
if (( $# != 1 )); then
    printf 'Usage: bash eng/verify-final.sh <retained-1.1.1-api-baseline-packages>\n' >&2
    exit 1
fi
final_baseline="$(cd -- "$1" && pwd -P)"
mkdir -p -- "$final_root/artifacts/final-verification/runs"
final_evidence="$(mktemp -d "$final_root/artifacts/final-verification/runs/run.XXXXXXXX")"
final_temp= final_pid= final_group= final_wait_exit=
final_cancelled=false final_terminal=false final_captured=false
final_profile="$final_root/eng/FinalVerification/profile.json"
final_areas=()
for final_kind in api-compatibility package-symbols ci-consumers independent-consumers package-consumer verification test-results coverage coverage-results; do
    final_areas+=("$final_root/artifacts/$final_kind/runs")
done
: > "$final_evidence/preexisting-directories.txt"
for final_area in "${final_areas[@]}"; do
    [[ -d "$final_area" ]] || continue
    for final_path in "$final_area"/*; do
        [[ ! -d "$final_path" ]] || printf '%s\n' "$final_path" >> "$final_evidence/preexisting-directories.txt"
    done
done
printf '%s\n' running > "$final_evidence/status.txt"

capture_final_sources() (
    cd -- "$final_root" || return 1
    local path
    git ls-files --cached --others --exclude-standard -z > "$final_evidence/source-paths.txt" || return 1
    [[ -s "$final_evidence/source-paths.txt" ]] || return 1
    while IFS= read -r -d '' path; do
        if [[ -f "$path" ]]; then
            git hash-object -- "$path" || return 1
            printf '%s\n' "$path"
        fi
    done < "$final_evidence/source-paths.txt"
)


capture_final_binaries() (
    cd -- "$final_root" || return 1
    local paths=() project path
    for project in IQToolkit IQToolkit.Data IQToolkit.Data.Access IQToolkit.Data.MySqlClient IQToolkit.Data.SQLite IQToolkit.Data.SqlClient IQToolkitTest Test; do
        paths+=("$project/bin/Release" "$project/obj/Release")
    done
    rg --files --hidden --no-ignore -0 -g '*.dll' -g '*.pdb' "${paths[@]}" \
        | sort -z > "$final_evidence/binary-paths.txt" || return 1
    [[ -s "$final_evidence/binary-paths.txt" ]] || return 1
    while IFS= read -r -d '' path; do
        [[ ! -L "$path" ]] || return 1
        sha256sum -- "$path" || return 1
    done < "$final_evidence/binary-paths.txt"
)

final_members() {
    # Linux/procps supervision is a platform boundary. Exclude settled zombies;
    # retain start identity and executable name before any forced termination.
    ps -eo pid=,pgid=,sid=,lstart=,stat=,comm= \
        | awk -v session="$final_group" '$3 == session && $9 !~ /^Z/ { print }'
}

final_tree_size() {
    local base="$1" part attempt result
    for (( attempt = 1; attempt <= 3; attempt++ )); do
        result=0
        part="$(du -sb -- "$base" 2> "$final_evidence/storage-scan-error.txt")" || result=$?
        if (( result == 0 )); then
            part="${part%%$'\t'*}"
            [[ "$part" =~ ^[0-9]+$ ]] || return 1
            printf '%s\n' "$part"
            return 0
        fi
        printf 'attempt=%s exit=%s tree=%s\n' "$attempt" "$result" "$base" >> "$final_evidence/storage-retries.log"
        cat -- "$final_evidence/storage-scan-error.txt" >> "$final_evidence/storage-retries.log"
        (( $(stat --format=%s -- "$final_evidence/storage-retries.log") <= 16777216 )) || return 1
        # Native SDK files may disappear during enumeration. A retry still
        # requires a successful complete scan; no subtree is excluded.
        sleep 0.02
    done
    return 1
}

final_size_ok() {
    local total=0 part base path
    local owned=("$final_evidence")
    [[ -z "$final_temp" ]] || owned+=("$final_temp")
    for base in "${owned[@]}"; do
        part="$(final_tree_size "$base")" || return 1
        [[ "$part" =~ ^[0-9]+$ ]] || return 1
        total=$(( total + part ))
    done
    for base in "${final_areas[@]}"; do
        [[ -d "$base" ]] || continue
        for path in "$base"/*; do
            [[ -d "$path" ]] || continue
            if ! rg -Fxq -- "$path" "$final_evidence/preexisting-directories.txt"; then
                part="$(final_tree_size "$path")" || return 1
                [[ "$part" =~ ^[0-9]+$ ]] || return 1
                total=$(( total + part ))
            fi
        done
    done
    (( total < 3221225472 ))
}

settle_final_group() {
    local members count group
    [[ -n "$final_group" ]] || return 0
    members="$(final_members)" || return 1
    if [[ -n "$members" ]]; then
        printf '%s\n' "$members" >> "$final_evidence/forced-members.txt"
        while IFS= read -r group; do
            kill -TERM -- "-$group" 2>/dev/null || [[ -z "$(final_members)" ]] || return 1
        done < <(printf '%s\n' "$members" | awk '{print $2}' | sort -u)
        for (( count = 0; count < 20; count++ )); do
            members="$(final_members)" || return 1
            [[ -n "$members" ]] || break
            sleep 0.1
        done
        if [[ -n "$members" ]]; then
            while IFS= read -r group; do
                kill -KILL -- "-$group" 2>/dev/null || [[ -z "$(final_members)" ]] || return 1
            done < <(printf '%s\n' "$members" | awk '{print $2}' | sort -u)
        fi
        for (( count = 0; count < 20; count++ )); do
            members="$(final_members)" || return 1
            [[ -n "$members" ]] || break
            sleep 0.1
        done
        [[ -z "$members" ]] || return 1
    fi
    if [[ -n "$final_pid" ]]; then
        final_wait_exit=0
        wait "$final_pid" 2>/dev/null || final_wait_exit=$?
    fi
    final_pid=
    final_group=
}

final_stage() {
    local stage="$1" result=0 process_result log members deadline=120
    shift
    [[ "$stage" != coverage ]] || deadline=300
    [[ "$final_cancelled" == false || "$final_terminal" == true ]] || return 130
    final_size_ok || return 1
    log="$final_evidence/$stage.log"
    printf '%s\n' "$stage" > "$final_evidence/stage.txt"
    printf '%q ' "$@" > "$final_evidence/$stage.command.txt"
    printf '\n' >> "$final_evidence/$stage.command.txt"
    # Without shell job control, setsid remains the session leader at $!.
    # Track every group in that session, including nested producer jobs.
    ( exec setsid --wait timeout --verbose --signal=TERM --kill-after=5s "${deadline}s" "$@" ) > "$log" 2>&1 &
    final_pid=$!
    final_group=$final_pid
    final_wait_exit=
    printf '%s\n' "$final_pid" > "$final_evidence/$stage.pid.txt"
    final_members > "$final_evidence/$stage.identity.txt"
    while kill -0 "$final_pid" 2>/dev/null; do
        if [[ "$final_cancelled" == true && "$final_terminal" == false ]]; then
            result=130
            break
        fi
        if (( $(stat --format=%s -- "$log") > 16777216 )) || ! final_size_ok; then
            result=1
            break
        fi
        sleep 0.1
    done
    if (( result == 0 )); then
        wait "$final_pid" || result=$?
        process_result=$result
    fi
    members="$(final_members)" || result=1
    if [[ -n "$members" ]]; then
        settle_final_group || result=1
        process_result="${final_wait_exit:-unknown}"
        (( result != 0 )) || result=1
    else
        if [[ -z "${process_result:-}" ]]; then
            process_result=0
            wait "$final_pid" || process_result=$?
        fi
        final_pid=
        final_group=
    fi
    printf '%s\n' "$process_result" > "$final_evidence/$stage.process-exit.txt"
    cat -- "$log"
    (( $(stat --format=%s -- "$log") <= 16777216 )) && final_size_ok || result=1
    printf '%s\n' "$result" > "$final_evidence/$stage.exit.txt"
    return "$result"
}

finish_final() {
    local result=$? cleanup_failed=false
    trap - EXIT
    settle_final_group || cleanup_failed=true
    if [[ "$final_captured" == true ]]; then
        if ! capture_final_sources > "$final_evidence/source-after.txt" \
            || ! cmp -s "$final_evidence/source-before.txt" "$final_evidence/source-after.txt"; then
            cleanup_failed=true
        fi
        if ! capture_final_binaries > "$final_evidence/binaries-after.sha256" \
            || ! cmp -s "$final_evidence/binaries-before.sha256" "$final_evidence/binaries-after.sha256"; then
            cleanup_failed=true
        fi
    fi
    if [[ -n "$final_temp" && -d "$final_temp" ]]; then
        local project project_hash
        if ! find "$final_temp" -type f -name '*.csproj' -print0 > "$final_evidence/project-paths.txt"; then
            cleanup_failed=true
        else
            while IFS= read -r -d '' project; do
                project_hash="$(sha256sum -- "$project")" || { cleanup_failed=true; continue; }
                if [[ -e "$project.snapshot" ]] || ! mv -- "$project" "$project.snapshot"; then
                    cleanup_failed=true
                    continue
                fi
                printf '%s\t%s\t%s\n' "${project#"$final_temp/"}" "${project#"$final_temp/"}.snapshot" "${project_hash%% *}" \
                    >> "$final_evidence/project-filename-mapping.tsv"
            done < "$final_evidence/project-paths.txt"
        fi
        cd -- "$final_root"
        if ! mv -- "$final_temp" "$final_evidence/environment"; then
            cleanup_failed=true
        else
            final_temp=
        fi
    fi
    final_size_ok || cleanup_failed=true
    printf '%s\n' "$final_cancelled" > "$final_evidence/cancelled.txt"
    printf '%s\n' "$cleanup_failed" > "$final_evidence/cleanup-failed.txt"
    if [[ "$final_cancelled" == true || "$cleanup_failed" == true || -f "$final_evidence/forced-members.txt" ]]; then
        (( result != 0 )) || result=1
    fi
    # Current required fact/touched-file/seed classifiers are incomplete.
    # No final-success pointer is supported by this precursor.
    (( result != 0 )) || result=1
    printf '%s\n' rejected > "$final_evidence/status.txt"
    printf 'Final verification evidence: %s\n' "$final_evidence"
    exit "$result"
}
trap finish_final EXIT
trap 'final_cancelled=true' INT TERM HUP
[[ "$(uname -s)" == Linux ]] || { printf 'Final supervision requires the Linux adapter.\n' >&2; exit 1; }
timeout --version > "$final_evidence/timeout-version.txt"
ps --version > "$final_evidence/ps-version.txt"
command -v rg > "$final_evidence/rg-path.txt"
setsid --version > "$final_evidence/setsid-version.txt"
capture_final_sources > "$final_evidence/source-before.txt"
capture_final_binaries > "$final_evidence/binaries-before.sha256"
final_captured=true
# Native inventories are authenticated against the reviewed baseline in the helper.
git cat-file commit 60ddef60d4b635c4b24b78bff4327e3f3ff9ca5e > "$final_evidence/baseline-commit.txt"
git ls-tree -r -z --full-tree 60ddef60d4b635c4b24b78bff4327e3f3ff9ca5e > "$final_evidence/baseline-tree.txt"
final_temp="$(mktemp -d "${TMPDIR:-/tmp}/iqtoolkit-final.XXXXXXXX")"
final_temp="$(cd -- "$final_temp" && pwd -P)"
case "$final_temp/" in "$final_root/"*) printf 'Final helper must build outside the checkout.\n' >&2; exit 1 ;; esac
printf '%s\n' "$final_temp" > "$final_evidence/temporary-directory.txt"
mkdir -- "$final_temp/tool" "$final_temp/source" "$final_temp/tmp" "$final_temp/cli-home" "$final_evidence/preflight-inputs" "$final_evidence/assessment-inputs"
for final_source in global.json eng/verify-final.sh eng/FinalVerification/Program.cs eng/FinalVerification/Classifiers.cs eng/FinalVerification/CompilationContinuity.cs eng/FinalVerification/CommandInitializationContinuity.cs eng/FinalVerification/MathIdentifierContinuity.cs eng/FinalVerification/NullableColumnContinuity.cs eng/FinalVerification/CanonicalPublicationContinuity.cs eng/FinalVerification/PackageIndependentEvidence.cs eng/FinalVerification/PackageNativeConsumerEvidence.cs eng/FinalVerification/CacheSeedEvidence.cs eng/FinalVerification/fact-bindings.json eng/FinalVerification/coverage-layers.json eng/FinalVerification/Verifier.csproj.template eng/FinalVerification/NuGet.config eng/FinalVerification/profile.json; do
    mkdir -p -- "$final_temp/source/$(dirname -- "$final_source")"
    cp -- "$final_source" "$final_temp/source/$final_source"
    cmp -- "$final_source" "$final_temp/source/$final_source"
done
cp -- "$final_temp/source/global.json" "$final_temp/global.json"
cp -- "$final_temp/source/eng/FinalVerification/Program.cs" "$final_temp/tool/Program.cs"
cp -- "$final_temp/source/eng/FinalVerification/Classifiers.cs" "$final_temp/tool/Classifiers.cs"
cp -- "$final_temp/source/eng/FinalVerification/CompilationContinuity.cs" "$final_temp/tool/CompilationContinuity.cs"
cp -- "$final_temp/source/eng/FinalVerification/CommandInitializationContinuity.cs" "$final_temp/tool/CommandInitializationContinuity.cs"
cp -- "$final_temp/source/eng/FinalVerification/MathIdentifierContinuity.cs" "$final_temp/tool/MathIdentifierContinuity.cs"
cp -- "$final_temp/source/eng/FinalVerification/NullableColumnContinuity.cs" "$final_temp/tool/NullableColumnContinuity.cs"
cp -- "$final_temp/source/eng/FinalVerification/CanonicalPublicationContinuity.cs" "$final_temp/tool/CanonicalPublicationContinuity.cs"
cp -- "$final_temp/source/eng/FinalVerification/PackageIndependentEvidence.cs" "$final_temp/tool/PackageIndependentEvidence.cs"
cp -- "$final_temp/source/eng/FinalVerification/PackageNativeConsumerEvidence.cs" "$final_temp/tool/PackageNativeConsumerEvidence.cs"
cp -- "$final_temp/source/eng/FinalVerification/CacheSeedEvidence.cs" "$final_temp/tool/CacheSeedEvidence.cs"
cp -- "$final_temp/source/eng/FinalVerification/Verifier.csproj.template" "$final_temp/tool/Verifier.csproj"
cp -- "$final_temp/source/eng/FinalVerification/NuGet.config" "$final_temp/tool/NuGet.config"
final_dotnet="$(command -v dotnet)"
export DOTNET_CLI_HOME="$final_temp/cli-home" TMPDIR="$final_temp/tmp"
export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1 DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
export MSBUILDDISABLENODEREUSE=1 DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER=1 UseSharedCompilation=false GITHUB_ACTIONS=true
final_properties=(-p:ImportDirectoryBuildProps=false -p:ImportDirectoryBuildTargets=false
    -p:ManagePackageVersionsCentrally=false -p:ContinuousIntegrationBuild=true
    --disable-build-servers -p:UseSharedCompilation=false -nr:false -warnaserror)
final_stage restore "$final_dotnet" restore "$final_temp/tool/Verifier.csproj" --configfile "$final_temp/tool/NuGet.config" "${final_properties[@]}"
final_stage build "$final_dotnet" build "$final_temp/tool/Verifier.csproj" -c Release --no-restore "${final_properties[@]}"
final_tool="$final_temp/tool/bin/Release/net10.0/Verifier.dll"
final_stage preflight "$final_dotnet" "$final_tool" preflight "$final_root" "$final_profile" "$final_evidence/preflight-inputs"

producer_directory() {
    local stage="$1" prefix="$2" area="$3" value
    mapfile -t final_matches < <(sed -n "s/^$prefix//p" "$final_evidence/$stage.log")
    (( ${#final_matches[@]} == 1 )) || return 1
    value="${final_matches[0]}"
    [[ "$(dirname -- "$value")" == "$final_root/artifacts/$area/runs" && -d "$value" ]] || return 1
    if rg -Fxq -- "$value" "$final_evidence/preexisting-directories.txt"; then return 1; fi
    [[ "$(cat -- "$value/status.txt")" == passed ]] || return 1
    printf '%s\n' "$value"
}
final_stage api /bin/bash eng/verify-api.sh "$final_baseline"
final_api="$(producer_directory api 'API compatibility evidence: ' api-compatibility)"
final_feed="$final_api/packages"
final_stage packages "$final_dotnet" msbuild eng/VerifyPackages.proj "-p:PackageDirectory=$final_feed" -p:ContinuousIntegrationBuild=true -warnaserror
final_stage symbols /bin/bash eng/verify-symbols.sh "$final_feed"
final_symbols="$(producer_directory symbols 'Symbol provenance evidence: ' package-symbols)"
final_stage consumers /bin/bash eng/verify-ci-consumers.sh "$final_feed"
final_consumers="$(producer_directory consumers 'CI consumer evidence: ' ci-consumers)"
final_stage strict /bin/bash eng/verify.sh
final_strict="$(producer_directory strict 'Verification evidence: ' verification)"
IFS= read -r final_anchor < "$final_evidence/preflight-inputs/active-descriptor.txt"
[[ "$final_anchor" == "$final_root/artifacts/nullable-column/coverage.843ik22m/collection/descriptor.json" && -f "$final_anchor" ]] || exit 1
final_stage coverage /bin/bash eng/verify-coverage.sh "$final_anchor" "$final_strict"
final_coverage="$(producer_directory coverage 'Coverage collection evidence: ' coverage)"
final_stage assessment "$final_dotnet" "$final_tool" assess "$final_root" "$final_profile" "$final_evidence/assessment-inputs" \
    "$final_api" "$final_symbols" "$final_consumers" "$final_strict" "$final_coverage" \
    "$final_evidence/baseline-tree.txt" "$final_evidence/source-before.txt" "$final_evidence/baseline-commit.txt"
