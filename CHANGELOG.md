# Changelog

Notable changes to Reminder Buddy are recorded here. This project follows [Semantic Versioning](https://semver.org/).

## Unreleased

### Added

- Shared Avalonia desktop app with experimental Intel and Apple Silicon macOS builds
- Mac menu-bar controls, system sounds, per-user login startup, and local Application Support storage
- Cross-platform builds/tests and ad-hoc signed Mac beta archives from GitHub Actions
- Original Reminder Buddy app icon for the executable, window, tray, installer, and shortcuts
- Desktop shortcut enabled by default during installation
- User-focused README and separate developer guide
- Contribution, security, conduct, issue, and pull-request guidance

### Changed

- Windows packages now include the .NET 10 runtime and all application files
- Release workflows validate version tags as data and limit write permission to draft-release creation
- Mac full-screen deferral remains unavailable pending a tested native implementation

## 0.1.0 - 2026-10-30

### Added

- Fixed-time and repeating reminders with weekdays and active hours
- Custom messages, optional sounds, snooze actions, and persistent or timed display
- Animated Buddy, Abu, Shibu, and Tom characters
- System-tray controls, start-with-Windows option, and full-screen deferral
- Local settings with atomic saves and backup recovery
- Windows installer, portable package, SHA-256 checksums, and release automation
- Automated core checks and Windows CI

[Unreleased]: https://github.com/sidharth096/reminder-buddy/compare/v0.1.0...HEAD
