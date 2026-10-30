# Reminder Buddy

A Windows reminder app with an original walking character above the taskbar. No account, internet connection, or downloaded character assets required.

## Run

Double-click `dist\ReminderBuddy.exe`. Choose **+ New reminder**, set the name, message, weekdays and schedule, then **Save reminder**. **Preview animation** tests the current editor values without saving.

Closing the settings window keeps the app running in the system tray. Double-click the tray icon to reopen settings; right-click to pause, resume, or exit. Windows may place the icon in the tray overflow menu.

## Features

- Fixed-time or interval schedules with selected weekdays and active hours, including overnight ranges.
- Custom messages, optional system sounds, enable/disable, duplicate, delete and preview.
- Per-reminder character selection: Buddy (default), Abu, Shibu, and Tom.
- Persistent reminders walk in and stop; timed reminders walk left to right and auto-hide.
- OK, Close and 5/10/15-minute snooze. Preview snooze only dismisses the preview.
- One notification at a time, tray pause controls, optional sign-in startup and full-screen deferral.
- Transparent topmost notification without stealing keyboard focus. Clickable message controls; transparent surroundings do not block other applications.
- Local settings, atomic saves, backup recovery and single-instance operation.

## Schedule details

Times use the PC's local clock in 24-hour format. Interval reminders fire at the start time and repeat until, but not including, the end time. Overnight windows belong to the weekday on which they start. Saving an edited reminder arms it for future occurrences.

After sleep, only a recent occurrence within five minutes is eligible, with at most one catch-up notification after a long clock gap. Older scheduled occurrences are skipped. Snoozes survive restarts, but expire after twelve hours overdue. While paused, occurrences are skipped; deferred full-screen notifications wait in a deduplicated in-memory queue. Queued, unshown notifications are not retained after exit. The app must be running and the PC awake to display a reminder; it does not wake the PC.

The first version uses the primary monitor's working area. Standard apps are supported; the Windows security desktop and exclusive full-screen applications may prevent overlays. Extra characters, adjustable character size/speed, monitor selection and import/export are future scope. The supplied character is original vector artwork rather than the football player in the reference image.

### Characters

All four characters are original vector drawings rendered by WPF, so there are no image files to install and no flat pictures sliding across the screen. Arms and legs animate independently during movement, while persistent reminders switch to a standing pose. The characters use original names and artwork without team crests, sponsor marks, or imported photographs.

## Install

Download the latest `ReminderBuddy-Setup-<version>.exe` from [GitHub Releases](https://github.com/sidharth096/reminder-buddy/releases), double-click it, and follow the setup wizard. The installer adds Reminder Buddy to the Start Menu and Windows Installed apps. Administrator access is not required for the default per-user installation.

After the first public release is published, PowerShell users can download the latest installer and verify its SHA-256 checksum with:

```powershell
irm https://raw.githubusercontent.com/sidharth096/reminder-buddy/main/scripts/install-online.ps1 | iex
```

Review [`scripts/install-online.ps1`](scripts/install-online.ps1) before running a script from the internet. The script downloads both the installer and `SHA256SUMS.txt` from the latest GitHub Release, refuses a checksum mismatch, and then opens the normal setup wizard.

### Developer installation

Run in PowerShell from this folder:

```powershell
powershell -ExecutionPolicy Bypass -File .\install.ps1
```

This copies the executable into `%LOCALAPPDATA%\Programs\ReminderBuddy` and creates a Start Menu shortcut. No administrator rights required. Enable **Start with Windows** after installing so startup uses the installed path.

To remove the installed app, exit from the tray menu and run:

```powershell
powershell -ExecutionPolicy Bypass -File .\uninstall.ps1
```

Your reminders are retained in `%LOCALAPPDATA%\ReminderBuddy\settings.json`. The previous save is `settings.json.bak`. Errors are recorded in `error.log` in the same folder.

Upgrading from Taskbar Buddy: exit the older app first. On first launch, Reminder Buddy copies your old settings and backup if no new settings exist, keeping the originals intact. The installer also migrates an enabled startup entry and replaces the old Start Menu shortcut.

## Build and check

Windows 10/11 with .NET Framework 4.x is required. There are no NuGet dependencies or SDK installation steps.

```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1 -Test
.\dist\ReminderBuddy.exe --smoke-test
powershell -ExecutionPolicy Bypass -File .\tests\UiSmoke.ps1
```

The smoke mode opens the real settings window and a five-second preview, then exits after eight seconds without changing reminders. Source is under `src`; scheduler and persistence assertions are in `tests\CoreTests.cs`.

GitHub Actions runs `build.ps1 -Test` on every push and pull request using `.github/workflows/ci.yml`. Successful runs provide a temporary `ReminderBuddy-Windows` build artifact. The interactive UI smoke test remains a local check because hosted runners do not provide a reliable interactive Windows desktop.

## Package and release

Install Inno Setup 6, then create the installer, portable ZIP, and SHA-256 checksum file locally:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\package.ps1
```

Artifacts are written to `release\`. `VERSION` is the single release version source. To publish a release, update `VERSION`, commit and push the change, wait for Windows CI to pass, then push a matching tag such as `v0.1.0`. `.github/workflows/release.yml` tests and packages that exact commit and creates a draft GitHub Release. Download and test the draft installer before publishing it.

## Manual QA

1. Create a fixed reminder for the next minute; verify it appears over another application without interrupting typing.
2. Preview persistent and timed modes, and verify OK, Close, every snooze option and sound.
3. Save two reminders for the same minute; confirm they display sequentially.
4. Test full-screen deferral, pause/resume, exit/relaunch, Windows startup and sleep/wake.
5. Check 100%, 150% and 200% display scaling and a moved/auto-hidden taskbar.
