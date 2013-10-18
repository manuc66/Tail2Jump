using System;
using System.Globalization;
using System.Windows.Data;

namespace Be.Manuc66.Library
{
  public class EmptyConverter : IValueConverter
  {
    public object Convert(object value_, Type targetType_, object parameter_, CultureInfo culture_)
    {
      return value_;
    }

    public object ConvertBack(object value_, Type targetType_, object parameter_, CultureInfo culture_)
    {
      return value_;
    }
  }
}
