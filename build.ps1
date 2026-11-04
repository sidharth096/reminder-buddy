param([switch]$Test, [string]$Runtime = 'win-x64')
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
if (!(Test-Path -LiteralPath $root)) { throw 'Project folder missing.' }
if (!(Get-Command dotnet -ErrorAction SilentlyContinue)) { throw 'Install the .NET 10 SDK, then reopen PowerShell.' }
if ($Runtime -notin @('win-x64', 'win-arm64')) { throw 'Use win-x64 or win-arm64. For macOS, use scripts/package-macos.sh.' }
$output = Join-Path $root 'dist'
$version = (Get-Content -LiteralPath (Join-Path $root 'VERSION') -Raw).Trim()
if ($version -notmatch '^\d+\.\d+\.\d+$') { throw 'VERSION must use major.minor.patch format.' }
if ($Test) {
    dotnet run --project (Join-Path $root 'tests\CoreTests.csproj') -c Release
    if ($LASTEXITCODE -ne 0) { throw 'Tests failed.' }
}
if (Get-Process ReminderBuddy -ErrorAction SilentlyContinue | Where-Object { $_.Path -eq (Join-Path $output 'ReminderBuddy.exe') }) { throw 'Exit the dist copy of Reminder Buddy before rebuilding it.' }
if (Test-Path -LiteralPath $output) { Remove-Item -LiteralPath $output -Recurse -Force }
dotnet publish (Join-Path $root 'src\ReminderBuddy.csproj') -c Release -r $Runtime --self-contained true -o $output
if ($LASTEXITCODE -ne 0) { throw 'Application publish failed.' }
"Built: $output\ReminderBuddy.exe"
