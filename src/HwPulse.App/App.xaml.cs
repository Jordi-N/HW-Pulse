using System.Runtime.InteropServices;
using System.Windows.Input;
using H.NotifyIcon;
using H.NotifyIcon.Core;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace HwPulse.App;

// La aplicación vive en la bandeja: Esc oculta el panel, el icono lo vuelve a mostrar y solo
// termina con «Salir» del menú del icono.
public partial class App : Application, IDisposable
{
    private const int ForceDarkAppMode = 2;

    private MainWindow? window;
    private TaskbarIcon? tray;
    private DispatcherQueue? dispatcher;

    public App()
    {
        InitializeComponent();
        // WinUI termina la aplicación al cerrarse su última ventana; aquí la ventana se oculta y la
        // aplicación sigue en la bandeja.
        DispatcherShutdownMode = DispatcherShutdownMode.OnExplicitShutdown;
    }

    // Otra ejecución (el acceso del menú Inicio) con este panel ya en marcha. Llega desde fuera del
    // hilo de la interfaz.
    public void ShowPanelFromAnotherLaunch() => dispatcher?.TryEnqueue(ShowPanel);

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        dispatcher = DispatcherQueue.GetForCurrentThread();
        UseDarkSystemMenus();

        tray = new TaskbarIcon
        {
            ToolTipText = "HW Pulse",
            NoLeftClickDelay = true,
            LeftClickCommand = new ActionCommand(ShowPanel),
            RightClickCommand = new ActionCommand(ShowMenu),
            Icon = new System.Drawing.Icon(Path.Combine(AppContext.BaseDirectory, "HwPulse.ico")),
        };
        tray.ForceCreate();

        window = new MainWindow();
        window.Activate();
    }

    private void ShowPanel() => window?.ShowPanel();

    // El menú nativo de Windows y no un MenuFlyout de XAML: sale junto al cursor, se voltea en el
    // borde de la pantalla y no necesita una ventana visible, que con el panel oculto no la hay.
    // Se arma en cada apertura para que «Iniciar con Windows» refleje el estado del momento.
    private void ShowMenu()
    {
        if (tray?.TrayIcon is null || !GetCursorPos(out var cursor)) return;

        var startsWithWindows = StartupTask.IsEnabled;
        var menu = new PopupMenu();
        menu.Items.Add(new PopupMenuItem("Mostrar panel", (_, _) => ShowPanel()));
        menu.Items.Add(new PopupMenuSeparator());
        menu.Items.Add(new PopupMenuItem("Iniciar con Windows", (_, _) => StartupTask.Toggle())
        {
            Checked = startsWithWindows == true,
            Enabled = startsWithWindows is not null,
        });
        menu.Items.Add(new PopupMenuSeparator());
        menu.Items.Add(new PopupMenuItem("Salir", (_, _) => Quit()));
        menu.Show(tray.TrayIcon.WindowHandle, cursor.X, cursor.Y);
    }

    public void Dispose()
    {
        tray?.Dispose();
        tray = null;
        GC.SuppressFinalize(this);
    }

    private void Quit()
    {
        window?.Shutdown();
        Dispose();
        Exit();
    }

    // El menú del icono es Win32 y sale en claro aunque la aplicación vaya en oscuro. Las dos
    // funciones de uxtheme solo se exportan por ordinal, desde Windows 10 1809 (la versión mínima
    // de la aplicación); es la vía que usa el propio Explorador.
    private static void UseDarkSystemMenus()
    {
        _ = SetPreferredAppMode(ForceDarkAppMode);
        FlushMenuThemes();
    }

    [LibraryImport("uxtheme.dll", EntryPoint = "#135")]
    private static partial int SetPreferredAppMode(int mode);

    [LibraryImport("uxtheme.dll", EntryPoint = "#136")]
    private static partial void FlushMenuThemes();

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetCursorPos(out CursorPoint point);

    [StructLayout(LayoutKind.Sequential)]
    private struct CursorPoint
    {
        public int X;
        public int Y;
    }

    private sealed class ActionCommand(Action run) : ICommand
    {
        public event EventHandler? CanExecuteChanged { add { } remove { } }

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter) => run();
    }
}
