param(
    [ValidateSet('Debug', 'Staging', 'Release')]
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
Import-Module (Join-Path $PSScriptRoot 'RepositoryTools.psm1') -Force

$projectPath = Join-Path $repositoryRoot 'Icod.Widgets.csproj'
$version = Get-MSBuildProperty -ProjectPath $projectPath -Name 'PackageVersion' -Configuration $Configuration
if ($version -notmatch '^[0-9]+\.[0-9]+\.[0-9]+(?:-[0-9A-Za-z][0-9A-Za-z.-]*)?$') {
    throw "Invalid Widgets package version '$version'."
}

$notesPath = Join-Path $repositoryRoot "docs/releases/$version.md"
if (-not (Test-Path -LiteralPath $notesPath -PathType Leaf)) {
    throw "Package version '$version' requires curated notes at '$notesPath' before tagging."
}
if ([string]::IsNullOrWhiteSpace([System.IO.File]::ReadAllText($notesPath))) {
    throw "Curated notes at '$notesPath' are empty."
}

Write-Host "Curated release notes found for Icod.Widgets $version."
