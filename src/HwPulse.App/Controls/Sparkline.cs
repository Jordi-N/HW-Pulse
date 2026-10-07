using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;

namespace HwPulse.App.Controls;

// Gráfica de área de 0 a 100 sobre una rejilla, con los últimos Capacity valores. Se llena de
// derecha a izquierda, como el SensorPanel: el valor actual siempre está en el borde derecho.
public sealed partial class Sparkline : Grid
{
    private const int Rows = 5;
    private const int Columns = 10;

    private readonly Canvas grid = new();
    private readonly Polygon area = new() { Fill = (Brush)Application.Current.Resources["PulseDataArea"] };
    private readonly Polyline line = new()
    {
        Stroke = (Brush)Application.Current.Resources["PulseData"],
        StrokeThickness = 2,
        StrokeLineJoin = PenLineJoin.Round,
    };

    private IReadOnlyCollection<float> values = [];

    public Sparkline()
    {
        Background = (Brush)Application.Current.Resources["PulseGraph"];
        Children.Add(grid);
        Children.Add(area);
        Children.Add(line);
        Children.Add(AxisLabel("100", VerticalAlignment.Top));
        Children.Add(AxisLabel("0", VerticalAlignment.Bottom));
        SizeChanged += (_, _) =>
        {
            DrawGrid();
            Draw();
        };
    }

    public int Capacity { get; set; } = 60;

    public void Show(IReadOnlyCollection<float> newValues)
    {
        values = newValues;
        Draw();
    }

    private static TextBlock AxisLabel(string text, VerticalAlignment alignment) => new()
    {
        Text = text,
        Margin = new Thickness(3, 1, 0, 1),
        FontSize = 10,
        FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
        HorizontalAlignment = HorizontalAlignment.Left,
        VerticalAlignment = alignment,
        Foreground = (Brush)Application.Current.Resources["PulseInk"],
    };

    private void DrawGrid()
    {
        var stroke = (Brush)Application.Current.Resources["PulseTrack"];
        grid.Children.Clear();
        for (var row = 0; row <= Rows; row++)
        {
            var y = ActualHeight * row / Rows;
            grid.Children.Add(new Line { X2 = ActualWidth, Y1 = y, Y2 = y, Stroke = stroke, StrokeThickness = 1 });
        }
        for (var column = 0; column <= Columns; column++)
        {
            var x = ActualWidth * column / Columns;
            grid.Children.Add(new Line { X1 = x, X2 = x, Y2 = ActualHeight, Stroke = stroke, StrokeThickness = 1 });
        }
    }

    private void Draw()
    {
        var width = ActualWidth;
        var height = ActualHeight;
        var step = width / Math.Max(Capacity - 1, 1);
        var x = width - (values.Count - 1) * step;

        var points = new PointCollection();
        var filled = new PointCollection();
        foreach (var value in values)
        {
            var point = new Point(x, height - Math.Clamp(value, 0, 100) / 100 * height);
            points.Add(point);
            filled.Add(point);
            x += step;
        }
        if (points.Count > 0)
        {
            filled.Add(new Point(width, height));
            filled.Add(new Point(points[0].X, height));
        }
        line.Points = points;
        area.Points = filled;
    }
}
