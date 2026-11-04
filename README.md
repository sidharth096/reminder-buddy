# Reminder Buddy

> Friendly reminders delivered by animated characters on your desktop.

[![Latest release](https://img.shields.io/github/v/release/sidharth096/reminder-buddy?label=download&style=for-the-badge)](https://github.com/sidharth096/reminder-buddy/releases/latest)
[![Desktop CI](https://img.shields.io/github/actions/workflow/status/sidharth096/reminder-buddy/ci.yml?branch=main&label=tests&style=for-the-badge)](https://github.com/sidharth096/reminder-buddy/actions/workflows/ci.yml)
[![MIT license](https://img.shields.io/badge/license-MIT-165f49?style=for-the-badge)](LICENSE)

Reminder Buddy is for people who want noticeable reminders without opening another productivity app. Everything runs locally; no account or internet connection is needed after installation. Windows is supported; macOS support is an experimental beta pending real-Mac testing.

## Install

### Windows

1. Open the [latest release](https://github.com/sidharth096/reminder-buddy/releases/latest).
2. Download **`ReminderBuddy-Setup-<version>.exe`** from **Assets**.
3. Open the downloaded file and follow the setup wizard.

Reminder Buddy will be available from the Windows Start menu and **Settings → Apps → Installed apps**.
The installer also creates a desktop shortcut by default; uncheck that option during setup if you do not want one.

<details>
<summary>Install with PowerShell</summary>

```powershell
irm https://raw.githubusercontent.com/sidharth096/reminder-buddy/main/scripts/install-online.ps1 | iex
```

The script downloads the latest installer, verifies its SHA-256 checksum, and opens the setup wizard. You can [review the script](scripts/install-online.ps1) before running it.

</details>

### macOS Beta

Mac downloads become available after the cross-platform release workflow runs and its draft is published. Existing Windows-only releases do not contain them.

1. Open the [latest release](https://github.com/sidharth096/reminder-buddy/releases/latest).
2. Choose `ReminderBuddy-macOS-osx-arm64-<version>-beta.zip` for Apple Silicon, or `osx-x64` for Intel. Find the chip under **Apple menu > About This Mac**.
3. Extract the ZIP, move **Reminder Buddy.app** into **Applications**, and open it there.
4. Use its menu-bar icon to open settings, pause reminders, or quit.

These beta builds are ad-hoc signed, not signed with an Apple Developer ID or notarized by Apple. macOS may block opening them. For a build you trust, follow Apple's **Privacy & Security > Open Anyway** process if offered; never disable Gatekeeper globally. If your Mac does not allow the beta, wait for a Developer ID signed release.

The beta includes animated characters, schedules, snooze, sounds, and optional start at login. Full-screen deferral is currently Windows-only. Dock positioning, Spaces, and login behavior still need validation on a real Mac. Do not use the Mac beta for critical reminders.

## Create A Reminder

1. Select **+ New reminder**.
2. Enter a name and custom message.
3. Choose the days and either a specific time or repeating interval.
4. Select **Buddy**, **Abu**, **Shibu**, or **Tom**.
5. Choose whether it stays until closed or hides automatically.
6. Select **Preview animation**, then **Save reminder**.

Closing the settings window keeps Reminder Buddy running in the Windows tray or Mac menu bar. Use its icon to reopen settings, pause reminders, or exit.

## Features

- Fixed-time and repeating reminders with selectable weekdays
- Custom messages, sounds, active hours, and auto-hide duration
- Four original animated characters
- OK, Close, and 5/10/15-minute snooze actions
- Optional start with Windows and full-screen deferral
- Local settings with no account, tracking, or cloud storage

## Good To Know

- Reminder Buddy supports Windows 10 and 11; experimental Mac builds target macOS 15 or newer on Intel and Apple Silicon.
- The app must be running and the computer must be awake to show reminders.
- Windows security screens and some exclusive full-screen apps can hide overlays.
- Settings are stored in `%LOCALAPPDATA%\ReminderBuddy` and survive app updates.
- Mac settings are stored in `~/Library/Application Support/ReminderBuddy`. There is no automatic sync between computers.
- Downloads include the .NET runtime; users do not need to install .NET separately. Keep the entire extracted portable folder together.

## Help

Found a bug or have an idea? [Open a GitHub issue](https://github.com/sidharth096/reminder-buddy/issues).

Developers can use the [development and release guide](docs/DEVELOPMENT.md) to build, test, package, and publish the app.

Want to contribute? Read [`CONTRIBUTING.md`](CONTRIBUTING.md). See [`CHANGELOG.md`](CHANGELOG.md) for released and upcoming changes.

## License

Reminder Buddy is available under the [MIT License](LICENSE).
