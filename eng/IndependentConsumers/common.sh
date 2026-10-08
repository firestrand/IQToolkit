#!/usr/bin/env bash
# Shared source for the explicit preparation and separate locked verification.

independent_profiles=(
    IQToolkit IQToolkit.Data IQToolkit.Data.SQLite IQToolkit.Data.SqlClient
    IQToolkit.Data.MySqlClient IQToolkit.Data.Access
)
independent_version=1.1.2-preview.1

capture_independent_inputs() {
    local profile path
    for path in "$independent_template/NuGet.config" "$independent_template/common.sh" \
        "$independent_template/Verifier/Verifier.csproj.template" "$independent_template/Verifier/Program.cs" \
        "$independent_template/Verifier/NuGet.config" "$task_root/global.json" \
        "$task_root/eng/prepare-independent-consumers.sh" "$task_root/eng/verify-independent-consumers.sh"; do
        sha256sum -- "$path"
    done
    for profile in "${independent_profiles[@]}"; do
        sha256sum -- "$independent_template/Profiles/$profile/Consumer.csproj.template" \
            "$independent_template/Profiles/$profile/PublicSymbols.cs" \
            "$independent_feed/$profile.$independent_version.nupkg"
        if [[ "$independent_mode" == verify ]]; then
            sha256sum -- "$independent_template/Profiles/$profile/packages.lock.json"
        fi
    done
    if [[ "$independent_mode" == verify ]]; then
        sha256sum -- "$independent_template/prepared-feed.sha256"
    fi
}

capture_independent_feed() {
    local profile
    (
        cd -- "$independent_feed"
        for profile in "${independent_profiles[@]}"; do
            sha256sum -- "$profile.$independent_version.nupkg"
        done
    )
}

finish_independent_consumers() {
    local exit_code=$?
    trap - EXIT
    if [[ -f "$independent_evidence/inputs-before.sha256" ]]; then
        if ! capture_independent_inputs > "$independent_evidence/inputs-after.sha256" \
            || ! cmp -s "$independent_evidence/inputs-before.sha256" "$independent_evidence/inputs-after.sha256"; then
            printf 'Original independent consumer/feed inputs changed during execution.\n' >&2
            exit_code=1
        fi
    fi
    if (( exit_code != 0 )); then
        printf '%s\n' failed > "$independent_evidence/status.txt"
        if [[ -n "${independent_active_profile:-}" ]]; then
            printf '%s\n' failed > "$independent_evidence/$independent_active_profile/status.txt"
        fi
    fi
    # Workspace language servers may restore any discovered .csproj, including
    # retained evidence. Preserve its exact bytes under a non-discoverable name
    # before moving the owned outside environment back into the repository.
    local project profile retained hash
    for project in "$independent_temp/tool/Verifier.csproj"; do
        if [[ -f "$project" ]]; then
            retained="$project.snapshot"
            hash="$(sha256sum -- "$project")"
            if ! mv -- "$project" "$retained"; then
                exit_code=1
            fi
            printf '%s\t%s\t%s\n' "${project#"$independent_temp/"}" "${retained#"$independent_temp/"}" "${hash%% *}" \
                >> "$independent_evidence/project-filename-mapping.tsv"
        fi
    done
    for profile in "${independent_profiles[@]}"; do
        project="$independent_temp/$profile/Consumer.csproj"
        if [[ -f "$project" ]]; then
            retained="$project.snapshot"
            hash="$(sha256sum -- "$project")"
            if ! mv -- "$project" "$retained"; then
                exit_code=1
            fi
            printf '%s\t%s\t%s\n' "${project#"$independent_temp/"}" "${retained#"$independent_temp/"}" "${hash%% *}" \
                >> "$independent_evidence/project-filename-mapping.tsv"
        fi
    done
    cd -- "$task_root"
    if mv -- "$independent_temp" "$independent_evidence/environment"; then
        printf '%s\n' "$independent_evidence/environment" > "$independent_evidence/retained-environment.txt"
    else
        exit_code=1
    fi
    if (( exit_code == 0 )); then
        printf '%s\n' "$([[ "$independent_mode" == prepare ]] && printf prepared || printf passed)" \
            > "$independent_evidence/status.txt"
        printf '%s\n' "$independent_evidence" > "$independent_artifacts/latest-$independent_mode.txt.tmp"
        mv -- "$independent_artifacts/latest-$independent_mode.txt.tmp" "$independent_artifacts/latest-$independent_mode.txt"
    else
        printf '%s\n' failed > "$independent_evidence/status.txt"
    fi
    printf 'Independent package %s evidence: %s\n' "$independent_mode" "$independent_evidence"
    exit "$exit_code"
}

