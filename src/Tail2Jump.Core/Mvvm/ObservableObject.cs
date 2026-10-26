using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Be.Manuc66.Library.Mvvm
{
  /// <summary>
  /// Minimal INotifyPropertyChanged implementation.
  /// Replaces the WPF-bound Prism <c>BindableBase</c>.
  /// </summary>
  public abstract class ObservableObject : INotifyPropertyChanged
  {
    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
      var handler = PropertyChanged;
      if (handler == null) return;
      handler(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
    {
      if (EqualityComparer<T>.Default.Equals(storage, value)) return false;

      storage = value;
      OnPropertyChanged(propertyName);
      return true;
    }
  }
}
