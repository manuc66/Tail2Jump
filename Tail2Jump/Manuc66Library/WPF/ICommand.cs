using System.Windows.Input;

namespace Be.Manuc66.Library
{
  public interface ICommand<in T> : ICommand
  {
    bool CanExecute(T parameter_);
    void Execute(T parameter_);

    void RaiseCanExecuteChanged();
  }
}