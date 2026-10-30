$ErrorActionPreference = 'Stop'
if (Get-Process ReminderBuddy -ErrorAction SilentlyContinue) { throw 'Exit Reminder Buddy from its tray menu before uninstalling.' }
$installDirectory = Join-Path $env:LOCALAPPDATA 'Programs\ReminderBuddy'
$target = Join-Path $installDirectory 'ReminderBuddy.exe'
$shortcut = Join-Path ([Environment]::GetFolderPath('Programs')) 'Reminder Buddy.lnk'
$desktopShortcut = Join-Path ([Environment]::GetFolderPath('Desktop')) 'Reminder Buddy.lnk'
Remove-ItemProperty -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run' -Name ReminderBuddy -ErrorAction SilentlyContinue
if (Test-Path -LiteralPath $shortcut) { Remove-Item -LiteralPath $shortcut }
if (Test-Path -LiteralPath $desktopShortcut) { Remove-Item -LiteralPath $desktopShortcut }
if (Test-Path -LiteralPath $installDirectory) { Remove-Item -LiteralPath $installDirectory -Recurse }
'Uninstalled. Your reminder settings were kept in %LOCALAPPDATA%\ReminderBuddy.'
