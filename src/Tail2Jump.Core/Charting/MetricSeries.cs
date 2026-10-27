using System;
using System.Collections.ObjectModel;

namespace Tail2Jump.Charting
{
  /// <summary>
  /// Fixed-length series of samples used by the performance charts.
  /// </summary>
  public class MetricSeries
  {
    private readonly ObservableCollection<double> _values = new ObservableCollection<double>();

    public MetricSeries(int capacity)
    {
      if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));

      Capacity = capacity;
    }

    public int Capacity { get; }

    public ObservableCollection<double> Values => _values;

    public double Latest => _values.Count == 0 ? 0 : _values[_values.Count - 1];

    public void Add(double value)
    {
      _values.Add(value);

      while (_values.Count > Capacity)
      {
        _values.RemoveAt(0);
      }
    }
  }
}
