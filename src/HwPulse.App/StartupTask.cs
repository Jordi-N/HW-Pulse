using System.Runtime.InteropServices;

namespace HwPulse.App;

// «Iniciar con Windows»: el disparador de inicio de sesión de la tarea que crea el instalador. Se
// activa o desactiva el disparador y no la tarea entera, porque el acceso del menú Inicio también
// arranca el panel lanzando la tarea (así no salta UAC), y una tarea desactivada no se puede lanzar.
//
// El Programador de tareas solo se expone por COM («Schedule.Service»); dynamic evita traer una
// librería entera para leer y cambiar un booleano.
internal static class StartupTask
{
    private const string TaskName = "HW Pulse";
    private const int CreateOrUpdate = 6;
    private const int InteractiveToken = 3;
    private const int FileNotFound = unchecked((int)0x80070002);

    // null cuando la tarea no existe: el panel se está ejecutando sin instalar.
    public static bool? IsEnabled
    {
        get
        {
            var task = Find();
            return task is null ? null : (bool)task.Definition.Triggers.Item(1).Enabled;
        }
    }

    public static void Toggle()
    {
        var folder = RootFolder();
        var task = Find();
        if (task is null) return;

        var definition = task.Definition;
        var trigger = definition.Triggers.Item(1);
        trigger.Enabled = !(bool)trigger.Enabled;
        folder.RegisterTaskDefinition(TaskName, definition, CreateOrUpdate, definition.Principal.UserId, null, InteractiveToken);
    }

    private static dynamic? Find()
    {
        try
        {
            return RootFolder().GetTask(TaskName);
        }
        catch (COMException e) when (e.HResult == FileNotFound)
        {
            return null;
        }
    }

    private static dynamic RootFolder()
    {
        dynamic service = Activator.CreateInstance(Type.GetTypeFromProgID("Schedule.Service", throwOnError: true)!)!;
        service.Connect();
        return service.GetFolder("\\");
    }
}
