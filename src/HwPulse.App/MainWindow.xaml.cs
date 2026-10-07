using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text.Json;
using HwPulse.Sensors;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.Graphics;

namespace HwPulse.App;

public sealed partial class MainWindow : Window
{
    private const int HistoryLength = 60;

    // Pantalla y sistema despiertos mientras el panel se vea: es un panel que se mira, no se toca.
    private const uint EsContinuous = 0x80000000;
    private const uint EsSystemRequired = 0x00000001;
    private const uint EsDisplayRequired = 0x00000002;

    private static readonly string SettingsPath = Path.Combine(AppContext.BaseDirectory, "settings.json");

    private readonly History ramHistory = new(HistoryLength);
    private readonly History cpuHistory = new(HistoryLength);
    private readonly Task readLoop;

    // Lo levanta «Salir»: el bucle de lectura termina en su siguiente vuelta y la ventana deja de
    // ocultarse en vez de cerrarse.
    private volatile bool exiting;

    // Ningún sensor da el reloj máximo: las barras de reloj se miden contra el mayor visto.
    private float cpuClockMax;
    private float gpuClockMax;

    public MainWindow()
    {
        InitializeComponent();
        RamGraph.Capacity = HistoryLength;
        CpuGraph.Capacity = HistoryLength;
        AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "HwPulse.ico"));
        PlaceOnChosenDisplay();
        KeepAwake(true);

        // Alt+F4 oculta igual que Esc: solo se sale desde el icono de la bandeja.
        AppWindow.Closing += (_, e) =>
        {
            if (exiting) return;
            e.Cancel = true;
            HidePanel();
        };

        readLoop = Task.Run(ReadLoopAsync);
    }

    public void ShowPanel()
    {
        AppWindow.Show();
        AppWindow.SetPresenter(AppWindowPresenterKind.FullScreen);
        Activate();
        KeepAwake(true);
    }

    public void Shutdown()
    {
        exiting = true;
        readLoop.Wait();
        Close();
    }

    private void HidePanel()
    {
        AppWindow.Hide();
        KeepAwake(false);
    }

    private static void KeepAwake(bool on)
    {
        var flags = on ? EsContinuous | EsSystemRequired | EsDisplayRequired : EsContinuous;
        if (SetThreadExecutionState(flags) == 0) throw new Win32Exception();
    }

    [LibraryImport("kernel32.dll")]
    private static partial uint SetThreadExecutionState(uint flags);

    private void PlaceOnChosenDisplay()
    {
        var displays = Displays();
        var primary = DisplayArea.Primary.DisplayId.Value;
        var index = DisplayPick.Choose(
            displays.Select(d => Corner(d.OuterBounds)).ToList(),
            displays.FindIndex(d => d.DisplayId.Value == primary),
            SavedDisplay());
        MoveTo(displays[index]);
    }

    // Con settings.json mal escrito se usa la pantalla por defecto: el error ya lo enseña el bucle
    // de lectura, que carga el mismo fichero.
    private static DisplayCorner? SavedDisplay()
    {
        try
        {
            return MonitorSettings.Load(SettingsPath).Display;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException)
        {
            return null;
        }
    }

    // Ocupa entera la pantalla destino antes de pasar a pantalla completa: con solo mover la
    // esquina, la ventana conserva su tamaño y puede quedarse en la pantalla que más tape.
    private void MoveTo(DisplayArea display)
    {
        AppWindow.SetPresenter(AppWindowPresenterKind.Overlapped);
        AppWindow.MoveAndResize(display.OuterBounds);
        AppWindow.SetPresenter(AppWindowPresenterKind.FullScreen);
    }

    // De izquierda a derecha, para que «Pantalla 1» sea la de más a la izquierda. Recorrido por
    // índice: el enumerador de DisplayArea.FindAll falla en las aplicaciones sin empaquetar.
    private static List<DisplayArea> Displays()
    {
        var all = DisplayArea.FindAll();
        var displays = new List<DisplayArea>(all.Count);
        for (var i = 0; i < all.Count; i++) displays.Add(all[i]);
        return [.. displays.OrderBy(d => d.OuterBounds.X).ThenBy(d => d.OuterBounds.Y)];
    }

    private static DisplayCorner Corner(RectInt32 bounds) => new(bounds.X, bounds.Y);

    // Clic derecho en el panel: elegir la pantalla, que se recuerda para los siguientes arranques.
    private void OnRightTapped(object sender, RightTappedRoutedEventArgs args)
    {
        var primary = DisplayArea.Primary.DisplayId.Value;
        var current = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Nearest).DisplayId.Value;
        var menu = new MenuFlyout();
        menu.Items.Add(new MenuFlyoutItem { Text = "PANTALLA", Style = (Style)Application.Current.Resources["PulseMenuTitle"] });
        menu.Items.Add(new MenuFlyoutSeparator());
        var number = 0;
        foreach (var display in Displays())
        {
            var bounds = display.OuterBounds;
            var label = $"Pantalla {++number} · {bounds.Width}×{bounds.Height}";
            var item = new RadioMenuFlyoutItem
            {
                Text = display.DisplayId.Value == primary ? label + " · principal" : label,
                IsChecked = display.DisplayId.Value == current,
            };
            item.Click += (_, _) =>
            {
                MoveTo(display);
                SaveDisplay(Corner(bounds));
            };
            menu.Items.Add(item);
        }

        var target = (UIElement)sender;
        menu.ShowAt(target, args.GetPosition(target));
    }

    private void SaveDisplay(DisplayCorner corner)
    {
        try
        {
            MonitorSettings.SaveDisplay(SettingsPath, corner);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException)
        {
            ShowNotice(InfoBarSeverity.Error, "No se puede guardar la pantalla elegida", e.Message);
        }
    }

    private void OnEscape(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args) => HidePanel();

    // En segundo plano: abrir LibreHardwareMonitor tarda segundos y cada lectura, decenas de ms.
    // Un fallo (settings.json mal escrito, por ejemplo) se enseña en pantalla en vez de perderse.
    private async Task ReadLoopAsync()
    {
        try
        {
            using var reader = new HardwareReader(MonitorSettings.Load(SettingsPath));
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
            do
            {
                var snapshot = reader.Read();
                DispatcherQueue.TryEnqueue(() => Show(snapshot));
            }
            while (!exiting && await timer.WaitForNextTickAsync().ConfigureAwait(false));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException)
        {
            DispatcherQueue.TryEnqueue(() => ShowNotice(InfoBarSeverity.Error, "No se pueden leer los sensores", e.Message));
        }
    }

    private void Show(Snapshot snapshot)
    {
        ramHistory.Add(snapshot.Memory.LoadPercent);
        RamGraph.Show(ramHistory.Values);
        RamPercent.Text = Format.Percent(snapshot.Memory.LoadPercent);

        ShowCpu(snapshot.Cpu);
        ShowGpu(snapshot.Gpu);
        Disks.ItemsSource = snapshot.Disks.Select(d => DiskRow.From(d, snapshot.Disks.Count)).ToList();
        Fans.ItemsSource = snapshot.Fans.Select(FanRow.From).ToList();
        NetworkAdapter.Text = snapshot.Network?.Adapter ?? "—";
        NetworkDown.Text = Format.Bitrate(snapshot.Network?.DownloadBytesPerSecond);
        NetworkUp.Text = Format.Bitrate(snapshot.Network?.UploadBytesPerSecond);
        Services.ItemsSource = snapshot.Services.Select(ServiceRow.From).ToList();
        NoServices.Visibility = snapshot.Services.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        if (snapshot.HasLowLevelAccess) Notice.IsOpen = false;
        else ShowNotice(InfoBarSeverity.Warning, "Falta el driver PawnIO", "Sin él no hay temperatura de CPU ni ventiladores. Reinstala HW Pulse con su instalador, que lo incluye.");
    }

    private void ShowCpu(ChipStatus? cpu)
    {
        cpuHistory.Add(cpu?.LoadPercent);
        CpuGraph.Show(cpuHistory.Values);
        cpuClockMax = Math.Max(cpuClockMax, cpu?.ClockMhz ?? 0);

        CpuName.Text = Format.ChipName(cpu?.Name);
        CpuRing.Celsius = cpu?.TemperatureC ?? double.NaN;
        CpuLoad.Text = Format.Percent(cpu?.LoadPercent);
        CpuClock.Text = Format.Mhz(cpu?.ClockMhz);
        CpuClockMeter.Fraction = Format.Fraction(cpu?.ClockMhz, cpuClockMax);
    }

    private void ShowGpu(ChipStatus? gpu)
    {
        gpuClockMax = Math.Max(gpuClockMax, gpu?.ClockMhz ?? 0);

        GpuName.Text = Format.ChipName(gpu?.Name);
        GpuRing.Celsius = gpu?.TemperatureC ?? double.NaN;
        GpuLoad.Text = Format.Percent(gpu?.LoadPercent);
        GpuLoadMeter.Fraction = Format.Fraction(gpu?.LoadPercent, 100);
        GpuClock.Text = Format.Mhz(gpu?.ClockMhz);
        GpuClockMeter.Fraction = Format.Fraction(gpu?.ClockMhz, gpuClockMax);
    }

    private void ShowNotice(InfoBarSeverity severity, string title, string message)
    {
        Notice.Severity = severity;
        Notice.Title = title;
        Notice.Message = message;
        Notice.IsOpen = true;
    }
}
