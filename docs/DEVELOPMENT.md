# Development And Release Guide

> **Audience:** Developers who know Git and C# but are new to the Reminder Buddy repository.

Use this guide to build, test, package, and release Reminder Buddy. Ordinary users should follow the installation steps in the project README instead.

## Requirements

- Windows 10/11 for Windows UI testing, or macOS 15+ for Mac testing and packaging
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0); `global.json` selects the SDK family
- [Inno Setup 6](https://jrsoftware.org/isdl.php) for Windows release packaging

NuGet restores pinned Avalonia packages automatically. End-user packages are self-contained and do not require a separate runtime installation.

## Cross-Platform Development

```sh
dotnet build src/ReminderBuddy.csproj -c Release
dotnet run --project tests/CoreTests.csproj -c Release
dotnet run --project src/ReminderBuddy.csproj
```

`src/Core.cs` keeps the original JSON data contracts and schedules. `src/Platform.cs` isolates Windows registry/user32 and macOS login startup/sounds. The editor and vector characters use Avalonia on both platforms. Existing Windows settings remain in the same location.

For Mac packaging, run on macOS (GitHub runners can do this if you do not own a Mac):

```sh
bash scripts/package-macos.sh osx-arm64
bash scripts/package-macos.sh osx-x64
```

Each command produces a self-contained `.app` ZIP and architecture-specific checksums under `release/`. The script creates icons, validates the bundle metadata, and ad-hoc signs native binaries and the app. It does not use Developer ID signing or notarization. Local cross-compilation from Windows can validate managed code but cannot validate macOS launch behavior or run Apple's packaging tools.

Mac login startup creates only the current user's `~/Library/LaunchAgents/com.reminderbuddy.app.plist`; disabling removes it. Enable it after moving the app to Applications. If removing the app manually, disable login startup first or remove that plist. It takes effect on the next sign-in.

Mac beta limits: full-screen deferral is disabled, second-instance launch does not reopen hidden settings (use the menu-bar icon), and Dock/Spaces/focus behavior is not yet real-device verified. No screen recording or accessibility permission is requested.

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

GitHub Actions builds the app and runs core tests on Windows and macOS, and packages both Mac architectures on every push and pull request. The Windows UI smoke check remains local because hosted runners do not provide a reliable interactive desktop.

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

The tag triggers `.github/workflows/release.yml`. It validates the tag as data, tests and packages Windows plus both Mac architectures, then creates one draft GitHub Release with the installer, portable ZIP, Mac beta ZIPs, and combined checksums. Only the final release job has repository write permission.

Before publishing the draft:

1. Download the installer from the draft release.
2. Install it and open Reminder Buddy from the Start menu.
3. Create and preview a reminder with each character.
4. Confirm Reminder Buddy appears in **Settings → Apps → Installed apps**.
5. Uninstall it and confirm `%LOCALAPPDATA%\ReminderBuddy` is preserved.
6. Publish the draft only after these checks pass.

Mac public-release gate: download the archive in a browser on real Intel and Apple Silicon Macs; test launch approval, menu-bar visibility, all four characters, snooze, sounds, closing/reopening settings, sleep/wake, login startup on/off, secondary monitors, Dock at bottom/left/right and auto-hidden, and Spaces/full-screen transitions. Validate scaling and confirm reminders do not steal keyboard focus. Keep the beta label until verified. For smoother public distribution, configure Developer ID signing, minimal hardened-runtime entitlements, notarization, and ticket stapling in a later release; keep credentials in GitHub secrets, never source control.

## Manual QA

1. Create a fixed reminder for the next minute and confirm it appears above another app without interrupting typing.
2. Test persistent and timed notifications, all snooze options, and sounds.
3. Save two reminders for the same minute and confirm they display sequentially.
4. Test pause/resume, exit/relaunch, start with Windows, full-screen deferral, and sleep/wake.
5. Check 100%, 150%, and 200% display scaling and a moved or auto-hidden taskbar.
