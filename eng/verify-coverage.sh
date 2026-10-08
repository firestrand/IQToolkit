#!/usr/bin/env bash
set -euo pipefail
umask 077
ulimit -c 0

coverage_root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd -P)"
cd -- "$coverage_root"
if (( $# < 2 || $# > 3 )); then
    printf 'Usage: bash eng/verify-coverage.sh <baseline-descriptor.json> <passing-strict-run-directory> [<vstest-filter>]\n' >&2
    exit 1
fi
coverage_baseline="$(cd -- "$(dirname -- "$1")" && pwd -P)/$(basename -- "$1")"
coverage_strict="$(cd -- "$2" && pwd -P)"
coverage_filter="${3:-}"
mkdir -p -- "$coverage_root/artifacts/coverage/runs"
coverage_evidence="$(mktemp -d "$coverage_root/artifacts/coverage/runs/run.XXXXXXXX")"
coverage_temp=
coverage_tool=
coverage_pid=
coverage_group=
coverage_wait_exit=
coverage_cancelled=false
coverage_captured=false
coverage_prepared=false
coverage_terminal=false
coverage_sources=(global.json eng/verify-coverage.sh eng/CoverageCollector/Program.cs
    eng/CoverageCollector/Verifier.csproj.template eng/CoverageCollector/NuGet.config
    eng/verify-coverage-results.sh eng/CoverageVerifier/Program.cs
    eng/CoverageVerifier/Verifier.csproj.template eng/CoverageVerifier/NuGet.config)
printf '%s\n' running > "$coverage_evidence/status.txt"

capture_coverage_sources() (
    cd -- "$coverage_root" || return 1
    git rev-parse HEAD || return 1
    local path
    for path in "${coverage_sources[@]}" "$coverage_baseline"; do
        git hash-object -- "$path" || return 1
        printf '%s\n' "$path"
    done
)

coverage_members() {
    # Linux/procps supervision is a platform boundary. Exclude settled zombies;
    # retain start identity and executable name before any forced termination.
    ps -eo pid=,pgid=,sid=,lstart=,stat=,comm= \
        | awk -v group="$coverage_group" '$2 == group && $9 !~ /^Z/ { print }'
}

coverage_size_ok() {
    local total
    total="$(du -sb -- "$coverage_evidence" "${coverage_temp:-$coverage_evidence}" | awk '{sum += $1} END {print sum}')"
    [[ "$total" =~ ^[0-9]+$ ]] && (( total < 536870912 ))
}

settle_coverage_group() {
    local members count
    [[ -n "$coverage_group" ]] || return 0
    members="$(coverage_members)" || return 1
    if [[ -n "$members" ]]; then
        printf '%s\n' "$members" >> "$coverage_evidence/forced-members.txt"
        kill -TERM -- "-$coverage_group" || return 1
        for (( count = 0; count < 20; count++ )); do
            members="$(coverage_members)" || return 1
            [[ -n "$members" ]] || break
            sleep 0.1
        done
        if [[ -n "$members" ]]; then
            kill -KILL -- "-$coverage_group" || return 1
        fi
        for (( count = 0; count < 20; count++ )); do
            members="$(coverage_members)" || return 1
            [[ -n "$members" ]] || break
            sleep 0.1
        done
        [[ -z "$members" ]] || return 1
    fi
    if [[ -n "$coverage_pid" ]]; then
        coverage_wait_exit=0
        wait "$coverage_pid" 2>/dev/null || coverage_wait_exit=$?
    fi
    coverage_pid=
    coverage_group=
}

coverage_stage() {
    local stage="$1" result=0 process_result log members
    shift
    [[ "$coverage_cancelled" == false || "$coverage_terminal" == true ]] || return 130
    coverage_size_ok || return 1
    log="$coverage_evidence/$stage.log"
    printf '%s\n' "$stage" > "$coverage_evidence/stage.txt"
    printf '%q ' "$@" > "$coverage_evidence/$stage.command.txt"
    printf '\n' >> "$coverage_evidence/$stage.command.txt"
    # Bash monitor mode creates this job's separate group. timeout supervises
    # the group, including bootstrap SDK work before a helper exists.
    ( exec timeout --verbose --signal=TERM --kill-after=5s 120s "$@" ) > "$log" 2>&1 &
    coverage_pid=$!
    coverage_group=$coverage_pid
    coverage_wait_exit=
    printf '%s\n' "$coverage_pid" > "$coverage_evidence/$stage.pid.txt"
    coverage_members > "$coverage_evidence/$stage.identity.txt"
    while kill -0 "$coverage_pid" 2>/dev/null; do
        if [[ "$coverage_cancelled" == true && "$coverage_terminal" == false ]]; then
            result=130
            break
        fi
        if (( $(stat --format=%s -- "$log") > 16777216 )) || ! coverage_size_ok; then
            result=1
            break
        fi
        sleep 0.1
    done
    if (( result == 0 )); then
        wait "$coverage_pid" || result=$?
        process_result=$result
    fi
    members="$(coverage_members)" || result=1
    if [[ -n "$members" ]]; then
        settle_coverage_group || result=1
        process_result="${coverage_wait_exit:-unknown}"
        (( result != 0 )) || result=1
    else
        if [[ -z "${process_result:-}" ]]; then
            process_result=0
            wait "$coverage_pid" || process_result=$?
        fi
        coverage_pid=
        coverage_group=
    fi
    printf '%s\n' "$process_result" > "$coverage_evidence/$stage.process-exit.txt"
    cat -- "$log"
    (( $(stat --format=%s -- "$log") <= 16777216 )) && coverage_size_ok || result=1
    printf '%s\n' "$result" > "$coverage_evidence/$stage.exit.txt"
    return "$result"
}

finish_coverage() {
    local result=$? cleanup_failed=false
    trap - EXIT
    settle_coverage_group || cleanup_failed=true
    if [[ "$coverage_prepared" == true && -f "$coverage_tool" ]]; then
        coverage_terminal=true
        coverage_stage terminal-preservation "$coverage_dotnet" "$coverage_tool" preserve "$coverage_evidence" terminal \
            || cleanup_failed=true
    fi
    if [[ "$coverage_captured" == true ]]; then
        if ! capture_coverage_sources > "$coverage_evidence/source-after.txt" \
            || ! cmp -s "$coverage_evidence/source-before.txt" "$coverage_evidence/source-after.txt"; then
            cleanup_failed=true
        fi
    fi
    if [[ -n "$coverage_temp" && -d "$coverage_temp" ]]; then
        if [[ -f "$coverage_temp/tool/Verifier.csproj" ]] \
            && ! mv -- "$coverage_temp/tool/Verifier.csproj" "$coverage_temp/tool/Verifier.csproj.snapshot"; then
            cleanup_failed=true
        fi
        cd -- "$coverage_root"
        if ! mv -- "$coverage_temp" "$coverage_evidence/environment"; then
            cleanup_failed=true
        fi
    fi
    printf '%s\n' "$coverage_cancelled" > "$coverage_evidence/cancelled.txt"
    printf '%s\n' "$cleanup_failed" > "$coverage_evidence/cleanup-failed.txt"
    if [[ "$coverage_cancelled" == true || "$cleanup_failed" == true || -f "$coverage_evidence/forced-members.txt" ]]; then
        (( result != 0 )) || result=1
    fi
    if (( result == 0 )); then
        printf '%s\n' passed > "$coverage_evidence/status.txt"
        printf '%s\n' "$coverage_evidence" > "$coverage_root/artifacts/coverage/latest-success.txt.tmp"
        mv -- "$coverage_root/artifacts/coverage/latest-success.txt.tmp" "$coverage_root/artifacts/coverage/latest-success.txt"
    else
        printf '%s\n' failed > "$coverage_evidence/status.txt"
    fi
    printf 'Coverage collection evidence: %s\n' "$coverage_evidence"
    exit "$result"
}
trap finish_coverage EXIT
trap 'coverage_cancelled=true' INT TERM HUP

[[ "$(uname -s)" == Linux ]] || { printf 'Coverage supervision requires the Linux adapter.\n' >&2; exit 1; }
timeout --version > "$coverage_evidence/timeout-version.txt"
ps --version > "$coverage_evidence/ps-version.txt"
[[ "$coverage_filter" != *$'\n'* && ${#coverage_filter} -le 1024 ]] || exit 1
set -m
capture_coverage_sources > "$coverage_evidence/source-before.txt"
coverage_captured=true
coverage_temp="$(mktemp -d "${TMPDIR:-/tmp}/iqtoolkit-coverage.XXXXXXXX")"
coverage_temp="$(cd -- "$coverage_temp" && pwd -P)"
case "$coverage_temp/" in
    "$coverage_root/"*) printf 'Collector workspace must be outside the checkout.\n' >&2; exit 1 ;;
esac
printf '%s\n' "$coverage_temp" > "$coverage_evidence/temporary-directory.txt"
mkdir -- "$coverage_temp/tool" "$coverage_temp/source" "$coverage_temp/tmp" "$coverage_temp/cli-home"
for path in "${coverage_sources[@]}"; do
    mkdir -p -- "$coverage_temp/source/$(dirname -- "$path")"
    cp -- "$path" "$coverage_temp/source/$path"
    cmp -- "$path" "$coverage_temp/source/$path"
done
cp -- "$coverage_baseline" "$coverage_temp/baseline.json"
cmp -- "$coverage_baseline" "$coverage_temp/baseline.json"
cp -- "$coverage_temp/source/global.json" "$coverage_temp/global.json"
cp -- "$coverage_temp/source/eng/CoverageCollector/Program.cs" "$coverage_temp/tool/Program.cs"
cp -- "$coverage_temp/source/eng/CoverageCollector/Verifier.csproj.template" "$coverage_temp/tool/Verifier.csproj"
cp -- "$coverage_temp/source/eng/CoverageCollector/NuGet.config" "$coverage_temp/tool/NuGet.config"
coverage_dotnet="$(command -v dotnet)"
coverage_dotnet_root="$(cd -- "$(dirname -- "$coverage_dotnet")" && pwd -P)"
coverage_environment=(env -i "PATH=$coverage_dotnet_root:/usr/bin:/bin" "DOTNET_ROOT=$coverage_dotnet_root"
    "LANG=C.UTF-8" "LC_ALL=C.UTF-8" "DOTNET_CLI_HOME=$coverage_temp/cli-home"
    "DOTNET_CLI_TELEMETRY_OPTOUT=1" "DOTNET_NOLOGO=1" "DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1"
    "MSBUILDDISABLENODEREUSE=1" "DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER=1" "TMPDIR=$coverage_temp/tmp")
coverage_properties=(-p:ImportDirectoryBuildProps=false -p:ImportDirectoryBuildTargets=false
    -p:ManagePackageVersionsCentrally=false -p:ContinuousIntegrationBuild=true
    --disable-build-servers -p:UseSharedCompilation=false -nr:false -warnaserror)
cd -- "$coverage_temp"
coverage_stage sdk-info "${coverage_environment[@]}" "$coverage_dotnet" --info
coverage_stage restore "${coverage_environment[@]}" "$coverage_dotnet" restore tool/Verifier.csproj \
    --configfile tool/NuGet.config --packages "$coverage_temp/packages" "${coverage_properties[@]}"
coverage_stage build "${coverage_environment[@]}" "$coverage_dotnet" build tool/Verifier.csproj \
    -c Release --no-restore "${coverage_properties[@]}"
coverage_tool="$coverage_temp/tool/bin/Release/net10.0/Verifier.dll"
cd -- "$coverage_root"
# Mark preparation before invoking it so a partial prepare still gets terminal
# preservation whenever it managed to publish its initial state.
coverage_prepared=true
coverage_stage prepare "${coverage_environment[@]}" "$coverage_dotnet" "$coverage_tool" prepare \
    "$coverage_root" "$coverage_temp/baseline.json" "$coverage_strict" "$coverage_temp" "$coverage_evidence"
IFS= read -r coverage_adapter < "$coverage_evidence/collector-directory.txt"
coverage_command=("${coverage_environment[@]}" "$coverage_dotnet" test "$coverage_temp/shadow/IQToolkitTest.dll"
    --test-adapter-path "$coverage_adapter" --settings "$coverage_root/eng/coverage.runsettings"
    --collect 'Code Coverage;Format=Cobertura' --results-directory "$coverage_evidence/results"
    --logger 'trx;LogFileName=coverage.trx')
if [[ -n "$coverage_filter" ]]; then coverage_command+=(--filter "$coverage_filter"); fi
coverage_stage collect "${coverage_command[@]}"
coverage_stage collection-preservation "${coverage_environment[@]}" "$coverage_dotnet" "$coverage_tool" preserve "$coverage_evidence" collection
coverage_stage descriptor "${coverage_environment[@]}" "$coverage_dotnet" "$coverage_tool" descriptor "$coverage_evidence"
coverage_stage comparison "${coverage_environment[@]}" /bin/bash eng/verify-coverage-results.sh \
    "$coverage_temp/baseline.json" "$coverage_evidence/descriptor.json"
