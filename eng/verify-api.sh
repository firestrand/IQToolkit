#!/usr/bin/env bash
set -euo pipefail

task_root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
cd -- "$task_root"
ci_properties=()
if [[ "${GITHUB_ACTIONS:-false}" == 'true' ]]; then
    ci_properties=(-p:ContinuousIntegrationBuild=true)
fi
baseline_packages="${1:-$task_root/artifacts/package-baseline}"
api_root="$task_root/artifacts/api-compatibility"
mkdir -p -- "$api_root/runs"
api_output="$(mktemp -d "$api_root/runs/run.XXXXXXXX")"
printf '%s\n' running > "$api_output/status.txt"
git rev-parse HEAD > "$api_output/revision.txt"
git branch --show-current > "$api_output/branch.txt"
git status --porcelain=v1 > "$api_output/worktree.txt"
finish_api_verification() {
    local exit_code=$?
    if (( exit_code != 0 )); then
        printf '%s\n' failed > "$api_output/status.txt"
    fi
    printf 'API compatibility evidence: %s\n' "$api_output"
}
trap finish_api_verification EXIT

# Record dirty compilation/verification inputs, not just the committed revision.
capture_api_inputs() {
    while IFS= read -r -d '' source_path; do
        case "$source_path" in
            *.cs|*.csproj|*.props|*.targets|*.sln|*.globalconfig|.editorconfig|global.json|NuGet.config|eng/verify-api.sh|*/packages.lock.json)
                if [[ -f "$source_path" ]]; then
                    git hash-object -- "$source_path"
                    printf '%s\n' "$source_path"
                fi
                ;;
        esac
    done < <(git ls-files --cached --others --exclude-standard -z)
}
capture_api_inputs > "$api_output/snapshot.txt"

# Rebuild the captured source inputs before packaging. A clean per-run package
# directory and a fresh validation semaphore prevent reuse of stale evidence.
dotnet restore IQToolkit.sln --locked-mode -p:Configuration=Release "${ci_properties[@]}" -warnaserror 2>&1 | tee "$api_output/restore.txt"
dotnet build IQToolkit.sln -c Release --no-restore -t:Rebuild "${ci_properties[@]}" -warnaserror \
    2>&1 | tee "$api_output/rebuild.txt"

# Exact versions and SHA-512 content hashes are checked by locked restore. The
# isolated engineering project never becomes a runtime/package dependency.
api_reference_packages="$api_output/reference-packages"
api_reference_project="$task_root/eng/ApiCompatibilityReferences/ApiCompatibilityReferences.csproj"
dotnet restore "$api_reference_project" --locked-mode \
    -p:Configuration=Release "${ci_properties[@]}" -warnaserror \
    "-p:RestorePackagesPath=$api_reference_packages" \
    "-p:BaseIntermediateOutputPath=$api_output/reference-obj/" \
    2>&1 | tee "$api_output/reference-restore.txt"
cp -- eng/ApiCompatibilityReferences/packages.lock.json "$api_output/reference-packages.lock.json"
printf '%s\n' 'System.Drawing.Common 10.0.8; Microsoft.Win32.SystemEvents 10.0.8' \
    'Purpose: metadata-only ApiCompat reference closure; both MIT.' \
    'Route: CustomAfterMicrosoftCommonTargets -> AddIQToolkitApiCompatibilityReferences -> existing SDK ReferencePath groups.' \
    'No runtime IQToolkit project references the engineering project.' > "$api_output/reference-provenance.txt"
dotnet --info > "$api_output/dotnet-info.txt"
while IFS= read -r -d '' reference_file; do
    case "$reference_file" in
        *.dll|*.nuspec|*.nupkg) sha256sum -- "$reference_file" ;;
    esac
done < <(rg --files --hidden -0 "$api_reference_packages") \
    > "$api_output/reference-hashes.txt"
mkdir -p -- "$api_output/tested-packages" "$api_output/baseline-packages" "$api_output/packages"

