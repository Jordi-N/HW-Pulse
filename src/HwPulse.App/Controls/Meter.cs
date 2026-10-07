using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace HwPulse.App.Controls;

// Barra de 0 a 1, horizontal (se llena hacia la derecha) o vertical (hacia arriba). Filas o columnas
// proporcionales en vez de escalar el relleno: así las esquinas no se deforman con valores bajos.
public sealed partial class Meter : Grid
{
    public static readonly DependencyProperty FractionProperty = DependencyProperty.Register(
        nameof(Fraction), typeof(double), typeof(Meter), new PropertyMetadata(0d, (d, _) => ((Meter)d).Fill()));

    private readonly ColumnDefinition filledColumn = new();
    private readonly ColumnDefinition emptyColumn = new();
    private readonly RowDefinition emptyRow = new();
    private readonly RowDefinition filledRow = new();
    private readonly Border bar = new()
    {
        CornerRadius = new CornerRadius(3),
        Background = (Brush)Application.Current.Resources["PulseData"],
    };

    private Orientation orientation = Orientation.Horizontal;

    public Meter()
    {
        Height = 6;
        CornerRadius = new CornerRadius(3);
        Background = (Brush)Application.Current.Resources["PulseTrack"];
        Children.Add(bar);
        Orient();
    }

    public double Fraction
    {
        get => (double)GetValue(FractionProperty);
        set => SetValue(FractionProperty, value);
    }

    public Orientation Orientation
    {
        get => orientation;
        set
        {
            orientation = value;
            Orient();
        }
    }

    private void Orient()
    {
        ColumnDefinitions.Clear();
        RowDefinitions.Clear();
        if (orientation == Orientation.Horizontal)
        {
            ColumnDefinitions.Add(filledColumn);
            ColumnDefinitions.Add(emptyColumn);
            SetRow(bar, 0);
        }
        else
        {
            RowDefinitions.Add(emptyRow);
            RowDefinitions.Add(filledRow);
            SetRow(bar, 1);
        }
        Fill();
    }

    private void Fill()
    {
        var fraction = Math.Clamp(Fraction, 0, 1);
        filledColumn.Width = filledRow.Height = new GridLength(fraction, GridUnitType.Star);
        emptyColumn.Width = emptyRow.Height = new GridLength(1 - fraction, GridUnitType.Star);
    }
}
