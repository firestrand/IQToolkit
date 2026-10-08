#!/usr/bin/env bash
set -euo pipefail
umask 077

coverage_root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd -P)"
cd -- "$coverage_root"
if (( $# != 2 )); then
    printf 'Usage: bash eng/verify-coverage-results.sh <baseline-descriptor.json> <current-descriptor.json>\n' >&2
    exit 1
fi
coverage_baseline="$(cd -- "$(dirname -- "$1")" && pwd -P)/$(basename -- "$1")"
coverage_current="$(cd -- "$(dirname -- "$2")" && pwd -P)/$(basename -- "$2")"
coverage_sources=(global.json eng/verify-coverage-results.sh eng/CoverageVerifier/Program.cs
    eng/CoverageVerifier/Verifier.csproj.template eng/CoverageVerifier/NuGet.config)
mkdir -p -- "$coverage_root/artifacts/coverage-results/runs"
coverage_evidence="$(mktemp -d "$coverage_root/artifacts/coverage-results/runs/run.XXXXXXXX")"
coverage_temp=
coverage_captured=false
printf '%s\n' running > "$coverage_evidence/status.txt"
capture_coverage_inputs() (
    cd -- "$coverage_root" || return 1
    git rev-parse HEAD || return 1
    local path
    for path in "${coverage_sources[@]}" "$coverage_baseline" "$coverage_current"; do
        git hash-object -- "$path" || return 1
        printf '%s\n' "$path"
    done
)
finish_coverage_results() {
    local result=$?
    trap - EXIT
    if [[ "$coverage_captured" == true ]]; then
        if ! capture_coverage_inputs > "$coverage_evidence/inputs-after.txt" \
            || ! cmp -s "$coverage_evidence/inputs-before.txt" "$coverage_evidence/inputs-after.txt"; then
            printf 'Coverage verifier source or descriptors changed.\n' >&2
            result=1
        fi
    fi
    if [[ -n "$coverage_temp" && -d "$coverage_temp" ]]; then
        if [[ -f "$coverage_temp/tool/Verifier.csproj" ]] \
            && ! mv -- "$coverage_temp/tool/Verifier.csproj" "$coverage_temp/tool/Verifier.csproj.snapshot"; then
            result=1
        fi
        cd -- "$coverage_root"
        if ! mv -- "$coverage_temp" "$coverage_evidence/environment"; then
            result=1
        fi
    fi
    if (( result == 0 )); then
        printf '%s\n' passed > "$coverage_evidence/status.txt"
        printf '%s\n' "$coverage_evidence" > "$coverage_root/artifacts/coverage-results/latest-success.txt.tmp"
        mv -- "$coverage_root/artifacts/coverage-results/latest-success.txt.tmp" "$coverage_root/artifacts/coverage-results/latest-success.txt"
    else
        printf '%s\n' failed > "$coverage_evidence/status.txt"
    fi
    printf 'Coverage result evidence: %s\n' "$coverage_evidence"
    exit "$result"
}
trap finish_coverage_results EXIT
capture_coverage_inputs > "$coverage_evidence/inputs-before.txt"
coverage_captured=true
coverage_temp="$(mktemp -d "${TMPDIR:-/tmp}/iqtoolkit-coverage-results.XXXXXXXX")"
coverage_temp="$(cd -- "$coverage_temp" && pwd -P)"
case "$coverage_temp/" in
    "$coverage_root/"*) printf 'Coverage tool must build outside the checkout.\n' >&2; exit 1 ;;
esac
printf '%s\n' "$coverage_temp" > "$coverage_evidence/temporary-directory.txt"
mkdir -- "$coverage_temp/tool" "$coverage_temp/source" "$coverage_temp/descriptors" "$coverage_temp/retained-inputs"
for path in "${coverage_sources[@]}"; do
    mkdir -p -- "$coverage_temp/source/$(dirname -- "$path")"
    cp -- "$path" "$coverage_temp/source/$path"
    cmp -- "$path" "$coverage_temp/source/$path"
done
cp -- "$coverage_baseline" "$coverage_temp/descriptors/baseline.json"
cp -- "$coverage_current" "$coverage_temp/descriptors/current.json"
cmp -- "$coverage_baseline" "$coverage_temp/descriptors/baseline.json"
cmp -- "$coverage_current" "$coverage_temp/descriptors/current.json"
cp -- "$coverage_temp/source/global.json" "$coverage_temp/global.json"
cp -- "$coverage_temp/source/eng/CoverageVerifier/Verifier.csproj.template" "$coverage_temp/tool/Verifier.csproj"
cp -- "$coverage_temp/source/eng/CoverageVerifier/Program.cs" "$coverage_temp/tool/Program.cs"
cp -- "$coverage_temp/source/eng/CoverageVerifier/NuGet.config" "$coverage_temp/tool/NuGet.config"
cd -- "$coverage_temp"
export NUGET_PACKAGES="$coverage_temp/packages"
export NUGET_HTTP_CACHE_PATH="$coverage_temp/http-cache"
export NUGET_PLUGINS_CACHE_PATH="$coverage_temp/plugin-cache"
coverage_properties=(-p:ImportDirectoryBuildProps=false -p:ImportDirectoryBuildTargets=false
    -p:ManagePackageVersionsCentrally=false -p:ContinuousIntegrationBuild=true)
dotnet --info > "$coverage_evidence/dotnet-info.txt"
dotnet restore tool/Verifier.csproj --configfile tool/NuGet.config --packages "$NUGET_PACKAGES" \
    "${coverage_properties[@]}" --disable-build-servers -p:UseSharedCompilation=false -nr:false \
    -warnaserror 2>&1 | tee "$coverage_evidence/restore.log"
dotnet build tool/Verifier.csproj -c Release --no-restore \
    "${coverage_properties[@]}" --disable-build-servers -p:UseSharedCompilation=false -nr:false \
    -warnaserror 2>&1 | tee "$coverage_evidence/build.log"
dotnet tool/bin/Release/net10.0/Verifier.dll descriptors/baseline.json descriptors/current.json \
    "$coverage_temp/retained-inputs" 2>&1 | tee "$coverage_evidence/outcome.jsonl"
for path in "${coverage_sources[@]}"; do
    cmp -- "$coverage_root/$path" "$coverage_temp/source/$path"
done
cmp -- "$coverage_baseline" "$coverage_temp/descriptors/baseline.json"
cmp -- "$coverage_current" "$coverage_temp/descriptors/current.json"
