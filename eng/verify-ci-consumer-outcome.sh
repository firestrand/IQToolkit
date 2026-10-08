#!/usr/bin/env bash
set -euo pipefail

# Assert real producer outcomes. The owning supervisor separately authenticates
# process identities, interruption eligibility and terminal child disposition.
if (( $# != 5 )); then
    printf 'Usage: bash eng/verify-ci-consumer-outcome.sh <interrupted|complete> <evidence> <success-pointer-before> <success-pointer-after> <native-exit-file>\n' >&2
    exit 1
fi
outcome_mode=$1
outcome_evidence=$2
outcome_before=$3
outcome_after=$4
outcome_exit=$(cat -- "$5")
if [[ ! "$outcome_exit" =~ ^-?[0-9]+$ ]]; then
    printf 'A recorded native exit is required.\n' >&2
    exit 1
fi
case "$outcome_mode" in
    interrupted)
        if [[ "$(cat -- "$outcome_evidence/status.txt")" != failed ]]; then
            printf 'Interrupted CI consumer verification must record failed status.\n' >&2
            exit 1
        fi
        if (( outcome_exit == 0 )); then
            printf 'Interrupted CI consumer verification must exit nonzero.\n' >&2
            exit 1
        fi
        if ! cmp -s -- "$outcome_before" "$outcome_after"; then
            printf 'Interrupted CI consumer verification must preserve latest-success bytes.\n' >&2
            exit 1
        fi
        ;;
    complete)
        [[ "$(cat -- "$outcome_evidence/status.txt")" == passed ]]
        (( outcome_exit == 0 ))
        printf '%s\n' "$outcome_evidence" | cmp -s -- - "$outcome_after"
        outcome_log="$outcome_evidence/verify-independent.log"
        # Check each actual public-compilation JSON and both native verifier
        # confirmations exactly once; aggregate counts alone miss duplicates.
        outcome_profiles=(IQToolkit IQToolkit.Data IQToolkit.Data.SQLite IQToolkit.Data.SqlClient IQToolkit.Data.MySqlClient IQToolkit.Data.Access)
        for outcome_profile in "${outcome_profiles[@]}"; do
            awk -v prefix="{\"Package\":\"$outcome_profile\",\"Output\":" \
                'index($0, prefix) == 1 && index($0, "\"Sha256\":") && index($0, "\"References\":[") { count++ } END { exit count != 1 }' "$outcome_log"
            awk -v expected="Verified sole direct dependency and complete package provenance: $outcome_profile." \
                '$0 == expected { count++ } END { exit count != 1 }' "$outcome_log"
        done
        awk '$0 == "Public-symbol compilation verified; no provider/backend code executed." { count++ } END { exit count != 6 }' "$outcome_log"
        for outcome_profile in "${outcome_profiles[@]}" SQLite-runtime; do
            awk '$0 == "Verified complete lock equality except feed-bound toolkit contentHash leaves." { count++ } END { exit count != 1 }' \
                "$outcome_evidence/$outcome_profile.json.delta.log"
        done
        awk '$0 == "SQLite package-only native executor verification passed." { count++ } END { exit count != 1 }' "$outcome_evidence/verify-sqlite.log"
        ;;
    *) printf 'Unsupported outcome mode: %s\n' "$outcome_mode" >&2; exit 1 ;;
esac
cmp -- "$outcome_evidence/inputs-before.sha256" "$outcome_evidence/inputs-after.sha256"
printf 'CI consumer %s outcome verified.\n' "$outcome_mode"
