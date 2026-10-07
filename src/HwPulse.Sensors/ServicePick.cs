namespace HwPulse.Sensors;

// Qué servicios de Windows se vigilan y en qué estado están. Sin configuración se muestran los que
// tienen sentido en un servidor y estén instalados: IIS, Jellyfin y cada runner de GitHub Actions.
internal static class ServicePick
{
    private const string RunnerPrefix = "actions.runner.";

    public static IReadOnlyList<ServiceSetting> Defaults(IEnumerable<string> installed) =>
        installed
            .Select(Default)
            .OfType<ServiceSetting>()
            .OrderBy(s => s.Label, StringComparer.OrdinalIgnoreCase)
            .ToList();

    public static bool IsRunner(string serviceName) =>
        serviceName.StartsWith(RunnerPrefix, StringComparison.OrdinalIgnoreCase);

    // Un runner ocupado tiene un Runner.Worker vivo dentro de su carpeta. El ImagePath del servicio
    // apunta a <carpeta>\bin\RunnerService.exe, entre comillas si la ruta tiene espacios.
    public static bool IsRunnerBusy(string? imagePath, IEnumerable<string> workerPaths)
    {
        if (string.IsNullOrWhiteSpace(imagePath)) return false;
        var bin = Path.GetDirectoryName(imagePath.Trim().Trim('"'));
        var root = Path.GetDirectoryName(bin);
        if (root is null) return false;
        var prefix = root.TrimEnd('\\') + '\\';
        return workerPaths.Any(p => p.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }

    public static ServiceState State(bool installed, bool running, bool busy) =>
        !installed ? ServiceState.Missing
        : !running ? ServiceState.Stopped
        : busy ? ServiceState.Busy
        : ServiceState.Running;

    // actions.runner.<propietario>-<repo>.<runner>: lo que distingue a cada uno es el último tramo.
    private static ServiceSetting? Default(string name) => name switch
    {
        _ when name.Equals("W3SVC", StringComparison.OrdinalIgnoreCase) => new ServiceSetting(name, "IIS"),
        _ when name.Equals("JellyfinServer", StringComparison.OrdinalIgnoreCase) => new ServiceSetting(name, "Jellyfin"),
        _ when IsRunner(name) => new ServiceSetting(name, name[(name.LastIndexOf('.') + 1)..]),
        _ => null,
    };
}
