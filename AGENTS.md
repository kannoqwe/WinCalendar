# AGENTS.md

## Project Overview

WinCalendar is a Windows calendar and planner application built with .NET 8 and WinUI.

Main solution:

- `WinCalendar/WinCalendar.slnx`

Main app project:

- `WinCalendar/WinCalendar/WinCalendar.csproj`

Test project:

- `WinCalendar/WinCalendar.Tests/WinCalendar.Tests.csproj`

## Repository Layout

- `WinCalendar/WinCalendar/Core` - shared abstractions and core utilities.
- `WinCalendar/WinCalendar/Bootstrap` - application startup and composition.
- `WinCalendar/WinCalendar/Shared` - shared UI or app-level helpers.
- `WinCalendar/WinCalendar/Modules/Calendar` - calendar UI.
- `WinCalendar/WinCalendar/Modules/Planner` - planner state, view models, compact planner UI, dialogs, notes, settings, and today view.
- `WinCalendar/WinCalendar/Modules/Shell` - tray, taskbar, overlay, and shell integration.
- `WinCalendar/WinCalendar/Modules/Tasks` - task entity, repository contracts, SQLite infrastructure, and task use cases.
- `WinCalendar/WinCalendar.Tests` - lightweight console-based tests.

## Build And Test

Run commands from the repository root unless a task says otherwise.

Build the solution:

```powershell
dotnet build WinCalendar/WinCalendar.slnx
```

Run tests:

```powershell
dotnet run --project WinCalendar/WinCalendar.Tests/WinCalendar.Tests.csproj
```

When changing UI-only XAML, still build the app project before finishing. When changing task domain logic or use cases, run the test project too.

## Coding Rules

- Keep changes small and focused on the requested behavior.
- Follow the existing module boundaries: UI code stays in `UI`, presentation state stays in `Presentation`, domain and use-case logic stays in `Modules/Tasks`.
- Prefer existing project patterns over adding new abstractions.
- Keep nullable reference types clean.
- Do not introduce unrelated formatting churn.
- Avoid broad refactors unless the task explicitly requires them.
- Add or update tests when changing task entity behavior, task use cases, date/time behavior, or repository-facing contracts.
- Be careful with WinUI sizing and DPI-sensitive code; verify compact planner and overlay changes visually when possible.

## Git Rules

- Prefer many small commits over one large commit.
- Each commit should represent one logical change that can be reviewed and reverted independently.
- Use clear conventional-style commit messages already present in the history, for example:
  - `feat(planner): add task duration picker`
  - `fix(shell): sync overlay from window events`
  - `refactor(planner): simplify compact layout`
  - `test(tasks): cover completion timestamp`
- Do not mix unrelated UI, infrastructure, and test changes in the same commit.
- Before committing, check `git status --short` and review the diff.
- Do not commit generated build output, local IDE files, or machine-specific state.
- If a requested change is risky, commit the safe preparatory steps separately before the behavioral change.

## Agent Workflow

- Inspect the current worktree before editing.
- Never overwrite user changes unless the user explicitly asks to discard uncommitted work.
- Prefer `rg` for searches when available; use PowerShell file commands as a fallback.
- Explain important assumptions in the final response.
- Leave the repository in a buildable state whenever possible.
