# Integration checks use fake game/loader files and never touch personal saves.
$ErrorActionPreference = 'Stop'
$repository = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$fixture = Join-Path $PSScriptRoot ('.tmp.' + [Guid]::NewGuid().ToString('N'))
$initialize = Join-Path $repository 'tools/mgt2-mod-toolkit/initialize.ps1'
$deploy = Join-Path $repository 'scripts/deploy.ps1'
. (Join-Path $repository 'tools/mgt2-mod-toolkit/scripts/project-config.ps1')

function Require($Condition, [string]$Message) {
    if (-not $Condition) {
        throw $Message
    }
}

function Expect-Failure([scriptblock]$Action) {
    $failed = $false
    try {
        & $Action
    } catch {
        $failed = $true
    }

    Require $failed 'An invalid operation unexpectedly succeeded.'
}

try {
    # Build a fake installation with paths that must be treated literally.
    $game = Join-Path $fixture 'Game space & ; $() [literal] = test'
    $saves = Join-Path $fixture 'Save space'
    $output = Join-Path $fixture 'Build output'
    $config = Join-Path $fixture '.config.local'

    foreach ($directory in @((Join-Path $game 'BepInEx/core'), $saves, $output)) {
        [IO.Directory]::CreateDirectory($directory) | Out-Null
    }

    foreach ($file in @('BepInEx.dll', 'BepInEx.Preloader.dll')) {
        [IO.File]::WriteAllText((Join-Path $game "BepInEx/core/$file"), 'fake loader')
    }

    $assembly = Join-Path $output 'MarketingAutomation.dll'
    $symbols = Join-Path $output 'MarketingAutomation.pdb'
    [IO.File]::WriteAllText($assembly, 'first plugin')
    [IO.File]::WriteAllText($symbols, 'symbols')
    [IO.File]::WriteAllText((Join-Path $saves 'untouched'), 'save marker')

    # Initialization must preserve paths and refuse unintended config replacement.
    & $initialize -ConfigOnly -GameDirectory $game -SaveDirectory $saves -ConfigPath $config
    $loaded = Get-ProjectLocalConfig $config
    Require ($loaded.game_directory -eq $game) 'Game path did not round-trip literally.'
    Require ($loaded.save_directory -eq $saves) 'Save path did not round-trip.'
    $original = [IO.File]::ReadAllText($config)
    Expect-Failure { & $initialize -ConfigOnly -GameDirectory $game -SaveDirectory $saves -ConfigPath $config }
    Require ([IO.File]::ReadAllText($config) -eq $original) 'Initialization overwrote existing config.'
    Expect-Failure { & $initialize -ConfigOnly -GameDirectory 'relative' -SaveDirectory $saves -ConfigPath $config -Force }
    Require ([IO.File]::ReadAllText($config) -eq $original) 'Invalid reinitialization changed config.'

    # Deploy both output files, then repeat with an updated DLL and no source PDB.
    & $deploy -AssemblyPath $assembly -ConfigPath $config
    $destination = Join-Path $game 'BepInEx/plugins'
    Require ([IO.File]::ReadAllText((Join-Path $destination 'MarketingAutomation.dll')) -eq 'first plugin') 'DLL copy failed.'
    Require ([IO.File]::ReadAllText((Join-Path $destination 'MarketingAutomation.pdb')) -eq 'symbols') 'PDB copy failed.'

    Remove-Item -LiteralPath $symbols
    [IO.File]::WriteAllText($assembly, 'second plugin')
    & $deploy -AssemblyPath $assembly -ConfigPath $config
    Require ([IO.File]::ReadAllText((Join-Path $destination 'MarketingAutomation.dll')) -eq 'second plugin') 'DLL overwrite or optional PDB failed.'
    Expect-Failure { & $deploy -AssemblyPath (Join-Path $output 'Other.dll') -ConfigPath $config }

    # Accept Windows line endings, but reject duplicate, unknown, or missing keys.
    [IO.File]::WriteAllText($config, $original.Replace("`n", "`r`n"))
    & $deploy -AssemblyPath $assembly -ConfigPath $config
    [IO.File]::WriteAllText($config, $original + "game_directory=$game`n")
    Expect-Failure { & $deploy -AssemblyPath $assembly -ConfigPath $config }
    [IO.File]::WriteAllText($config, $original + "unexpected=value`n")
    Expect-Failure { & $deploy -AssemblyPath $assembly -ConfigPath $config }
    [IO.File]::WriteAllText($config, "game_directory=$game`n")
    Expect-Failure { & $deploy -AssemblyPath $assembly -ConfigPath $config }

    & $initialize -ConfigOnly -GameDirectory $game -SaveDirectory $saves -ConfigPath $config -Force

    # A deployment launched elsewhere must use its mod root, not the caller/toolkit.
    $modRoot = Join-Path $fixture 'Another mod'
    $modScripts = Join-Path $modRoot 'scripts'
    $sharedScripts = Join-Path $modRoot 'tools/mgt2-mod-toolkit/scripts'
    [IO.Directory]::CreateDirectory($modScripts) | Out-Null
    [IO.Directory]::CreateDirectory($sharedScripts) | Out-Null
    Copy-Item -LiteralPath $deploy -Destination (Join-Path $modScripts 'deploy.ps1')
    Copy-Item -LiteralPath (Join-Path $repository 'tools/mgt2-mod-toolkit/scripts/project-config.ps1') `
        -Destination (Join-Path $sharedScripts 'project-config.ps1')
    & $initialize -ConfigOnly -ProjectRoot $modRoot -GameDirectory $game -SaveDirectory $saves
    Push-Location -LiteralPath $output
    try {
        & (Join-Path $modScripts 'deploy.ps1') -AssemblyPath $assembly
    } finally {
        Pop-Location
    }
    Require (Test-Path -LiteralPath (Join-Path $modRoot '.config.local')) 'Project config was written outside the mod.'

    # Losing loader files must stop deployment without touching save data.
    Remove-Item -LiteralPath (Join-Path $game 'BepInEx/core/BepInEx.dll')
    Expect-Failure { & $deploy -AssemblyPath $assembly -ConfigPath $config }
    Expect-Failure { & $deploy -AssemblyPath $assembly -ConfigPath (Join-Path $fixture 'missing') }
    Require ([IO.File]::ReadAllText((Join-Path $saves 'untouched')) -eq 'save marker') 'Save directory was modified.'
    Write-Host 'PASS: PowerShell initialization, literal config, validation, and deployment.'
} finally {
    # Bound recursive cleanup to the fixture created by this test run.
    $resolved = [IO.Path]::GetFullPath($fixture)
    $boundary = [IO.Path]::GetFullPath($PSScriptRoot) + [IO.Path]::DirectorySeparatorChar
    if (-not $resolved.StartsWith($boundary, [StringComparison]::OrdinalIgnoreCase) -or
        -not [IO.Path]::GetFileName($resolved).StartsWith('.tmp.')) {
        throw 'Refusing cleanup outside the disposable test directory.'
    }

    if (Test-Path -LiteralPath $resolved) {
        Remove-Item -LiteralPath $resolved -Recurse -Force
    }
}
