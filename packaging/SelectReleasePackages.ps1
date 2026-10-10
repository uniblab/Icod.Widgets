param(
    [Parameter(Mandatory = $true)]
    [string]$SourceDirectory,

    [Parameter(Mandatory = $true)]
    [string]$DestinationDirectory,

    [Parameter(Mandatory = $true)]
    [string]$ExpectedVersion,

    [string]$GitHubOutputPath = ''
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
Import-Module (Join-Path $PSScriptRoot 'RepositoryTools.psm1') -Force

foreach ($variableName in @('SourceDirectory', 'DestinationDirectory')) {
    $value = Get-Variable -Name $variableName -ValueOnly
    if (-not [System.IO.Path]::IsPathRooted($value)) {
        $value = Join-Path $repositoryRoot $value
    }
    Set-Variable -Name $variableName -Value ([System.IO.Path]::GetFullPath($value))
}

if (-not (Test-Path -LiteralPath $SourceDirectory -PathType Container)) {
    throw "Source package directory '$SourceDirectory' does not exist."
}
if (Test-Path -LiteralPath $DestinationDirectory) {
    Remove-Item -LiteralPath $DestinationDirectory -Recurse -Force
}
New-Item -ItemType Directory -Path $DestinationDirectory -Force | Out-Null

$selected = @()
$packages = @(
    Get-ChildItem -LiteralPath $SourceDirectory -Filter '*.nupkg' -File |
        Where-Object { -not $_.Name.EndsWith('.symbols.nupkg', [System.StringComparison]::OrdinalIgnoreCase) } |
        Sort-Object Name
)

foreach ($package in $packages) {
    $metadata = Get-PackageMetadata -PackagePath $package.FullName
    if ($metadata.Version -ne $ExpectedVersion) {
        Write-Host "Skipping $($metadata.Id) $($metadata.Version); release tag expects $ExpectedVersion."
        continue
    }

    $destination = Join-Path $DestinationDirectory $package.Name
    Copy-Item -LiteralPath $package.FullName -Destination $destination
    $selected += $destination

    # dotnet nuget push discovers the companion symbol package beside the nupkg.
    $symbols = @(Get-ChildItem -LiteralPath $SourceDirectory -Filter '*.snupkg' -File |
        Where-Object { $_.BaseName -eq $package.BaseName })
    if ($package.BaseName -eq "Icod.Widgets.$ExpectedVersion" -and $symbols.Count -ne 1) {
        throw "Expected one companion symbol package for '$($package.Name)'; found $($symbols.Count)."
    }
    foreach ($symbol in $symbols) {
        Copy-Item -LiteralPath $symbol.FullName -Destination (Join-Path $DestinationDirectory $symbol.Name)
        Write-Host "Selected symbols $($symbol.Name)."
    }
    Write-Host "Selected $($metadata.Id) $($metadata.Version)."
}

$hasPackages = 0 -lt $selected.Count
if (-not [string]::IsNullOrWhiteSpace($GitHubOutputPath)) {
    "has_packages=$($hasPackages.ToString().ToLowerInvariant())" >> $GitHubOutputPath
    "package_count=$($selected.Count)" >> $GitHubOutputPath
}

Write-Host "Selected $($selected.Count) NuGet package(s) for release $ExpectedVersion."
