using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Tail2Jump.App.Charts
{
  /// <summary>
  /// Lightweight sparkline / area chart, drawn with Avalonia primitives.
  /// </summary>
  public class SparklineChart : Control
  {
    public static readonly StyledProperty<IEnumerable<double>> ValuesProperty =
      AvaloniaProperty.Register<SparklineChart, IEnumerable<double>>(nameof(Values));

    public static readonly StyledProperty<double> MaximumProperty =
      AvaloniaProperty.Register<SparklineChart, double>(nameof(Maximum), 100.0);

    public static readonly StyledProperty<bool> AutoScaleProperty =
      AvaloniaProperty.Register<SparklineChart, bool>(nameof(AutoScale));

    public static readonly StyledProperty<IBrush> StrokeProperty =
      AvaloniaProperty.Register<SparklineChart, IBrush>(nameof(Stroke),
        new SolidColorBrush(Color.Parse("#FF9ACD32")));

    public static readonly StyledProperty<IBrush> FillProperty =
      AvaloniaProperty.Register<SparklineChart, IBrush>(nameof(Fill),
        new SolidColorBrush(Color.Parse("#339ACD32")));

    static SparklineChart()
    {
      AffectsRender<SparklineChart>(ValuesProperty, MaximumProperty, AutoScaleProperty,
        StrokeProperty, FillProperty);
    }

    public SparklineChart()
    {
      ClipToBounds = true;
    }

    public IEnumerable<double> Values
    {
      get { return GetValue(ValuesProperty); }
      set { SetValue(ValuesProperty, value); }
    }

    public double Maximum
    {
      get { return GetValue(MaximumProperty); }
      set { SetValue(MaximumProperty, value); }
    }

    public bool AutoScale
    {
      get { return GetValue(AutoScaleProperty); }
      set { SetValue(AutoScaleProperty, value); }
    }

    public IBrush Stroke
    {
      get { return GetValue(StrokeProperty); }
      set { SetValue(StrokeProperty, value); }
    }

    public IBrush Fill
    {
      get { return GetValue(FillProperty); }
      set { SetValue(FillProperty, value); }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
      base.OnPropertyChanged(change);

      if (change.Property != ValuesProperty) return;

      if (change.OldValue is INotifyCollectionChanged oldCollection)
      {
        oldCollection.CollectionChanged -= OnValuesChanged;
      }

      if (change.NewValue is INotifyCollectionChanged newCollection)
      {
        newCollection.CollectionChanged += OnValuesChanged;
      }
    }

    private void OnValuesChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
      InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
      var bounds = new Rect(Bounds.Size);
      if (bounds.Width <= 0 || bounds.Height <= 0) return;

      context.FillRectangle(new SolidColorBrush(Color.Parse("#CC141414")), bounds);

      var gridPen = new Pen(new SolidColorBrush(Color.Parse("#22FFFFFF")), 1);
      for (int i = 1; i < 4; i++)
      {
        double y = bounds.Height * i / 4.0;
        context.DrawLine(gridPen, new Point(0, y), new Point(bounds.Width, y));
      }

      var values = Values?.ToArray() ?? Array.Empty<double>();
      if (values.Length < 2) return;

      double maximum = AutoScale
        ? Math.Max(values.Max(), 1) * 1.15
        : Math.Max(Maximum, 1);

      double stepX = bounds.Width / Math.Max(values.Length - 1, 1);

      double Y(double value)
      {
        double ratio = Math.Clamp(value / maximum, 0, 1);
        return bounds.Height - ratio * bounds.Height;
      }

      var geometry = new StreamGeometry();
      using (var geometryContext = geometry.Open())
      {
        geometryContext.BeginFigure(new Point(0, bounds.Height), true);
        geometryContext.LineTo(new Point(0, Y(values[0])));

        for (int i = 1; i < values.Length; i++)
        {
          geometryContext.LineTo(new Point(i * stepX, Y(values[i])));
        }

        geometryContext.LineTo(new Point(bounds.Width, bounds.Height));
        geometryContext.EndFigure(true);
      }

      context.DrawGeometry(Fill, new Pen(Stroke, 1.5), geometry);
    }
  }
}
