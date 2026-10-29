$ErrorActionPreference = 'Stop'
$target = Join-Path $env:LOCALAPPDATA 'Programs\ReminderBuddy'
if (!(Test-Path -LiteralPath $env:LOCALAPPDATA)) { throw 'Local app data folder missing.' }
$source = Join-Path $PSScriptRoot 'dist\ReminderBuddy.exe'
if (!(Test-Path -LiteralPath $source)) { throw 'Run build.ps1 first.' }
if (Get-Process ReminderBuddy, TaskbarBuddy -ErrorAction SilentlyContinue) { throw 'Exit Reminder Buddy (and any older Taskbar Buddy version) from its tray menu before installing.' }
New-Item -ItemType Directory -Force -Path $target | Out-Null
Copy-Item -LiteralPath $source -Destination (Join-Path $target 'ReminderBuddy.exe') -Force
$shell = New-Object -ComObject WScript.Shell
$shortcut = $shell.CreateShortcut((Join-Path ([Environment]::GetFolderPath('Programs')) 'Reminder Buddy.lnk'))
$shortcut.TargetPath = Join-Path $target 'ReminderBuddy.exe'
$shortcut.WorkingDirectory = $target
$shortcut.Save()
$run = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run'
if ((Get-ItemProperty -Path $run -Name ReminderBuddy -ErrorAction SilentlyContinue) -or (Get-ItemProperty -Path $run -Name TaskbarBuddy -ErrorAction SilentlyContinue)) {
    Set-ItemProperty -Path $run -Name ReminderBuddy -Value ('"' + (Join-Path $target 'ReminderBuddy.exe') + '" --background')
    Remove-ItemProperty -Path $run -Name TaskbarBuddy -ErrorAction SilentlyContinue
}
$legacyShortcut = Join-Path ([Environment]::GetFolderPath('Programs')) 'Taskbar Buddy.lnk'
if (Test-Path -LiteralPath $legacyShortcut) { Remove-Item -LiteralPath $legacyShortcut }
"Installed: $target\ReminderBuddy.exe"
'Launch Reminder Buddy from the Start menu.'
