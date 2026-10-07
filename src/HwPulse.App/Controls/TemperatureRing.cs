using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace HwPulse.App.Controls;

// Anillo de 0 a 100 °C con la cifra grande en el centro y «°C» debajo. NaN es «sin sensor». El
// texto se escala con el diámetro, así sirve igual para la CPU que para un disco.
public sealed partial class TemperatureRing : UserControl
{
    public static readonly DependencyProperty CelsiusProperty = DependencyProperty.Register(
        nameof(Celsius), typeof(double), typeof(TemperatureRing), new PropertyMetadata(double.NaN, (d, _) => ((TemperatureRing)d).Show()));

    private readonly ProgressRing ring = new()
    {
        IsIndeterminate = false,
        Minimum = 0,
        Maximum = 100,
        Foreground = (Brush)Application.Current.Resources["PulseData"],
        Background = (Brush)Application.Current.Resources["PulseTrack"],
    };

    private readonly TextBlock number = new()
    {
        HorizontalAlignment = HorizontalAlignment.Center,
        FontWeight = FontWeights.Bold,
        Foreground = (Brush)Application.Current.Resources["PulseInk"],
    };

    private readonly TextBlock unit = new()
    {
        Text = "°C",
        HorizontalAlignment = HorizontalAlignment.Center,
        Foreground = (Brush)Application.Current.Resources["PulseInk"],
    };

    public TemperatureRing()
    {
        var label = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Children = { number, unit } };
        Content = new Grid { Children = { ring, label } };
        SizeChanged += (_, e) => Resize(Math.Min(e.NewSize.Width, e.NewSize.Height));
        Show();
    }

    public double Celsius
    {
        get => (double)GetValue(CelsiusProperty);
        set => SetValue(CelsiusProperty, value);
    }

    private void Resize(double diameter)
    {
        ring.Width = ring.Height = diameter;
        number.FontSize = diameter * 0.3;
        number.LineHeight = number.FontSize;
        unit.FontSize = Math.Max(diameter * 0.1, 9);
    }

    private void Show()
    {
        ring.Value = double.IsNaN(Celsius) ? 0 : Celsius;
        number.Text = Format.Celsius(Celsius);
    }
}
