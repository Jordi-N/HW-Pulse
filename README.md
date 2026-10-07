# HW Pulse

Full-screen, real-time view of the machine's status (CPU, GPU, RAM, disks and fans) for servers and PCs with an auxiliary display (designed for 1280×400). An alternative to the AIDA64 SensorPanel.

- **UI:** WinUI 3 (Windows App SDK) with the Windows 11 acrylic backdrop.
- **Sensors:** [LibreHardwareMonitor](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor). Requires administrator rights and the [PawnIO](https://pawnio.eu) driver for CPU temperatures and fans.

## Structure

| Folder | Content |
|---|---|
| `src/HwPulse.Sensors` | Hardware reading, no UI |
| `src/HwPulse.App` | Full-screen window |
| `tests/HwPulse.Tests` | Tests for the sensor logic |
| `install` | Publishing and automatic startup |

## Development

```powershell
dotnet build HwPulse.sln
dotnet test HwPulse.sln
```

## Install

From an administrator PowerShell:

```powershell
.\install\instalar.ps1
```

It publishes to `C:\Program Files\HW Pulse`, installs PawnIO with winget and registers a logon task with highest privileges. Run it again to update. Press Esc to close the window.

## Optional configuration

`settings.json` next to the executable, to name the fans:

```json
{
  "fans": [
    { "sensor": "Fan #2", "label": "PUMP", "maxRpm": 3000 }
  ]
}
```
