using System.Diagnostics;
using System.ServiceProcess;
using Microsoft.Win32;

namespace HwPulse.Sensors;

// Estado de los servicios vigilados, leído de Windows en cada vuelta: así aparece un runner recién
// instalado o un servicio que se ha caído sin reiniciar el panel.
internal sealed class ServiceReader(IReadOnlyList<ServiceSetting> configured)
{
    public IReadOnlyList<ServiceStatus> Read()
    {
        var services = ServiceController.GetServices();
        try
        {
            var byName = services.ToDictionary(s => s.ServiceName, StringComparer.OrdinalIgnoreCase);
            var watched = configured.Count > 0 ? configured : ServicePick.Defaults(byName.Keys);
            var workers = watched.Any(w => ServicePick.IsRunner(w.Name)) ? RunnerWorkerPaths() : [];
            return watched.Select(w => Status(w, byName.GetValueOrDefault(w.Name), workers)).ToList();
        }
        finally
        {
            foreach (var service in services) service.Dispose();
        }
    }

    private static ServiceStatus Status(ServiceSetting watched, ServiceController? service, IReadOnlyList<string> workers)
    {
        var running = service?.Status == ServiceControllerStatus.Running;
        var busy = running && ServicePick.IsRunner(watched.Name) && ServicePick.IsRunnerBusy(ImagePath(watched.Name), workers);
        return new ServiceStatus(watched.Label, ServicePick.State(service is not null, running, busy));
    }

    private static string? ImagePath(string serviceName)
    {
        using var key = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Services\{serviceName}");
        return key?.GetValue("ImagePath") as string;
    }

    private static List<string> RunnerWorkerPaths()
    {
        var workers = Process.GetProcessesByName("Runner.Worker");
        try
        {
            return workers.Select(p => p.MainModule?.FileName).OfType<string>().ToList();
        }
        finally
        {
            foreach (var worker in workers) worker.Dispose();
        }
    }
}