# These packages were captured from the original v1.1.1 build before changes.
# Do not substitute a download from an assumed published version on NuGet.
for package_id in IQToolkit IQToolkit.Data IQToolkit.Data.Access \
    IQToolkit.Data.MySqlClient IQToolkit.Data.SQLite IQToolkit.Data.SqlClient; do
    baseline_package="$baseline_packages/$package_id.1.1.1.nupkg"
    if [[ ! -f "$baseline_package" ]]; then
        printf 'Missing immutable v1.1.1 baseline package: %s\n' "$baseline_package" >&2
        exit 1
    fi
    baseline_package="$(cd -- "$(dirname -- "$baseline_package")" && pwd)/$(basename -- "$baseline_package")"
    git hash-object -- "$baseline_package" >> "$api_output/baseline-hashes.txt"
    printf '%s\n' "$baseline_package" >> "$api_output/baseline-hashes.txt"
    cp -- "$baseline_package" "$api_output/baseline-packages/"
    baseline_snapshot="$api_output/baseline-packages/$package_id.1.1.1.nupkg"
    cmp -- "$baseline_package" "$baseline_snapshot"
    validation_semaphore="$api_output/$package_id.api-validated.semaphore"
    if [[ -e "$validation_semaphore" ]]; then
        printf 'API validation semaphore already exists for this fresh run: %s\n' "$validation_semaphore" >&2
        exit 1
    fi
    dotnet pack "$package_id/$package_id.csproj" -c Release --no-build --no-restore \
        "${ci_properties[@]}" -warnaserror \
        -o "$api_output/packages" \
        -p:EnablePackageValidation=true \
        "-p:PackageValidationBaselinePath=$baseline_snapshot" \
        -p:EnableStrictModeForBaselineValidation=false \
        -p:RunApiCompat=true \
        "-p:_ApiCompatValidatePackageSemaphoreFile=$validation_semaphore" \
        "-p:CustomAfterMicrosoftCommonTargets=$task_root/eng/api-compatibility.targets" \
        "-p:ApiCompatibilityReferenceDirectory=$api_reference_packages" \
        "-p:ApiCompatibilitySdkReferenceManifest=$api_output/$package_id.sdk-reference.txt" \
        "-p:ApiCompatibilityPackageInputManifest=$api_output/$package_id.package-inputs.sha256" \
        "-p:ApiCompatibilityEvidenceDirectory=$api_output/$package_id.metadata" \
        2>&1 | tee "$api_output/$package_id.txt"
    while IFS= read -r diagnostic; do
        if [[ "$diagnostic" == *"Could not resolve reference"* ]]; then
            printf 'Incomplete API comparison for %s: resolve its reference assembly before accepting this gate.\n' "$package_id" >&2
            exit 1
        fi
    done < "$api_output/$package_id.txt"
    if ! rg -q 'API reference closure: System.Drawing.Common 10.0.8' "$api_output/$package_id.txt"; then
        printf 'The API reference hook did not execute for %s.\n' "$package_id" >&2
        exit 1
    fi
    if [[ ! -f "$validation_semaphore" ]]; then
        printf 'The SDK package validation task did not complete for %s.\n' "$package_id" >&2
        exit 1
    fi
    sha256sum --check "$api_output/$package_id.package-inputs.sha256" \
        > "$api_output/$package_id.package-inputs-after.txt"
    sha256sum --check "$api_output/$package_id.sdk-reference.txt.sha256" \
        > "$api_output/$package_id.sdk-reference-after.txt"
    # The first comparison input is the actual NuGetPackOutput passed to the
    # SDK validation task, including its current version; the second is 1.1.1.
    IFS= read -r comparison_input < "$api_output/$package_id.package-inputs.sha256"
    current_package="${comparison_input#*  }"
    if [[ "$current_package" != "$api_output/packages/"* || ! -f "$current_package" ]]; then
        printf 'Unexpected current package path from the SDK comparison: %s\n' "$current_package" >&2
        exit 1
    fi
    cp -- "$current_package" "$api_output/tested-packages/"
    cmp -- "$current_package" "$api_output/tested-packages/$(basename -- "$current_package")"
done

for sdk_manifest in "$api_output"/*.sdk-reference.txt; do
    cmp -- "$api_output/IQToolkit.sdk-reference.txt" "$sdk_manifest"
done
sha256sum --check "$api_output/reference-hashes.txt" > "$api_output/reference-hashes-after.txt"

sha256sum -- "$api_output"/tested-packages/*.nupkg "$api_output"/baseline-packages/*.nupkg \
    > "$api_output/package-hashes.txt"
capture_api_inputs > "$api_output/snapshot-after.txt"
if ! cmp -s "$api_output/snapshot.txt" "$api_output/snapshot-after.txt"; then
    printf 'Compilation or API verification inputs changed during verification; rerun against a stable snapshot.\n' >&2
    exit 1
fi

printf '%s\n' passed > "$api_output/status.txt"
printf '%s\n' "$api_output" > "$api_root/latest-success.txt.tmp"
mv -- "$api_root/latest-success.txt.tmp" "$api_root/latest-success.txt"
