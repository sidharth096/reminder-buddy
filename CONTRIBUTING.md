# Contributing To Reminder Buddy

> **Audience:** Contributors who know Git and PowerShell but are new to the Reminder Buddy codebase.

Thanks for helping improve Reminder Buddy. Bug fixes, accessibility improvements, documentation updates, tests, and focused feature proposals are welcome.

## Before You Start

- Search [existing issues](https://github.com/sidharth096/reminder-buddy/issues) before opening a new one.
- Open a feature request before implementing a large behavior or UI change.
- Never commit generated files from `dist\` or `release\`.
- Keep changes focused; unrelated fixes belong in separate pull requests.

## Set Up The Project

1. Fork and clone the repository.
2. Create a branch from `main`.
3. Follow [`docs/DEVELOPMENT.md`](docs/DEVELOPMENT.md) to build and test the app.

Run the required checks before opening a pull request:

```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1 -Test
powershell -ExecutionPolicy Bypass -File .\tests\UiSmoke.ps1
```

Expected result: all core checks pass, the UI smoke test verifies all four characters and notification actions, and the app exits automatically.

## Submit A Pull Request

1. Describe the user-visible problem and the chosen solution.
2. Include screenshots for interface changes.
3. Add or update the smallest test that proves non-trivial behavior.
4. Update `README.md`, `docs\DEVELOPMENT.md`, or `CHANGELOG.md` when behavior or contributor steps change.
5. Confirm the **Windows CI** check passes.

By contributing, you agree that your contribution is licensed under the repository's [MIT License](LICENSE) and that you will follow the [Code of Conduct](CODE_OF_CONDUCT.md).

## Report Security Problems

Do not open a public issue for a vulnerability. Follow [`SECURITY.md`](SECURITY.md) instead.
