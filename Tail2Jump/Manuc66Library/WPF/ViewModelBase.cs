using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Be.Manuc66.Library.WPF
{
  // taken from http://www.e-pedro.com/2009/07/a-view-model-base-class-to-use-with-mvvm-in-wpf/
  public class ViewModelBase : INotifyPropertyChanged
  {
    private readonly Dictionary<string, PropertyChangedEventArgs> _eventArgsCache;

    protected ViewModelBase()
    {
      _eventArgsCache = new Dictionary<string, PropertyChangedEventArgs>();
    }

    #region Implementation of INotifyPropertyChanged

    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
      PropertyChangedEventArgs args;
      if (!_eventArgsCache.TryGetValue(propertyName, out args))
      {
        args = new PropertyChangedEventArgs(propertyName);
        _eventArgsCache.Add(propertyName, args);
      }

      OnPropertyChanged(args);
    }

    protected void OnPropertyChanged(PropertyChangedEventArgs args_)
    {
      var handler = PropertyChanged;
      if (handler != null)
        handler(this, args_);
    }
    #endregion
  }
}
