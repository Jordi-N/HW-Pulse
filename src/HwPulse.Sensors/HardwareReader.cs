using LibreHardwareMonitor.Hardware;
using LibreHardwareMonitor.Hardware.Storage;
using LibreHardwareMonitor.PawnIo;

namespace HwPulse.Sensors;

// Única puerta a LibreHardwareMonitor. Necesita ejecutarse como administrador; sin el driver PawnIO
// siguen llegando discos, RAM y la gráfica NVIDIA, pero no temperaturas de CPU ni ventiladores.
public sealed class HardwareReader : IDisposable
{
    private static readonly HardwareType[] GpuTypes = [HardwareType.GpuNvidia, HardwareType.GpuAmd, HardwareType.GpuIntel];

    private readonly Computer computer = new()
    {
        IsCpuEnabled = true,
        IsGpuEnabled = true,
        IsMemoryEnabled = true,
        IsMotherboardEnabled = true,
        IsControllerEnabled = true,
        IsStorageEnabled = true,
    };

    private readonly MonitorSettings settings;

    public HardwareReader(MonitorSettings settings)
    {
        this.settings = settings;
        computer.Open();
    }

    public Snapshot Read()
    {
        foreach (var hardware in computer.Hardware) Update(hardware);

        var cpu = computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Cpu);
        var gpu = SensorPick.Gpu(computer.Hardware.Where(h => GpuTypes.Contains(h.HardwareType)), h => h.HardwareType);
        // «Total Memory» es la RAM. Hay otro Memory, «Virtual Memory», con los mismos sensores pero
        // contando también el archivo de paginación, y los módulos DIMM pueden ser Memory también.
        var memory = computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Memory && h.Name == "Total Memory");

        return new Snapshot(
            Memory(memory),
            cpu is null ? null : Cpu(cpu),
            gpu is null ? null : Gpu(gpu),
            Disks(),
            SensorPick.Fans(FanReadings(), settings.Fans),
            PawnIo.IsInstalled);
    }

    public void Dispose() => computer.Close();

    private static void Update(IHardware hardware)
    {
        hardware.Update();
        foreach (var sub in hardware.SubHardware) Update(sub);
    }

    private static List<Reading> Readings(IHardware hardware) =>
        hardware.Sensors.Select(s => new Reading(s.SensorType, s.Name, s.Value)).ToList();

    private static MemoryStatus Memory(IHardware? memory)
    {
        if (memory is null) return new MemoryStatus(null, null, null);
        var readings = Readings(memory);
        var used = readings.FirstOrDefault(r => r.Type == SensorType.Data && r.Name == "Memory Used").Value;
        var available = readings.FirstOrDefault(r => r.Type == SensorType.Data && r.Name == "Memory Available").Value;
        var load = readings.FirstOrDefault(r => r.Type == SensorType.Load && r.Name == "Memory").Value;
        return new MemoryStatus(load, used, used + available);
    }

    private static ChipStatus Cpu(IHardware cpu)
    {
        var readings = Readings(cpu);
        return new ChipStatus(cpu.Name, SensorPick.CpuTemperature(readings), SensorPick.CpuLoad(readings), SensorPick.CpuClock(readings));
    }

    private static ChipStatus Gpu(IHardware gpu)
    {
        var readings = Readings(gpu);
        return new ChipStatus(gpu.Name, SensorPick.GpuTemperature(readings), SensorPick.GpuLoad(readings), SensorPick.GpuClock(readings));
    }

    // Una entrada por letra de unidad: es como se piensa en un disco desde Windows.
    private List<DiskStatus> Disks()
    {
        var disks = new List<DiskStatus>();
        foreach (var device in computer.Hardware.OfType<StorageDevice>())
        {
            var temperature = SensorPick.Temperature(Readings(device));
            var letters = device.Storage.Partitions
                .Where(p => p.DriveLetter is not null)
                .Select(p => p.DriveLetter!.Value);
            foreach (var letter in letters)
            {
                var drive = new DriveInfo($"{letter}:\\");
                if (!drive.IsReady) continue;
                disks.Add(new DiskStatus(
                    $"{letter}:", device.Storage.Model.Trim(), device.Storage.IsSSD, temperature,
                    drive.TotalSize - drive.TotalFreeSpace, drive.TotalSize));
            }
        }
        return disks.OrderBy(d => d.Letter, StringComparer.Ordinal).ToList();
    }

    // Los ventiladores cuelgan del chip SuperIO de la placa (subhardware) o de controladoras
    // como las de refrigeración líquida.
    private List<Reading> FanReadings()
    {
        var readings = new List<Reading>();
        foreach (var hardware in computer.Hardware) Collect(hardware, readings);
        return readings;

        static void Collect(IHardware hardware, List<Reading> into)
        {
            into.AddRange(Readings(hardware).Where(r => r.Type == SensorType.Fan));
            foreach (var sub in hardware.SubHardware) Collect(sub, into);
        }
    }
}
