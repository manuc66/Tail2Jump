using System;
using System.Threading;
using System.Windows.Threading;

namespace Be.Manuc66.Library
{
  /// <summary>
  /// WPF Threading extension methods
  /// </summary>
  public static class WpfControlThreadingExtensions
  {
    #region Public Methods
    /// <summary>
    /// A simple WPF threading extension method, to invoke a delegate
    /// on the correct thread if it is not currently on the correct thread
    /// Which can be used with DispatcherObject types
    /// </summary>
    /// <param name="disp_">The Dispatcher object on which to do the Invoke</param>
    /// <param name="dotIt_">The delegate to run</param>
    /// <param name="priority_">The DispatcherPriority</param>
    public static void InvokeIfRequired(this Dispatcher disp_, Action dotIt_, DispatcherPriority priority_)
    {
      if (dotIt_ == null || disp_ == null)
      {
        return;
      }
      if (disp_.Thread != Thread.CurrentThread)
      {
        disp_.Invoke(priority_, dotIt_);
      }
      else
        dotIt_();
    }
    #endregion

  }
}