using System.Windows.Input;

namespace Be.Manuc66.Library.WPF
{
  public interface ICommand<in T> : ICommand
  {
    bool CanExecute(T parameter);
    void Execute(T parameter);

    void RaiseCanExecuteChanged();
  }
}