namespace HwPulse.Sensors;

// Los últimos N valores de una magnitud, del más antiguo al más reciente, para las gráficas.
public sealed class History(int capacity)
{
    private readonly Queue<float> values = new(capacity);

    public IReadOnlyCollection<float> Values => values;

    public void Add(float? value)
    {
        if (value is null) return;
        if (values.Count == capacity) values.Dequeue();
        values.Enqueue(value.Value);
    }
}
