# HW Pulse
<img width="2555" height="796" alt="image" src="https://github.com/user-attachments/assets/2ff39789-228d-42e8-8ca2-5962972bc8bf" />

Full-screen, real-time view of the machine's status (CPU, GPU, RAM, disks and fans) for servers and PCs with an auxiliary display (designed for 1280×400). An alternative to the AIDA64 SensorPanel.

- **UI:** WinUI 3 (Windows App SDK) with the Windows 11 acrylic backdrop.
- **Sensors:** [LibreHardwareMonitor](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor). Requires administrator rights and the [PawnIO](https://pawnio.eu) driver for CPU temperatures and fans.

## Structure

| Folder | Content |
|---|---|
| `src/HwPulse.Sensors` | Hardware reading, no UI |
| `src/HwPulse.App` | Full-screen window |
| `tests/HwPulse.Tests` | Tests for the sensor logic |
| `install` | Inno Setup script for the installer |

## Development

```powershell
dotnet build HwPulse.sln
dotnet test HwPulse.sln
```

## Install

Download `HwPulse-Setup-x.y.z.exe` from [Releases](https://github.com/Jordi-N/HW-Pulse/releases/latest) and run it. It is not code-signed, so SmartScreen may warn: choose «More info» → «Run anyway». It installs to `C:\Program Files\HW Pulse`, installs the PawnIO driver if missing and starts the panel at logon with highest privileges, so no UAC prompt. Run a newer setup to update; `settings.json` is kept. Esc hides the panel to the tray icon; click it to show the panel again. Its right-click menu toggles «Iniciar con Windows» and exits. Right-click the panel to choose its screen; the choice is saved in `settings.json`. Until then it opens on the first screen that is not the primary one. The Start menu shortcut opens the panel without a UAC prompt.

### Background and local telemetry

Select **Arrancar en segundo plano, solo en la bandeja** in the installer to start with the panel hidden. Click the tray icon to open it and press Esc to hide it again. Select **Guardar telemetría local** to record measurements whether the panel is visible or hidden.

For unattended setup, use `HwPulse-Setup-x.y.z.exe /VERYSILENT /TASKS="background,telemetry"`. The tasks can also be enabled individually. For a manual launch as administrator, use `--background` and `--telemetry`.

Telemetry is versioned UTF-8 JSON Lines in the installation directory, normally `C:\Program Files\HW Pulse`. The account that runs the scheduled task can read the files without elevation. Administrators and SYSTEM can manage them.

| Files | Contents |
|---|---|
| `telemetry.jsonl`, `telemetry.previous.jsonl` | Hardware approximately once per second. Includes CPU package temperature, hottest physical core and package power where provided. Rotates at 16 MiB. |
| `processes.jsonl`, `processes.previous.jsonl` | PID, creation time, name, cumulative CPU milliseconds, resident RAM and cumulative Windows I/O transfer bytes approximately every five seconds. Rotates at 32 MiB. |

Each line has `schema_version: 1` and a UTC `captured_at`. Readers can skip an incomplete last line and read across rotation. Match PID and creation time before calculating deltas. Missing counters are `null`. Windows process I/O transfer counters include activity beyond physical disks. HW Pulse writes locally. An integrating client controls any onward transfer.

## Release

Push a version tag and the `Release` workflow builds, tests and publishes the installer:

```powershell
git tag v0.2.0
git push origin v0.2.0
```

To build the installer locally (needs [Inno Setup 6](https://jrsoftware.org/isinfo.php) and `install\PawnIO_setup.exe` from [PawnIO.Setup](https://github.com/namazso/PawnIO.Setup/releases)):

```powershell
dotnet publish src\HwPulse.App\HwPulse.App.csproj -c Release -o publish
& "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe" /DVersion=0.2.0 install\HwPulse.iss
```

## Optional configuration

`settings.json` next to the executable (`C:\Program Files\HW Pulse`). Restart the panel after editing it.

```json
{
  "fans": [
    { "sensor": "Fan #2", "label": "CPU" },
    { "sensor": "Fan #7", "label": "PUMP", "maxRpm": 3000 },
    { "sensor": "Fan #1", "label": "CHASIS" },
    { "sensor": "Fan #4", "label": "CHASIS" }
  ],
  "services": [
    { "name": "W3SVC", "label": "IIS" }
  ],
  "display": { "x": -1280, "y": 0 }
}
```

- **fans:** sensors sharing a label become one marker showing the average of those spinning, in the order of first appearance. Without `fans`, every spinning sensor is shown under its own name. The GPU fans are always added last as a `GPU` marker.
- **display:** written by the panel when you pick a screen with right-click: the top-left corner of that screen. If no connected screen has that corner, the panel uses the first screen that is not the primary one. Saving it rewrites the file and drops any `//` comments.
- **services:** Windows services to watch. Without `services`, IIS, Jellyfin and every GitHub Actions runner installed are shown, and a runner running a job reads «Ocupado».

## License

[MIT](LICENSE).

## Privacy

This program will not transfer any information to other networked systems unless specifically requested by the user or the person installing or operating it.
