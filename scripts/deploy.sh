#!/usr/bin/env bash
# Copy this build's plugin files into the configured BepInEx installation.
set -euo pipefail

deployment_project=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd -P)
source "$deployment_project/tools/mgt2-mod-toolkit/scripts/project-config.sh"
set_project_root "$deployment_project"

# MSBuild supplies the assembly through the environment; arguments allow manual use.
assembly_path=${MOD_ASSEMBLY:-}
config_path=$project_config_path

while [[ $# -gt 0 ]]; do
    case "$1" in
        --assembly|--config)
            [[ $# -ge 2 ]] || fail "Missing value for $1"

            case "$1" in
                --assembly)
                    assembly_path=$2
                    ;;
                --config)
                    config_path=$2
                    ;;
            esac

            shift 2
            ;;
        *) fail "Unknown argument: $1" ;;
    esac
done

# Revalidate on every deployment: installations can move after initialization.
read_project_config "$config_path"
assert_bepinex_installation "$game_directory"

[[ "$assembly_path" == /* && "${assembly_path##*/}" == MarketingAutomation.dll && -f "$assembly_path" ]] ||
    fail 'Supply the absolute path to the built MarketingAutomation.dll.'

# Copy only mod-owned output, never the referenced game or loader assemblies.
destination="$game_directory/BepInEx/plugins"
mkdir -p -- "$destination"
cp -- "$assembly_path" "$destination/MarketingAutomation.dll"

# Symbols are useful during development but are not required to run the plugin.
symbols="${assembly_path%.dll}.pdb"
if [[ -f "$symbols" ]]; then
    cp -- "$symbols" "$destination/MarketingAutomation.pdb"
fi

printf 'MarketingAutomation deployed to %s\n' "$destination"
