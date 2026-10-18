param(
	[string]$Configuration = "Release",
	[switch]$Verify
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repositoryRoot "Icod.Widgets.csproj"
[xml]$project = Get-Content -LiteralPath $projectPath
$packageVersion = [string]$project.SelectSingleNode("/Project/PropertyGroup/PackageVersion").InnerText
$assemblyVersion = [string]$project.SelectSingleNode("/Project/PropertyGroup/AssemblyVersion").InnerText
$majorMinor = ($packageVersion.Split('.')[0..1] -join '.')
$fingerprintPath = Join-Path $repositoryRoot ("docs/Public-API-Fingerprint-{0}.json" -f $majorMinor)
$helperPath = Join-Path $PSScriptRoot "PublicApiFingerprint.cs"
$temporaryRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("Icod.Widgets-api-{0}" -f [Guid]::NewGuid().ToString("N"))
$targets = @("net8.0", "net9.0", "net10.0")

try {
	New-Item -ItemType Directory -Path $temporaryRoot | Out-Null
	$generated = @()
	foreach ($target in $targets) {
		$assemblyPath = Join-Path $repositoryRoot ("bin/{0}/{1}/Icod.Widgets.dll" -f $Configuration, $target)
		if (-not (Test-Path -LiteralPath $assemblyPath)) {
			throw "Missing $target assembly: $assemblyPath. Build $Configuration first."
		}
		$outputPath = Join-Path $temporaryRoot ("Public-API-{0}.json" -f $target)
		& dotnet run --file $helperPath -- $assemblyPath $packageVersion $outputPath
		if ($LASTEXITCODE -ne 0) {
			throw "Public API reflection failed for $target."
		}
		$generated += $outputPath
	}

	$reference = Get-Content -LiteralPath $generated[0] -Raw
	foreach ($candidate in $generated[1..($generated.Count - 1)]) {
		if ($reference -cne (Get-Content -LiteralPath $candidate -Raw)) {
			throw "Public API differs across net8.0, net9.0, and net10.0."
		}
	}

	$document = $reference | ConvertFrom-Json
	if ([string]$document.packageVersion -cne $packageVersion) {
		throw "Generated package version does not match $packageVersion."
	}
	if ([string]$document.assemblyVersion -cne $assemblyVersion) {
		throw "Generated assembly version does not match $assemblyVersion."
	}

	if ($Verify) {
		if (-not (Test-Path -LiteralPath $fingerprintPath)) {
			throw "Missing stored fingerprint: $fingerprintPath"
		}
		if ($reference -cne (Get-Content -LiteralPath $fingerprintPath -Raw)) {
			throw "Stored public API fingerprint is stale: $fingerprintPath"
		}
		Write-Host "Verified identical $packageVersion public API for $($targets -join ', ')."
	} else {
		[System.IO.File]::WriteAllText($fingerprintPath, $reference)
		Write-Host "Wrote $fingerprintPath from identical $($targets -join ', ') surfaces."
	}
} finally {
	if (Test-Path -LiteralPath $temporaryRoot) {
		Remove-Item -LiteralPath $temporaryRoot -Recurse -Force
	}
}
