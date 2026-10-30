param([string]$ExpectedVersion)

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot
$version = (Get-Content -LiteralPath (Join-Path $root 'VERSION') -Raw).Trim()
if ($version -notmatch '^\d+\.\d+\.\d+$') { throw 'VERSION must use major.minor.patch format.' }
if ($ExpectedVersion -and $ExpectedVersion -ne $version) { throw "Tag version $ExpectedVersion does not match VERSION $version." }

& (Join-Path $root 'build.ps1') -Test
if ($LASTEXITCODE -ne 0) { throw 'Build or tests failed.' }

$release = Join-Path $root 'release'
if (Test-Path -LiteralPath $release) { Remove-Item -LiteralPath $release -Recurse -Force }
New-Item -ItemType Directory -Path $release | Out-Null

$portableName = "ReminderBuddy-Windows-$version"
$portable = Join-Path $release $portableName
New-Item -ItemType Directory -Path $portable | Out-Null
Copy-Item -LiteralPath (Join-Path $root 'dist\ReminderBuddy.exe') -Destination $portable
Copy-Item -LiteralPath (Join-Path $root 'LICENSE') -Destination $portable
Copy-Item -LiteralPath (Join-Path $root 'README.md') -Destination $portable
Copy-Item -LiteralPath (Join-Path $root 'VERSION') -Destination $portable
Compress-Archive -LiteralPath $portable -DestinationPath (Join-Path $release "$portableName.zip") -CompressionLevel Optimal
Remove-Item -LiteralPath $portable -Recurse -Force

$compilerCandidates = @(
    (Join-Path $env:LOCALAPPDATA 'Programs\Inno Setup 6\ISCC.exe'),
    (Join-Path $env:ProgramFiles 'Inno Setup 6\ISCC.exe'),
    (Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 6\ISCC.exe')
)
$iscc = @($compilerCandidates | Where-Object { $_ -and (Test-Path -LiteralPath $_) })[0]
if (!$iscc) { throw 'Inno Setup 6 is required. Install JRSoftware.InnoSetup with WinGet.' }

& $iscc "/DAppVersion=$version" "/DSourceRoot=$root" "/DOutputRoot=$release" (Join-Path $root 'installer\ReminderBuddy.iss')
if ($LASTEXITCODE -ne 0) { throw 'Installer compilation failed.' }

$artifacts = @(
    "ReminderBuddy-Setup-$version.exe",
    "$portableName.zip"
)
$checksumPath = Join-Path $release 'SHA256SUMS.txt'
$checksumLines = foreach ($artifact in $artifacts) {
    $path = Join-Path $release $artifact
    if (!(Test-Path -LiteralPath $path)) { throw "Expected release artifact is missing: $artifact" }
    $hash = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()
    "$hash  $artifact"
}
$checksumLines | Set-Content -LiteralPath $checksumPath -Encoding ASCII

"Release packages created in $release"
$artifacts + 'SHA256SUMS.txt' | ForEach-Object { "  $_" }
