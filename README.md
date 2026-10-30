# Reminder Buddy

> Friendly reminders delivered by animated characters above your Windows taskbar.

[![Latest release](https://img.shields.io/github/v/release/sidharth096/reminder-buddy?label=download&style=for-the-badge)](https://github.com/sidharth096/reminder-buddy/releases/latest)
[![Windows CI](https://img.shields.io/github/actions/workflow/status/sidharth096/reminder-buddy/ci.yml?branch=main&label=tests&style=for-the-badge)](https://github.com/sidharth096/reminder-buddy/actions/workflows/ci.yml)
[![MIT license](https://img.shields.io/badge/license-MIT-165f49?style=for-the-badge)](LICENSE)

Reminder Buddy is for Windows users who want noticeable reminders without opening another productivity app. Everything runs locally; no account or internet connection is needed after installation.

## Install

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

## Create A Reminder

1. Select **+ New reminder**.
2. Enter a name and custom message.
3. Choose the days and either a specific time or repeating interval.
4. Select **Buddy**, **Abu**, **Shibu**, or **Tom**.
5. Choose whether it stays until closed or hides automatically.
6. Select **Preview animation**, then **Save reminder**.

Closing the settings window keeps Reminder Buddy running in the system tray. Double-click the tray icon to reopen it, or right-click it to pause reminders and exit.

## Features

- Fixed-time and repeating reminders with selectable weekdays
- Custom messages, sounds, active hours, and auto-hide duration
- Four original animated characters
- OK, Close, and 5/10/15-minute snooze actions
- Optional start with Windows and full-screen deferral
- Local settings with no account, tracking, or cloud storage

## Good To Know

- Reminder Buddy supports Windows 10 and 11.
- The app must be running and the computer must be awake to show reminders.
- Windows security screens and some exclusive full-screen apps can hide overlays.
- Settings are stored in `%LOCALAPPDATA%\ReminderBuddy` and survive app updates.

## Help

Found a bug or have an idea? [Open a GitHub issue](https://github.com/sidharth096/reminder-buddy/issues).

Developers can use the [development and release guide](docs/DEVELOPMENT.md) to build, test, package, and publish the app.

## License

Reminder Buddy is available under the [MIT License](LICENSE).
