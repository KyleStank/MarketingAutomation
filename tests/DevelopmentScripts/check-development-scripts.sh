#!/usr/bin/env bash
# Integration checks use fake game/loader files and never touch personal saves.
set -euo pipefail

test_directory=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)
repository=$(cd -- "$test_directory/../.." && pwd -P)
fixture=$(mktemp -d "$test_directory/.tmp.XXXXXX")

# Bound recursive cleanup to the fixture created by this test run.
cleanup() {
    case "$fixture" in
        "$test_directory"/.tmp.*)
            rm -rf -- "$fixture"
            ;;
        *)
            printf 'Refusing cleanup outside the disposable test directory.\n' >&2
            exit 1
            ;;
    esac
}

trap cleanup EXIT

expect_failure() {
    if "$@" > "$fixture/expected-error.log" 2>&1; then
        printf 'An invalid operation unexpectedly succeeded.\n' >&2
        exit 1
    fi
}

# Build a fake installation with paths that must be treated literally.
game="$fixture/Game's space ü & ; \$(touch INJECTED) = test"
saves="$fixture/Save space"
output="$fixture/Build output"
config="$fixture/.config.local"

mkdir -p -- "$game/BepInEx/core" "$saves" "$output"
printf 'fake loader' > "$game/BepInEx/core/BepInEx.dll"
printf 'fake preloader' > "$game/BepInEx/core/BepInEx.Preloader.dll"
printf 'first plugin' > "$output/MarketingAutomation.dll"
printf 'symbols' > "$output/MarketingAutomation.pdb"
printf 'save marker' > "$saves/untouched"

# Exercise interactive prompts as well as explicit command-line paths.
printf '%s\n%s\n' "$game" "$saves" | bash "$repository/tools/mgt2-mod-toolkit/initialize.sh" --config-only --config "$config"
source "$repository/tools/mgt2-mod-toolkit/scripts/project-config.sh"
read_project_config "$config"
[[ "$game_directory" == "$game" && "$save_directory" == "$saves" ]]
cp -- "$config" "$fixture/original"

# Existing config survives both accidental replacement and invalid reinitialization.
expect_failure bash "$repository/tools/mgt2-mod-toolkit/initialize.sh" --config-only --config "$config" --game-directory "$game" --save-directory "$saves"
cmp "$config" "$fixture/original"
expect_failure bash "$repository/tools/mgt2-mod-toolkit/initialize.sh" --config-only --config "$config" --force --game-directory relative --save-directory "$saves"
cmp "$config" "$fixture/original"

# Deploy both output files, then repeat with an updated DLL and no source PDB.
bash "$repository/scripts/deploy.sh" --assembly "$output/MarketingAutomation.dll" --config "$config"
cmp "$output/MarketingAutomation.dll" "$game/BepInEx/plugins/MarketingAutomation.dll"
cmp "$output/MarketingAutomation.pdb" "$game/BepInEx/plugins/MarketingAutomation.pdb"

rm -- "$output/MarketingAutomation.pdb"
printf 'second plugin' > "$output/MarketingAutomation.dll"
bash "$repository/scripts/deploy.sh" --assembly "$output/MarketingAutomation.dll" --config "$config"
cmp "$output/MarketingAutomation.dll" "$game/BepInEx/plugins/MarketingAutomation.dll"
expect_failure bash "$repository/scripts/deploy.sh" --assembly "$output/Other.dll" --config "$config"

# Accept Windows line endings, but reject duplicate, unknown, or missing keys.
printf 'game_directory=%s\r\nsave_directory=%s\r\n' "$game" "$saves" > "$config"
bash "$repository/scripts/deploy.sh" --assembly "$output/MarketingAutomation.dll" --config "$config"
printf 'game_directory=%s\nsave_directory=%s\ngame_directory=%s\n' "$game" "$saves" "$game" > "$config"
expect_failure bash "$repository/scripts/deploy.sh" --assembly "$output/MarketingAutomation.dll" --config "$config"
printf 'game_directory=%s\nsave_directory=%s\nunknown=value\n' "$game" "$saves" > "$config"
expect_failure bash "$repository/scripts/deploy.sh" --assembly "$output/MarketingAutomation.dll" --config "$config"
printf 'game_directory=%s\n' "$game" > "$config"
expect_failure bash "$repository/scripts/deploy.sh" --assembly "$output/MarketingAutomation.dll" --config "$config"

bash "$repository/tools/mgt2-mod-toolkit/initialize.sh" --config-only --config "$config" --force --game-directory "$game" --save-directory "$saves"

# A deployment launched elsewhere must use its mod root, not the caller/toolkit.
mod_root="$fixture/Another mod"
mkdir -p -- "$mod_root/scripts" "$mod_root/tools/mgt2-mod-toolkit/scripts"
cp -- "$repository/scripts/deploy.sh" "$mod_root/scripts/deploy.sh"
cp -- "$repository/tools/mgt2-mod-toolkit/scripts/project-config.sh" "$mod_root/tools/mgt2-mod-toolkit/scripts/project-config.sh"
bash "$repository/tools/mgt2-mod-toolkit/initialize.sh" --config-only --project-root "$mod_root" --game-directory "$game" --save-directory "$saves"
(
    cd -- "$output"
    bash "$mod_root/scripts/deploy.sh" --assembly "$output/MarketingAutomation.dll"
)
[[ -f "$mod_root/.config.local" ]]

# Losing loader files must stop deployment without touching save data.
rm -- "$game/BepInEx/core/BepInEx.dll"
expect_failure bash "$repository/scripts/deploy.sh" --assembly "$output/MarketingAutomation.dll" --config "$config"
expect_failure bash "$repository/scripts/deploy.sh" --assembly "$output/MarketingAutomation.dll" --config "$fixture/missing"
[[ "$(cat "$saves/untouched")" == 'save marker' && ! -e INJECTED ]]
printf 'PASS: Bash initialization, literal config, validation, and deployment.\n'
