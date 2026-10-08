#!/usr/bin/env bash
set -euo pipefail
shopt -s nullglob

# The shell uses Git hashing (available on the existing CI matrix); artifact
# validation and cryptographic checks use the framework-only metadata tool.
symbol_root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd -P)"
cd -- "$symbol_root"
if (( $# != 1 )); then
    printf 'Usage: bash eng/verify-symbols.sh <package-directory>\n' >&2
    exit 1
fi
symbol_feed="$(cd -- "$1" && pwd -P)"
symbol_sources=(global.json Directory.Build.props eng/verify-symbols.sh eng/VerifyPackages.proj
    .github/workflows/ci.yml eng/IndependentConsumers/Verifier/Verifier.csproj.template
    eng/IndependentConsumers/Verifier/Program.cs eng/IndependentConsumers/Verifier/NuGet.config)
symbol_archives=("$symbol_feed"/*.nupkg "$symbol_feed"/*.snupkg)
mkdir -p -- "$symbol_root/artifacts/package-symbols/runs"
symbol_evidence="$(mktemp -d "$symbol_root/artifacts/package-symbols/runs/run.XXXXXXXX")"
symbol_temp=
symbol_captured=false
printf '%s\n' running > "$symbol_evidence/status.txt"
capture_symbol_inputs() (
    cd -- "$symbol_root" || return 1
    git rev-parse HEAD || return 1
    local path
    for path in "${symbol_sources[@]}" "$symbol_feed"/*.nupkg "$symbol_feed"/*.snupkg; do
        git hash-object -- "$path" || return 1
        printf '%s\n' "$path"
    done
)
finish_symbols() {
    local result=$?
    trap - EXIT
    if [[ "$symbol_captured" == true ]]; then
        if ! capture_symbol_inputs > "$symbol_evidence/inputs-after.txt" \
            || ! cmp -s "$symbol_evidence/inputs-before.txt" "$symbol_evidence/inputs-after.txt"; then
            printf 'Original symbol source/feed/revision inputs changed.\n' >&2
            result=1
        fi
    fi
    if [[ -n "$symbol_temp" && -d "$symbol_temp" ]]; then
        if [[ -f "$symbol_temp/tool/Verifier.csproj" ]]; then
            if ! mv -- "$symbol_temp/tool/Verifier.csproj" "$symbol_temp/tool/Verifier.csproj.snapshot"; then
                result=1
            fi
        fi
        cd -- "$symbol_root"
        if ! mv -- "$symbol_temp" "$symbol_evidence/environment"; then
            result=1
        fi
    fi
    if (( result == 0 )); then
        printf '%s\n' passed > "$symbol_evidence/status.txt"
        printf '%s\n' "$symbol_evidence" > "$symbol_root/artifacts/package-symbols/latest-success.txt.tmp"
        mv -- "$symbol_root/artifacts/package-symbols/latest-success.txt.tmp" "$symbol_root/artifacts/package-symbols/latest-success.txt"
    else
        printf '%s\n' failed > "$symbol_evidence/status.txt"
    fi
    printf 'Symbol provenance evidence: %s\n' "$symbol_evidence"
    exit "$result"
}
trap finish_symbols EXIT
capture_symbol_inputs > "$symbol_evidence/inputs-before.txt"
symbol_captured=true
symbol_commit="$(git rev-parse HEAD)"
printf '%s\n' "$symbol_feed" > "$symbol_evidence/package-directory.txt"
if (( ${#symbol_archives[@]} != 12 )); then
    printf 'Symbol qualification requires six library and six symbol archives.\n' >&2
    exit 1
fi
symbol_temp="$(mktemp -d "${TMPDIR:-/tmp}/iqtoolkit-symbols.XXXXXXXX")"
symbol_temp="$(cd -- "$symbol_temp" && pwd -P)"
case "$symbol_temp/" in
    "$symbol_root/"*) printf 'Symbol tool must build outside the checkout.\n' >&2; exit 1 ;;
esac
printf '%s\n' "$symbol_temp" > "$symbol_evidence/temporary-directory.txt"
mkdir -- "$symbol_temp/tool" "$symbol_temp/source" "$symbol_temp/feed" "$symbol_evidence/tested-packages"
for path in "${symbol_sources[@]}"; do
    mkdir -p -- "$symbol_temp/source/$(dirname -- "$path")"
    cp -- "$path" "$symbol_temp/source/$path"
done
cp -- "${symbol_archives[@]}" "$symbol_temp/feed/"
cp -- "${symbol_archives[@]}" "$symbol_evidence/tested-packages/"
# Check the captured identities before any restore; verify all second copies too.
{
    IFS= read -r captured_commit
    [[ "$captured_commit" == "$symbol_commit" ]]
    while IFS= read -r captured_hash && IFS= read -r path; do
        if [[ "$path" == "$symbol_feed/"* ]]; then
            copied="$symbol_temp/feed/$(basename -- "$path")"
            cmp -- "$copied" "$symbol_evidence/tested-packages/$(basename -- "$path")"
        else
            copied="$symbol_temp/source/$path"
        fi
        [[ "$(git hash-object -- "$copied")" == "$captured_hash" ]]
    done
} < "$symbol_evidence/inputs-before.txt"
cp -- "$symbol_temp/source/global.json" "$symbol_temp/global.json"
cp -- "$symbol_temp/source/eng/IndependentConsumers/Verifier/Verifier.csproj.template" "$symbol_temp/tool/Verifier.csproj"
cp -- "$symbol_temp/source/eng/IndependentConsumers/Verifier/Program.cs" "$symbol_temp/tool/Program.cs"
cp -- "$symbol_temp/source/eng/IndependentConsumers/Verifier/NuGet.config" "$symbol_temp/tool/NuGet.config"
cmp -- "$symbol_temp/tool/Verifier.csproj" "$symbol_temp/source/eng/IndependentConsumers/Verifier/Verifier.csproj.template"
cmp -- "$symbol_temp/tool/Program.cs" "$symbol_temp/source/eng/IndependentConsumers/Verifier/Program.cs"
cmp -- "$symbol_temp/tool/NuGet.config" "$symbol_temp/source/eng/IndependentConsumers/Verifier/NuGet.config"
cmp -- "$symbol_temp/global.json" "$symbol_temp/source/global.json"
cd -- "$symbol_temp"
export NUGET_PACKAGES="$symbol_temp/packages"
export NUGET_HTTP_CACHE_PATH="$symbol_temp/http-cache"
export NUGET_PLUGINS_CACHE_PATH="$symbol_temp/plugin-cache"
symbol_properties=(-p:ImportDirectoryBuildProps=false -p:ImportDirectoryBuildTargets=false
    -p:ManagePackageVersionsCentrally=false -p:ContinuousIntegrationBuild=true)
dotnet --info > "$symbol_evidence/dotnet-info.txt"
dotnet restore tool/Verifier.csproj --configfile tool/NuGet.config --packages "$NUGET_PACKAGES" \
    "${symbol_properties[@]}" -warnaserror 2>&1 | tee "$symbol_evidence/restore.log"
dotnet build tool/Verifier.csproj -c Release --no-restore \
    "${symbol_properties[@]}" -warnaserror 2>&1 | tee "$symbol_evidence/build.log"
dotnet tool/bin/Release/net10.0/Verifier.dll symbols "$symbol_temp/feed" \
    "$symbol_temp/source/Directory.Build.props" "$symbol_commit" 2>&1 | tee "$symbol_evidence/provenance.jsonl"
# Retained evidence must contain the exact input bytes, not a changed temp copy.
for path in "${symbol_sources[@]}"; do
    cmp -- "$symbol_root/$path" "$symbol_temp/source/$path"
done
for path in "${symbol_archives[@]}"; do
    cmp -- "$path" "$symbol_temp/feed/$(basename -- "$path")"
    cmp -- "$path" "$symbol_evidence/tested-packages/$(basename -- "$path")"
done
cmp -- "$symbol_temp/tool/Program.cs" "$symbol_temp/source/eng/IndependentConsumers/Verifier/Program.cs"
cmp -- "$symbol_temp/tool/Verifier.csproj" "$symbol_temp/source/eng/IndependentConsumers/Verifier/Verifier.csproj.template"
