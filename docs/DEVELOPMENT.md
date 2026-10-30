# Development And Release Guide

> **Audience:** Windows developers who know Git and PowerShell but are new to the Reminder Buddy repository.

Use this guide to build, test, package, and release Reminder Buddy. Ordinary users should follow the installation steps in the project README instead.

## Requirements

- Windows 10 or 11
- .NET Framework 4.x, included with supported Windows versions
- [Inno Setup 6](https://jrsoftware.org/isdl.php) for release packaging

There are no NuGet dependencies or .NET SDK installation steps.

## App Icon

`assets\ReminderBuddy.svg` is the editable, open-source icon source. Regenerate the multi-size Windows icon after changing it:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\generate-icon.ps1
```

Expected result: `assets\ReminderBuddy.ico` contains 16, 24, 32, 48, 64, 128, and 256-pixel images. The build embeds it in the executable, and the installer uses the same file.

## Build And Test

Run from the repository root:

```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1 -Test
```

Expected result: all core checks pass and `dist\ReminderBuddy.exe` is created.

Run the interactive UI smoke check locally:

```powershell
powershell -ExecutionPolicy Bypass -File .\tests\UiSmoke.ps1
```

Expected result: the settings and reminder windows open, the test checks all four characters and notification actions, and the app exits automatically. If the build reports that `ReminderBuddy.exe` is in use, exit Reminder Buddy from its system-tray menu and rerun it.

GitHub Actions runs `build.ps1 -Test` on every push and pull request through `.github/workflows/ci.yml`. The UI smoke check remains local because hosted runners do not provide a reliable interactive desktop.

## Local Developer Installation

Build the app first, then run:

```powershell
powershell -ExecutionPolicy Bypass -File .\install.ps1
```

This installs the current build under `%LOCALAPPDATA%\Programs\ReminderBuddy` and creates a Start menu shortcut.

To uninstall the developer installation:

```powershell
powershell -ExecutionPolicy Bypass -File .\uninstall.ps1
```

Reminder settings remain in `%LOCALAPPDATA%\ReminderBuddy`.

## Build Release Packages

`VERSION` is the single source for the release version and must use `major.minor.patch` format, such as `0.2.0`.

Run:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\package.ps1
```

Expected files in `release\`:

```text
ReminderBuddy-Setup-<version>.exe
ReminderBuddy-Windows-<version>.zip
SHA256SUMS.txt
```

The package script builds and tests the app before creating the installer, portable ZIP, and SHA-256 checksums.

## Release A Version

1. Update `VERSION` to the new version.
2. Run `scripts\package.ps1` and test the generated installer.
3. Commit and push the version change.
4. Wait for **Windows CI** to pass on GitHub.
5. Create and push a matching tag:

```powershell
git tag v0.2.0
git push origin v0.2.0
```

Replace `0.2.0` with the value in `VERSION`.

The tag triggers `.github/workflows/release.yml`. It tests and packages the tagged commit, then creates a draft GitHub Release with the installer, portable ZIP, and checksums.

Before publishing the draft:

1. Download the installer from the draft release.
2. Install it and open Reminder Buddy from the Start menu.
3. Create and preview a reminder with each character.
4. Confirm Reminder Buddy appears in **Settings → Apps → Installed apps**.
5. Uninstall it and confirm `%LOCALAPPDATA%\ReminderBuddy` is preserved.
6. Publish the draft only after these checks pass.

## Manual QA

1. Create a fixed reminder for the next minute and confirm it appears above another app without interrupting typing.
2. Test persistent and timed notifications, all snooze options, and sounds.
3. Save two reminders for the same minute and confirm they display sequentially.
4. Test pause/resume, exit/relaunch, start with Windows, full-screen deferral, and sleep/wake.
5. Check 100%, 150%, and 200% display scaling and a moved or auto-hidden taskbar.
