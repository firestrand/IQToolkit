#!/usr/bin/env bash
set -euo pipefail

task_root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
cd -- "$task_root"
ci_properties=()
if [[ "${GITHUB_ACTIONS:-false}" == 'true' ]]; then
    ci_properties=(-p:ContinuousIntegrationBuild=true)
fi
baseline_revision=60ddef60d4b635c4b24b78bff4327e3f3ff9ca5e
baseline_root="$task_root/artifacts/api-baselines"
mkdir -p -- "$baseline_root/runs"
baseline_output="$(mktemp -d "$baseline_root/runs/run.XXXXXXXX")"
printf '%s\n' running > "$baseline_output/status.txt"
finish_baseline() {
    local exit_code=$?
    if (( exit_code != 0 )); then
        printf '%s\n' failed > "$baseline_output/status.txt"
    fi
    printf 'Release baseline evidence: %s\n' "$baseline_output"
}
trap finish_baseline EXIT

git cat-file -e "$baseline_revision^{commit}"
printf '%s\n' "$baseline_revision" > "$baseline_output/revision.txt"
mkdir -p -- "$baseline_output/source" "$baseline_output/packages"
git archive --format=tar --output="$baseline_output/source.tar" "$baseline_revision"
sha256sum -- "$baseline_output/source.tar" > "$baseline_output/source.sha256"
tar -xf "$baseline_output/source.tar" -C "$baseline_output/source"
cd -- "$baseline_output/source"
dotnet --info > "$baseline_output/dotnet-info.txt"
dotnet restore IQToolkit.sln -p:Configuration=Release \
    -p:ManagePackageVersionsCentrally=false -p:RestorePackagesWithLockFile=false \
    -p:ImportDirectoryBuildTargets=false "${ci_properties[@]}" -warnaserror \
    2>&1 | tee "$baseline_output/restore.txt"
dotnet build IQToolkit.sln -c Release --no-restore -warnaserror \
    -p:ManagePackageVersionsCentrally=false -p:RestorePackagesWithLockFile=false \
    -p:ImportDirectoryBuildTargets=false "${ci_properties[@]}" \
    2>&1 | tee "$baseline_output/build.txt"
dotnet pack IQToolkit.sln -c Release --no-build --no-restore \
    -p:ManagePackageVersionsCentrally=false -p:RestorePackagesWithLockFile=false \
    -p:ImportDirectoryBuildTargets=false "${ci_properties[@]}" -warnaserror \
    -o "$baseline_output/packages" 2>&1 | tee "$baseline_output/pack.txt"

for package_id in IQToolkit IQToolkit.Data IQToolkit.Data.Access \
    IQToolkit.Data.MySqlClient IQToolkit.Data.SQLite IQToolkit.Data.SqlClient; do
    baseline_package="$baseline_output/packages/$package_id.1.1.1.nupkg"
    if [[ ! -f "$baseline_package" ]]; then
        printf 'Missing required release baseline package: %s\n' "$baseline_package" >&2
        exit 1
    fi
done
sha256sum -- "$baseline_output"/packages/*.nupkg > "$baseline_output/package-hashes.txt"
printf '%s\n' "$baseline_output/packages" > "$baseline_output/packages-directory.txt"
printf '%s\n' passed > "$baseline_output/status.txt"
printf '%s\n' "$baseline_output" > "$baseline_root/latest-success.txt.tmp"
mv -- "$baseline_root/latest-success.txt.tmp" "$baseline_root/latest-success.txt"
printf 'API baseline packages: %s\n' "$baseline_output/packages"
