using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;

namespace HwPulse.App;

// Punto de entrada propio para que haya un solo panel. Si ya hay uno en marcha, quizá oculto en la
// bandeja, esta ejecución le pide que se muestre y termina: es lo que pasa al abrirlo desde el menú
// Inicio. El Main que genera WinUI no da hueco para decidirlo antes de arrancar la interfaz.
internal static class Program
{
    [STAThread]
    private static void Main()
    {
        WinRT.ComWrappersSupport.InitializeComWrappers();

        var main = AppInstance.FindOrRegisterForKey("HwPulse");
        if (!main.IsCurrent)
        {
            // Fuera de este hilo STA: esperar aquí a la redirección lo dejaría bloqueado.
            var activation = AppInstance.GetCurrent().GetActivatedEventArgs();
            Task.Run(() => main.RedirectActivationToAsync(activation).AsTask()).Wait();
            return;
        }

        Application.Start(_ =>
        {
            SynchronizationContext.SetSynchronizationContext(new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread()));
            var app = new App();
            main.Activated += (_, _) => app.ShowPanelFromAnotherLaunch();
        });
    }
}
