# Copy this build's plugin files into the configured BepInEx installation.
[CmdletBinding()]
param(
    [string]$AssemblyPath = $env:MOD_ASSEMBLY,
    [string]$ConfigPath
)

$ErrorActionPreference = 'Stop'
. "$PSScriptRoot/../tools/mgt2-mod-toolkit/scripts/project-config.ps1"

if (-not $ConfigPath) {
    $ConfigPath = Get-ProjectConfigPath ([IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..')))
}

# Revalidate on every deployment: installations can move after initialization.
$config = Get-ProjectLocalConfig $ConfigPath
Assert-BepInExInstallation $config.game_directory

# MSBuild supplies AssemblyPath through the environment; manual calls can override it.
if (-not $AssemblyPath -or -not [IO.Path]::IsPathRooted($AssemblyPath) -or
    [IO.Path]::GetFileName($AssemblyPath) -cne 'MarketingAutomation.dll' -or
    -not (Test-Path -LiteralPath $AssemblyPath -PathType Leaf)) {
    throw 'Supply the absolute path to the built MarketingAutomation.dll.'
}

# Copy only mod-owned output, never the referenced game or loader assemblies.
$destination = Join-Path $config.game_directory 'BepInEx/plugins'
[IO.Directory]::CreateDirectory($destination) | Out-Null
Copy-Item -LiteralPath $AssemblyPath -Destination (Join-Path $destination 'MarketingAutomation.dll') -Force

# Symbols are useful during development but are not required to run the plugin.
$symbols = [IO.Path]::ChangeExtension($AssemblyPath, '.pdb')
if (Test-Path -LiteralPath $symbols -PathType Leaf) {
    Copy-Item -LiteralPath $symbols -Destination (Join-Path $destination 'MarketingAutomation.pdb') -Force
}

Write-Host "MarketingAutomation deployed to $destination"
