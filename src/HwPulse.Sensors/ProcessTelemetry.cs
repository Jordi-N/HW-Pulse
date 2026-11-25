using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace HwPulse.Sensors;

public sealed record ProcessSample(int Pid, DateTimeOffset? StartedAt, string? Name, double? CpuMs,
    long? WorkingSetBytes, ulong? ReadBytes, ulong? WrittenBytes);

public static partial class ProcessTelemetry
{
    public static IReadOnlyList<ProcessSample> Read()
    {
        var processes = Process.GetProcesses();
        var samples = new List<ProcessSample>(processes.Length);
        foreach (var process in processes)
        {
            using (process)
            {
                var io = ReadCounter(() => GetProcessIoCounters(process.SafeHandle, out var counters)
                    ? counters : (IoCounters?)null);
                samples.Add(new ProcessSample(process.Id,
                    ReadCounter(() => (DateTimeOffset?)process.StartTime.ToUniversalTime()),
                    ReadCounter(() => process.ProcessName),
                    ReadCounter(() => (double?)process.TotalProcessorTime.TotalMilliseconds),
                    ReadCounter(() => (long?)process.WorkingSet64),
                    io?.ReadBytes, io?.WrittenBytes));
            }
        }
        return samples;
    }

    internal static T? ReadCounter<T>(Func<T> read)
    {
        try { return read(); }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or NotSupportedException)
        {
            return default;
        }
    }

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetProcessIoCounters(Microsoft.Win32.SafeHandles.SafeProcessHandle processHandle, out IoCounters counters);

    [StructLayout(LayoutKind.Sequential)]
    private struct IoCounters
    {
        public ulong ReadOperations;
        public ulong WriteOperations;
        public ulong OtherOperations;
        public ulong ReadBytes;
        public ulong WrittenBytes;
        public ulong OtherBytes;
    }
}
