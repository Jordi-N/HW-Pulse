# HW Pulse — agent rules

Full-screen hardware monitor for a 1280×400 auxiliary display, replacing the AIDA64 SensorPanel. Runs on Windows servers and desktop PCs.

## Layout

| Path | Content |
|---|---|
| `src/HwPulse.Sensors` | Hardware reading (LibreHardwareMonitorLib + PawnIO driver). No UI. |
| `src/HwPulse.App` | WinUI 3 unpackaged, self-contained, full-screen window. |
| `tests/HwPulse.Tests` | xUnit tests for the pure logic in Sensors. |
| `install` | Inno Setup script: installs the app and PawnIO and registers the logon task. Built by `.github/workflows/release.yml` on `v*` tags. |

```powershell
dotnet build HwPulse.sln
dotnet test HwPulse.sln
```

## Language

Think and code in English: identifiers, test names and instruction documents (this file). Code comments are sparse and in Spanish. User messages, commits and UI strings are in Spanish, keeping technical terms and identifiers unchanged.

## Rules

| Rule | Requirement |
|---|---|
| R1 | Fix causes. Never suppress analyzer warnings (`#pragma`, `NoWarn`, `.editorconfig` severity, `SuppressMessage`), add empty catches, loosen `Directory.Build.props` or skip/weaken/delete tests to pass. Warnings are errors; keep it that way. |
| R2 | Choose the simplest complete solution for current requirements. No speculative abstraction, interfaces with one implementation, unused options or forwarding wrappers. |
| R3 | One canonical path. Renames and moves update every consumer and remove the old code in the same change. No compatibility shims. |
| R4 | Size limits: at most 1000 lines per file, 300 per method, cognitive complexity 15. Split by responsibility, never by line count. Above 700 lines, move any new responsibility out. |
| R5 | Reuse first: search the repo for similar behaviour before writing. Hardware logic lives in Sensors, presentation in App. |
| R6 | Delete everything a change leaves unused: code, XAML resources, tests, packages and settings. Git history is the only backup. |
| R7 | Tests: new logic in Sensors gets xUnit tests; anything that can be tested without hardware or UI belongs in a pure function so it can be. Every run reports zero skipped tests. Run `dotnet test HwPulse.sln` before declaring work done. |
| R8 | Read the installed library docs and types before using an API (LibreHardwareMonitor, Windows App SDK). Prefer maintained libraries over reimplementing hidden complexity. |
| R9 | Grow through complete working layers. Never leave the app between unfinished designs. |
| R10 | Never commit or push unless asked; "commit" includes push. Stage explicit task paths only. Destructive git commands require a literal request. |
| R11 | Product decisions (what is shown, layout, colours) belong to the user; technical decisions are yours. |

After three failed fixes, stop and name the doubtful assumption.

## Platform notes

- Sensors need administrator rights; CPU temperatures and fans also need the PawnIO driver. Without it the app shows a warning instead of zeros: missing values are `null`, rendered as «—».
- The app is unpackaged (`WindowsPackageType=None`) and self-contained, so it runs on Windows Server without the Windows App SDK runtime installed.
