# Reminder Buddy Implementation Plan

**Goal:** A local Windows reminder application with a walking desktop character, custom schedules, and tray controls.

**Architecture:** WPF settings and a small transparent, non-activating topmost notification window. A dispatcher scheduler evaluates local-time schedules, queues notifications, and persists settings and delivery markers atomically. Use the Windows-provided .NET Framework compiler/runtime without external packages.

**Tech Stack:** C# / .NET Framework 4.x, WPF, Windows Forms NotifyIcon, JSON via DataContractJsonSerializer, PowerShell build and installation scripts.

## 1. Model and scheduling
- Create `src/Core.cs`: validated reminder models, weekday masks, fixed and interval schedules (including overnight windows), persisted delivery markers, local JSON storage.
- Create `tests/CoreTests.cs`: executable assertions covering fixed times, selected days, interval boundaries, overnight windows, clock jumps, snooze timing and persistence.
- Verify with `powershell -ExecutionPolicy Bypass -File .\build.ps1 -Test`.

## 2. Settings and background lifetime
- Create `src/App.cs` for single-instance startup, tray, pause/resume, full-screen deferral, queue management, optional startup and exception logging.
- Create `src/SettingsWindow.cs` with reminder list/editor, validation, preview, duplicate/delete and application preferences.
- Use `%LOCALAPPDATA%\ReminderBuddy` for settings; closing settings hides to tray.

## 3. Character overlay
- Create `src/OverlayWindow.cs`: original vector walking character and speech bubble in a compact topmost no-focus overlay.
- Persistent reminders walk in then stop. Timed reminders walk across and hide. Provide OK, Close and 5/10/15-minute snooze.
- Place within the primary display working area, track taskbar/display changes and keep transparent margins non-interactive.

## 4. Build and delivery
- Create `build.ps1`, `install.ps1`, `uninstall.ps1`, `README.md`, and `app.manifest`.
- Compile application and assertion tests using the Windows compiler. Install per-user with Start Menu shortcuts; no administrator access required.
- Run scheduler/persistence assertions, a real WPF UI smoke test, and package the portable files.

## Agreed behavior
- Each reminder: name, message, weekdays, time or interval, active hours, persistent or timed display, optional system sound, enabled state.
- Tray: settings, pause 30/60 minutes/until tomorrow, resume, exit. Optional sign-in startup.
- One notification at a time; do not replay a backlog after sleep; persist last delivery to avoid restart duplicates.
- Full-screen deferral is optional. Overlay shows above normal apps without keyboard focus; secure desktop/exclusive full-screen applications are outside its control.
- Work status is reminder-only. Additional characters, arbitrary monitor selection and import/export are later scope.
- No commits or repository changes requested.
