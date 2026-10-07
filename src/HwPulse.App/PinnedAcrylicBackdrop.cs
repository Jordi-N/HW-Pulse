using Microsoft.UI.Composition;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace HwPulse.App;

// El DesktopAcrylicBackdrop de serie pasa a color sólido cuando la ventana pierde el foco, y en la
// pantalla auxiliar no lo tiene casi nunca. Con el controlador directo se le dice que siempre está
// activa y el acrílico se mantiene.
internal sealed partial class PinnedAcrylicBackdrop : SystemBackdrop, IDisposable
{
    private DesktopAcrylicController? controller;

    protected override void OnTargetConnected(ICompositionSupportsSystemBackdrop connectedTarget, XamlRoot xamlRoot)
    {
        base.OnTargetConnected(connectedTarget, xamlRoot);
        controller = new DesktopAcrylicController();
        controller.AddSystemBackdropTarget(connectedTarget);
        controller.SetSystemBackdropConfiguration(new SystemBackdropConfiguration
        {
            IsInputActive = true,
            Theme = SystemBackdropTheme.Dark,
        });
    }

    protected override void OnTargetDisconnected(ICompositionSupportsSystemBackdrop disconnectedTarget)
    {
        base.OnTargetDisconnected(disconnectedTarget);
        controller?.RemoveSystemBackdropTarget(disconnectedTarget);
        Dispose();
    }

    public void Dispose()
    {
        controller?.Dispose();
        controller = null;
    }
}
