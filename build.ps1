param([switch]$Test)
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$compiler = "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$framework = Split-Path $compiler
if (!(Test-Path -LiteralPath $root)) { throw 'Project folder missing.' }
if (!(Test-Path -LiteralPath $compiler)) { throw '.NET Framework 4.x is required.' }
$output = Join-Path $root 'dist'
New-Item -ItemType Directory -Force -Path $output | Out-Null
$refs = @('System.dll', 'System.Core.dll', 'System.Runtime.Serialization.dll', 'System.Xml.dll', 'System.Xaml.dll', 'System.Windows.Forms.dll', 'System.Drawing.dll', 'WPF\WindowsBase.dll', 'WPF\PresentationCore.dll', 'WPF\PresentationFramework.dll') | ForEach-Object { '/reference:' + (Join-Path $framework $_) }
$sources = @('Core.cs', 'App.cs', 'SettingsWindow.cs', 'OverlayWindow.cs') | ForEach-Object { Join-Path $root "src\$_" }
& $compiler /nologo /target:winexe /optimize+ /platform:anycpu "/win32manifest:$root\app.manifest" "/out:$output\ReminderBuddy.exe" $refs $sources
if ($LASTEXITCODE -ne 0) { throw 'Application compilation failed.' }
if ($Test) {
    & $compiler /nologo /target:exe "/out:$output\CoreTests.exe" $refs "$root\src\Core.cs" "$root\tests\CoreTests.cs"
    if ($LASTEXITCODE -ne 0) { throw 'Test compilation failed.' }
    & "$output\CoreTests.exe"
    if ($LASTEXITCODE -ne 0) { throw 'Tests failed.' }
}
"Built: $output\ReminderBuddy.exe"
