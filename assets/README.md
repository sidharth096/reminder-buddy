# Brand Assets

- `ReminderBuddy.svg` is the editable source for the Reminder Buddy app icon.
- `ReminderBuddy.ico` is the generated multi-size Windows icon used by the app and installer.

The icon is original artwork created for Reminder Buddy. It is distributed under the repository's [MIT License](../LICENSE), so contributors can modify and redistribute it with the project.

After editing the SVG, update the PowerShell drawing instructions in `scripts/generate-icon.ps1`, then regenerate the ICO from the repository root:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\generate-icon.ps1
```
