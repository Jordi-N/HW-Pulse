namespace HwPulse.Sensors;

// En qué pantalla va el panel. La elegida con el clic derecho se recuerda por su esquina superior
// izquierda, que no cambia entre reinicios mientras no se recoloquen las pantallas en Windows.
public static class DisplayPick
{
    // La recordada si sigue conectada; si no, la primera que no sea la principal; si solo hay una,
    // la principal.
    public static int Choose(IReadOnlyList<DisplayCorner> corners, int primary, DisplayCorner? saved)
    {
        if (saved is { } corner)
        {
            for (var i = 0; i < corners.Count; i++)
            {
                if (corners[i] == corner) return i;
            }
        }

        for (var i = 0; i < corners.Count; i++)
        {
            if (i != primary) return i;
        }

        return primary;
    }
}

public readonly record struct DisplayCorner(int X, int Y);
