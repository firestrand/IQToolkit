#!/usr/bin/env bash
set -euo pipefail
umask 077

results_root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd -P)"
cd -- "$results_root"
if (( $# != 2 )); then
    printf 'Usage: bash eng/verify-test-results.sh <trx-report> <translation-output>\n' >&2
    exit 1
fi
results_trx="$(cd -- "$(dirname -- "$1")" && pwd -P)/$(basename -- "$1")"
results_translation="$(cd -- "$(dirname -- "$2")" && pwd -P)/$(basename -- "$2")"
results_sources=(global.json eng/verify-test-results.sh
    eng/TestResultsVerifier/Verifier.csproj.template
    eng/TestResultsVerifier/NuGet.config eng/TestResultsVerifier/Program.cs)
mkdir -p -- "$results_root/artifacts/test-results/runs"
results_evidence="$(mktemp -d "$results_root/artifacts/test-results/runs/run.XXXXXXXX")"
results_temp=
results_captured=false
printf '%s\n' running > "$results_evidence/status.txt"
capture_results_inputs() (
    cd -- "$results_root" || return 1
    git rev-parse HEAD || return 1
    local path
    for path in "${results_sources[@]}" "$results_trx" "$results_translation"; do
        git hash-object -- "$path" || return 1
        printf '%s\n' "$path"
    done
)
finish_results() {
    local result=$?
    trap - EXIT
    if [[ "$results_captured" == true ]]; then
        if ! capture_results_inputs > "$results_evidence/inputs-after.txt" \
            || ! cmp -s "$results_evidence/inputs-before.txt" "$results_evidence/inputs-after.txt"; then
            printf 'Test result inputs changed during verification.\n' >&2
            result=1
        fi
    fi
    if [[ -n "$results_temp" && -d "$results_temp" ]]; then
        if [[ -f "$results_temp/tool/Verifier.csproj" ]] \
            && ! mv -- "$results_temp/tool/Verifier.csproj" "$results_temp/tool/Verifier.csproj.snapshot"; then
            result=1
        fi
        cd -- "$results_root"
        if ! mv -- "$results_temp" "$results_evidence/environment"; then
            result=1
        fi
    fi
    if (( result == 0 )); then
        printf '%s\n' passed > "$results_evidence/status.txt"
        printf '%s\n' "$results_evidence" > "$results_root/artifacts/test-results/latest-success.txt.tmp"
        mv -- "$results_root/artifacts/test-results/latest-success.txt.tmp" "$results_root/artifacts/test-results/latest-success.txt"
    else
        printf '%s\n' failed > "$results_evidence/status.txt"
    fi
    printf 'Test result evidence: %s\n' "$results_evidence"
    exit "$result"
}
trap finish_results EXIT
capture_results_inputs > "$results_evidence/inputs-before.txt"
results_captured=true
results_temp="$(mktemp -d "${TMPDIR:-/tmp}/iqtoolkit-test-results.XXXXXXXX")"
results_temp="$(cd -- "$results_temp" && pwd -P)"
case "$results_temp/" in
    "$results_root/"*) printf 'Result tool must build outside the checkout.\n' >&2; exit 1 ;;
esac
printf '%s\n' "$results_temp" > "$results_evidence/temporary-directory.txt"
mkdir -- "$results_temp/tool" "$results_temp/source" "$results_temp/reports"
for path in "${results_sources[@]}"; do
    mkdir -p -- "$results_temp/source/$(dirname -- "$path")"
    cp -- "$path" "$results_temp/source/$path"
    cmp -- "$path" "$results_temp/source/$path"
done
cp -- "$results_trx" "$results_temp/reports/tests.trx"
cp -- "$results_translation" "$results_temp/reports/translation.txt"
cmp -- "$results_trx" "$results_temp/reports/tests.trx"
cmp -- "$results_translation" "$results_temp/reports/translation.txt"
cp -- "$results_temp/source/global.json" "$results_temp/global.json"
cp -- "$results_temp/source/eng/TestResultsVerifier/Verifier.csproj.template" "$results_temp/tool/Verifier.csproj"
cp -- "$results_temp/source/eng/TestResultsVerifier/Program.cs" "$results_temp/tool/Program.cs"
cp -- "$results_temp/source/eng/TestResultsVerifier/NuGet.config" "$results_temp/tool/NuGet.config"
cd -- "$results_temp"
export NUGET_PACKAGES="$results_temp/packages"
export NUGET_HTTP_CACHE_PATH="$results_temp/http-cache"
export NUGET_PLUGINS_CACHE_PATH="$results_temp/plugin-cache"
results_properties=(-p:ImportDirectoryBuildProps=false -p:ImportDirectoryBuildTargets=false
    -p:ManagePackageVersionsCentrally=false -p:ContinuousIntegrationBuild=true)
dotnet --info > "$results_evidence/dotnet-info.txt"
dotnet restore tool/Verifier.csproj --configfile tool/NuGet.config --packages "$NUGET_PACKAGES" \
    "${results_properties[@]}" --disable-build-servers -p:UseSharedCompilation=false -nr:false \
    -warnaserror 2>&1 | tee "$results_evidence/restore.log"
dotnet build tool/Verifier.csproj -c Release --no-restore \
    "${results_properties[@]}" --disable-build-servers -p:UseSharedCompilation=false -nr:false \
    -warnaserror 2>&1 | tee "$results_evidence/build.log"
dotnet tool/bin/Release/net10.0/Verifier.dll reports/tests.trx reports/translation.txt \
    2>&1 | tee "$results_evidence/outcome.jsonl"
for path in "${results_sources[@]}"; do
    cmp -- "$results_root/$path" "$results_temp/source/$path"
done
cmp -- "$results_trx" "$results_temp/reports/tests.trx"
cmp -- "$results_translation" "$results_temp/reports/translation.txt"
