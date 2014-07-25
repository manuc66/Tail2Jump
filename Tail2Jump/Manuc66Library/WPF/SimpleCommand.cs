using System;
using log4net;

namespace Be.Manuc66.Library
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

    public SimpleCommand(Predicate<T> canExecuteDelegate_, Action<T> executeDelegate_)
    {
      CanExecuteDelegate = canExecuteDelegate_;
      ExecuteDelegate = executeDelegate_;
    }

    #region ICommand Members

    private static readonly ILog Log = LogManager.GetLogger(typeof(SimpleCommand<T>));

    /// <summary>
    /// Checks if the command Execute method can run
    /// </summary>
    /// <param name="parameter_">THe command parameter to be passed</param>
    /// <returns>Returns true if the command can execute. By default true is returned so that if the user of SimpleCommand does not specify a CanExecuteCommand delegate the command still executes.</returns>
    public bool CanExecute(object parameter_)
    {
      if (CanExecuteDelegate != null)
      {
        if (parameter_ is T)
        {
          return (this as ICommand<T>).CanExecute((T)parameter_);
        }
        if (!ReferenceEquals(default(T) , parameter_)) {
          Log.Error("Bad parameter type for method " + CanExecuteDelegate.Method.Name + ", expected " + typeof(T) + " got: " + (parameter_ == null ? "(null)" : parameter_.GetType().FullName));
        }
        return (this as ICommand<T>).CanExecute(default(T));
      }
      return true;// if there is no can execute default to true
    }


    bool ICommand<T>.CanExecute(T parameter_)
    {
      return CanExecuteDelegate(parameter_);
    }

    public event EventHandler CanExecuteChanged;

    /// <summary>
    /// Executes the actual command
    /// </summary>
    /// <param name="parameter_">THe command parameter to be passed</param>
    public void Execute(object parameter_)
    {
      if (ExecuteDelegate != null)
      {
        if (parameter_ is T)
        {
          (this as ICommand<T>).Execute((T)parameter_);
        }
        else
        {
          if (!ReferenceEquals(default(T), parameter_))
          {
            Log.Error("Bad parameter type for method " + ExecuteDelegate.Method.Name + ", expected " + typeof(T) + " got: " + (parameter_ == null ? "(null)" : parameter_.GetType().FullName));
          }
          (this as ICommand<T>).Execute(default(T));
        }
      }
    }

    void ICommand<T>.Execute(T parameter_)
    {
      ExecuteDelegate(parameter_);
    }

    public void RaiseCanExecuteChanged()
    {
      if (CanExecuteChanged != null)
      {
        CanExecuteChanged(this, EventArgs.Empty);
      }
    }


    #endregion
  }
}