check_independent_copies() {
    local hash original copied
    while read -r hash original; do
        # All supported source paths are ordinary filesystem names. sha256sum's
        # escaped-filename format is rejected rather than ambiguously decoded.
        if [[ "$hash" == \\* ]]; then
            printf 'Escaped/newline input paths are unsupported: %s\n' "$original" >&2
            return 1
        fi
        if [[ "$original" == "$independent_template/"* ]]; then
            copied="$independent_temp/source/${original#"$independent_template/"}"
        elif [[ "$original" == "$independent_feed/"* ]]; then
            copied="$independent_temp/feed/$(basename -- "$original")"
        else
            copied="$independent_temp/source/$(basename -- "$original")"
        fi
        if [[ "$(sha256sum -- "$copied")" != "$hash  $copied" ]]; then
            printf 'Copied input bytes differ from the pre-copy snapshot: %s\n' "$original" >&2
            return 1
        fi
    done < "$independent_evidence/inputs-before.sha256"
    printf '%s\n' 'Every copied source/feed input matches its pre-copy SHA256.' \
        > "$independent_evidence/copied-input-inspection.txt"
}

run_independent_consumers() {
    independent_mode=$1
    shift
    if (( $# != 1 )); then
        printf 'Usage: bash eng/%s-independent-consumers.sh <tested-package-directory>\n' \
            "$([[ "$independent_mode" == prepare ]] && printf prepare || printf verify)" >&2
        return 1
    fi
    cd -- "$task_root"
    independent_template="$task_root/eng/IndependentConsumers"
    independent_feed="$(cd -- "$1" && pwd -P)"
    local profile file consumer
    local sdk_properties=(
        -p:ImportDirectoryBuildProps=false -p:ImportDirectoryBuildTargets=false
        -p:ManagePackageVersionsCentrally=false -p:Configuration=Release
        -p:ContinuousIntegrationBuild=true
    )
    for profile in "${independent_profiles[@]}"; do
        if [[ ! -f "$independent_feed/$profile.$independent_version.nupkg" ]]; then
            printf 'Missing tested package: %s\n' "$profile.$independent_version.nupkg" >&2
            return 1
        fi
        if [[ "$independent_mode" == verify && ! -f "$independent_template/Profiles/$profile/packages.lock.json" ]]; then
            printf 'Prepare and review the standalone lock for %s before verification.\n' "$profile" >&2
            return 1
        fi
    done
    if [[ "$independent_mode" == verify && ! -f "$independent_template/prepared-feed.sha256" ]]; then
        printf 'Prepare and review locks for the exact feed before verification.\n' >&2
        return 1
    fi
    independent_artifacts="$task_root/artifacts/independent-consumers"
    mkdir -p -- "$independent_artifacts/$independent_mode-runs"
    independent_evidence="$(mktemp -d "$independent_artifacts/$independent_mode-runs/run.XXXXXXXX")"
    independent_temp="$(mktemp -d "${TMPDIR:-/tmp}/iqtoolkit-independent.XXXXXXXX")"
    independent_temp="$(cd -- "$independent_temp" && pwd -P)"
    case "$independent_temp/" in
        "$task_root/"*)
            printf 'The consumer environment must be outside the repository; choose another TMPDIR.\n' >&2
            rmdir -- "$independent_temp"
            return 1
            ;;
    esac
    independent_active_profile=
    trap finish_independent_consumers EXIT
    printf '%s\n' running > "$independent_evidence/status.txt"
    printf '%s\n' "$independent_feed" > "$independent_evidence/package-directory.txt"
    printf '%s\n' "$independent_temp" > "$independent_evidence/temporary-directory.txt"
    for profile in "${independent_profiles[@]}"; do
        mkdir -- "$independent_evidence/$profile"
        printf '%s\n' not-run > "$independent_evidence/$profile/status.txt"
    done
    capture_independent_inputs > "$independent_evidence/inputs-before.sha256"
    capture_independent_feed > "$independent_evidence/feed.sha256"
    if [[ "$independent_mode" == verify ]]; then
        cmp -- "$independent_template/prepared-feed.sha256" "$independent_evidence/feed.sha256"
    fi
    mkdir -p -- "$independent_temp/source/Verifier" "$independent_temp/source/Profiles" "$independent_temp/feed"
    cp -- "$independent_template/NuGet.config" "$independent_template/common.sh" \
        "$task_root/global.json" "$task_root/eng/prepare-independent-consumers.sh" \
        "$task_root/eng/verify-independent-consumers.sh" "$independent_temp/source/"
    cp -- "$independent_template/Verifier/"* "$independent_temp/source/Verifier/"
    for profile in "${independent_profiles[@]}"; do
        mkdir -- "$independent_temp/source/Profiles/$profile"
        cp -- "$independent_template/Profiles/$profile/Consumer.csproj.template" \
            "$independent_template/Profiles/$profile/PublicSymbols.cs" "$independent_temp/source/Profiles/$profile/"
        cp -- "$independent_feed/$profile.$independent_version.nupkg" "$independent_temp/feed/"
        if [[ "$independent_mode" == verify ]]; then
            cp -- "$independent_template/Profiles/$profile/packages.lock.json" "$independent_temp/source/Profiles/$profile/"
        elif [[ -f "$independent_template/Profiles/$profile/packages.lock.json" ]]; then
            cp -- "$independent_template/Profiles/$profile/packages.lock.json" "$independent_evidence/$profile/previous.lock.json"
        fi
    done
    if [[ "$independent_mode" == verify ]]; then
        cp -- "$independent_template/prepared-feed.sha256" "$independent_temp/source/"
    fi
    check_independent_copies
    # Pin SDK selection outside the repository, without importing its props,
    # targets, CPM configuration or existing package caches.
    cp -- "$independent_temp/source/global.json" "$independent_temp/global.json"
    cmp -- "$independent_temp/source/global.json" "$independent_temp/global.json"
    mkdir -- "$independent_temp/tool"
    cp -- "$independent_temp/source/Verifier/Program.cs" "$independent_temp/source/Verifier/NuGet.config" "$independent_temp/tool/"
    cp -- "$independent_temp/source/Verifier/Verifier.csproj.template" "$independent_temp/tool/Verifier.csproj"
    cmp -- "$independent_temp/source/Verifier/Verifier.csproj.template" "$independent_temp/tool/Verifier.csproj"
    cmp -- "$independent_temp/source/Verifier/Program.cs" "$independent_temp/tool/Program.cs"
    cmp -- "$independent_temp/source/Verifier/NuGet.config" "$independent_temp/tool/NuGet.config"
    cd -- "$independent_temp"
    export NUGET_PACKAGES="$independent_temp/tool-packages"
    export NUGET_HTTP_CACHE_PATH="$independent_temp/http-cache"
    export NUGET_PLUGINS_CACHE_PATH="$independent_temp/plugin-cache"
    dotnet --info > "$independent_evidence/dotnet-info.txt"
    # Framework-only tooling has no NuGet package dependencies or feed access.
    dotnet restore tool/Verifier.csproj --configfile tool/NuGet.config --packages "$NUGET_PACKAGES" \
        "${sdk_properties[@]}" -warnaserror \
        2>&1 | tee "$independent_evidence/verifier-restore.log"
    dotnet build tool/Verifier.csproj -c Release --no-restore "${sdk_properties[@]}" -warnaserror \
        2>&1 | tee "$independent_evidence/verifier-build.log"
    for profile in "${independent_profiles[@]}"; do
        independent_active_profile=$profile
        printf '%s\n' running > "$independent_evidence/$profile/status.txt"
        consumer="$independent_temp/$profile"
        mkdir -p -- "$consumer/feed"
        cp -- "$independent_temp/source/Profiles/$profile/PublicSymbols.cs" "$independent_temp/source/NuGet.config" "$consumer/"
        cp -- "$independent_temp/source/Profiles/$profile/Consumer.csproj.template" "$consumer/Consumer.csproj"
        cp -- "$independent_temp/feed/"*.nupkg "$consumer/feed/"
        if [[ "$independent_mode" == verify ]]; then
            cp -- "$independent_temp/source/Profiles/$profile/packages.lock.json" "$consumer/"
        fi
        # Check the second copy before restore, including the shared exact feed.
        cmp -- "$consumer/Consumer.csproj" "$independent_temp/source/Profiles/$profile/Consumer.csproj.template"
        cmp -- "$consumer/PublicSymbols.cs" "$independent_temp/source/Profiles/$profile/PublicSymbols.cs"
        cmp -- "$consumer/NuGet.config" "$independent_temp/source/NuGet.config"
        for file in "$independent_temp/feed/"*.nupkg; do
            cmp -- "$file" "$consumer/feed/$(basename -- "$file")"
        done
        if [[ "$independent_mode" == verify ]]; then
            cmp -- "$consumer/packages.lock.json" "$independent_temp/source/Profiles/$profile/packages.lock.json"
        fi
        printf '%s\n' 'Profile source/feed copies match the captured originals; cold cache has not been created.' \
            > "$independent_evidence/$profile/copied-input-inspection.txt"
        export NUGET_PACKAGES="$consumer/packages"
        export NUGET_HTTP_CACHE_PATH="$consumer/http-cache"
        export NUGET_PLUGINS_CACHE_PATH="$consumer/plugin-cache"
        local restore_options=()
        if [[ "$independent_mode" == verify ]]; then
            restore_options=(--locked-mode)
        fi
        cd -- "$consumer"
        dotnet restore Consumer.csproj --configfile NuGet.config --packages "$consumer/packages" \
            "${restore_options[@]}" "${sdk_properties[@]}" -warnaserror \
            2>&1 | tee "$independent_evidence/$profile/restore.log"
        cp -- packages.lock.json obj/project.assets.json "$independent_evidence/$profile/"
        if [[ "$independent_mode" == verify ]]; then
            cmp -- packages.lock.json "$independent_temp/source/Profiles/$profile/packages.lock.json"
        fi
        dotnet "$independent_temp/tool/bin/Release/net10.0/Verifier.dll" graph "$consumer" "$profile" "$independent_version" \
            2>&1 | tee "$independent_evidence/$profile/provenance.log"
        if [[ "$independent_mode" == verify ]]; then
            dotnet build Consumer.csproj -c Release --no-restore -t:Rebuild \
                "${sdk_properties[@]}" -warnaserror 2>&1 | tee "$independent_evidence/$profile/build.log"
            dotnet "$independent_temp/tool/bin/Release/net10.0/Verifier.dll" output "$consumer" "$profile" "$independent_version" \
                2>&1 | tee "$independent_evidence/$profile/compile-output.log"
            # No build-side lock rewrite or changed source/feed may be hidden.
            cmp -- packages.lock.json "$independent_temp/source/Profiles/$profile/packages.lock.json"
        fi
        cmp -- "$consumer/Consumer.csproj" "$independent_temp/source/Profiles/$profile/Consumer.csproj.template"
        cmp -- "$consumer/PublicSymbols.cs" "$independent_temp/source/Profiles/$profile/PublicSymbols.cs"
        cmp -- "$consumer/NuGet.config" "$independent_temp/source/NuGet.config"
        for file in "$independent_temp/feed/"*.nupkg; do
            cmp -- "$file" "$consumer/feed/$(basename -- "$file")"
        done
        printf '%s\n' "$([[ "$independent_mode" == prepare ]] && printf prepared || printf passed)" \
            > "$independent_evidence/$profile/status.txt"
        independent_active_profile=
    done
    cd -- "$task_root"
    capture_independent_inputs > "$independent_evidence/inputs-after.sha256"
    cmp -- "$independent_evidence/inputs-before.sha256" "$independent_evidence/inputs-after.sha256"
    if [[ "$independent_mode" == prepare ]]; then
        # Install only actual generated locks after every independent graph and
        # original input has passed. This deliberate update requires review.
        for profile in "${independent_profiles[@]}"; do
            cp -- "$independent_evidence/$profile/packages.lock.json" \
                "$independent_template/Profiles/$profile/packages.lock.json"
        done
        cp -- "$independent_evidence/feed.sha256" "$independent_template/prepared-feed.sha256"
        printf 'Review all six Profiles/<package-id>/packages.lock.json files and prepared-feed.sha256 before locked verification.\n'
    else
        printf 'All six independent package graphs restored locked unchanged and compiled their named public symbols.\n'
    fi
}
