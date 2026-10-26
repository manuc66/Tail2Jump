using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace Tail2Jump.Charting
{
  /// <summary>
  /// Fixed-length series of samples used by the performance charts.
  /// </summary>
  public class MetricSeries
  {
    private readonly ObservableCollection<double> _values = new ObservableCollection<double>();

    public MetricSeries(int capacity, bool autoScale = false, double maximum = 100)
    {
      if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));

      Capacity = capacity;
      AutoScale = autoScale;
      FixedMaximum = maximum;
      Maximum = maximum;
    }

    public int Capacity { get; }

    public bool AutoScale { get; }

    public double FixedMaximum { get; }

    public ObservableCollection<double> Values => _values;

    public double Maximum { get; private set; }

    public double Latest => _values.Count == 0 ? 0 : _values[_values.Count - 1];

    public void Add(double value)
    {
      _values.Add(value);

      while (_values.Count > Capacity)
      {
        _values.RemoveAt(0);
      }

      if (AutoScale)
      {
        Maximum = Math.Max(_values.Max() * 1.15, 1);
      }
    }

    public void Clear()
    {
      _values.Clear();
      Maximum = AutoScale ? 1 : FixedMaximum;
    }
  }
}
