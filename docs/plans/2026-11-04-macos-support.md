# macOS Support Implementation Plan

**Goal:** Deliver the existing animated reminder experience on Windows and macOS, with downloadable Apple Silicon and Intel beta builds.

**Architecture:** Use one Avalonia desktop app on .NET 10. Retain the existing data contracts and scheduling rules. Keep OS-specific startup, sound, and foreground-window behavior in a small platform helper.

**Tech Stack:** C#, .NET 10 LTS, Avalonia 11.3, Inno Setup, GitHub Actions macOS runners.

## 1. Port the desktop application
- Add `src/ReminderBuddy.csproj`, `src/Program.cs`, and `tests/CoreTests.csproj`.
- Port `src/App.cs`, `src/SettingsWindow.cs`, and `src/OverlayWindow.cs`; retain layout, characters, and actions.
- Add asynchronous confirmation/error dialogs, native tray/menu-bar integration, and explicit application lifetime.
- Keep the Windows settings path and JSON representation. Use Application Support on macOS.
- Verify with `dotnet build src/ReminderBuddy.csproj` and `dotnet run --project tests/CoreTests.csproj`.

## 2. Integrate the operating systems
- Add `src/Platform.cs` for per-user login startup, sounds, fullscreen checks, and overlay behavior.
- Never request administrator privileges, screen recording, or accessibility permissions.
- Test persistence of existing settings, malformed snoozes, and overlay placement independently of native APIs.

## 3. Package and automate
- Update `build.ps1`, `scripts/package.ps1`, and `installer/ReminderBuddy.iss` for self-contained Windows publish output.
- Add `scripts/package-macos.sh` and `installer/macos/Info.plist` for `.app` bundles and architecture-specific archives.
- Build and test both platforms in `.github/workflows/ci.yml`; create one draft release after all packaging jobs succeed.
- Pass GitHub tag values through environment variables, validate strictly, and keep write permission confined to release creation.
- Mac builds are initially ad-hoc signed beta builds, not Developer ID signed or notarized.

## 4. Verify and document
- Run core tests, Windows build, self-contained publish, UI smoke checks, and installer compilation.
- Cross-publish Mac architectures locally; GitHub must verify packaging on macOS.
- Update README, development instructions, and changelog with downloads, prerequisites, and beta limitations.
- A real Mac tester must verify Dock placement (including hidden/side Dock), menu-bar actions, no focus stealing, fullscreen, sleep/wake, login startup, and upgrade behavior before production readiness.

No automatic commits, branch changes, or publishing. Apple Developer signing/notarization is a later public-release step.
