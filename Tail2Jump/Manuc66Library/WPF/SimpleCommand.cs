using System;
using log4net;

namespace Be.Manuc66.Library.WPF
{
  /// <summary>
  /// Implements the ICommand and wraps up all the verbose stuff so that you can just pass 2 delegates 1 for the CanExecute and one for the Execute
  /// </summary>
  public class SimpleCommand<T> : ICommand<T>
  {
    /// <summary>
    /// Gets or sets the Predicate to execute when the CanExecute of the command gets called
    /// </summary>
    public Predicate<T> CanExecuteDelegate { get; set; }

    /// <summary>
    /// Gets or sets the action to be called when the Execute method of the command gets called
    /// </summary>
    public Action<T> ExecuteDelegate { get; set; }

    public SimpleCommand(Predicate<T> canExecuteDelegate, Action<T> executeDelegate)
    {
      CanExecuteDelegate = canExecuteDelegate;
      ExecuteDelegate = executeDelegate;
    }

    private static readonly ILog Log = LogManager.GetLogger(typeof(SimpleCommand<T>));

    /// <summary>
    /// Checks if the command Execute method can run
    /// </summary>
    /// <param name="parameter">THe command parameter to be passed</param>
    /// <returns>Returns true if the command can execute. By default true is returned so that if the user of SimpleCommand does not specify a CanExecuteCommand delegate the command still executes.</returns>
    public bool CanExecute(object parameter)
    {
      if (CanExecuteDelegate != null)
      {
        if (parameter is T)
        {
          return (this as ICommand<T>).CanExecute((T)parameter);
        }
        if (!ReferenceEquals(default(T), parameter))
        {
          Log.Error("Bad parameter type for method " + CanExecuteDelegate.Method.Name + ", expected " + typeof(T) + " got: " + (parameter == null ? "(null)" : parameter.GetType().FullName));
        }
        return (this as ICommand<T>).CanExecute(default(T));
      }
      return true;// if there is no can execute default to true
    }


    bool ICommand<T>.CanExecute(T parameter)
    {
      return CanExecuteDelegate(parameter);
    }

    public event EventHandler CanExecuteChanged;

    /// <summary>
    /// Executes the actual command
    /// </summary>
    /// <param name="parameter">THe command parameter to be passed</param>
    public void Execute(object parameter)
    {
      if (ExecuteDelegate != null)
      {
        if (parameter is T)
        {
          (this as ICommand<T>).Execute((T)parameter);
        }
        else
        {
          if (!ReferenceEquals(default(T), parameter))
          {
            Log.Error("Bad parameter type for method " + ExecuteDelegate.Method.Name + ", expected " + typeof(T) + " got: " + (parameter == null ? "(null)" : parameter.GetType().FullName));
          }
          (this as ICommand<T>).Execute(default(T));
        }
      }
    }

    void ICommand<T>.Execute(T parameter)
    {
      ExecuteDelegate(parameter);
    }

    public void RaiseCanExecuteChanged()
    {
      if (CanExecuteChanged != null)
      {
        CanExecuteChanged(this, EventArgs.Empty);
      }
    }
  }
}
