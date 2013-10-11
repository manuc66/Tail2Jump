using System;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Threading;
using Be.Manuc66.Library.Debug;
using Be.Manuc66.Library.Log;
using log4net;
using MessageBox = System.Windows.Forms.MessageBox;

namespace Tail2Jump
{
  /// <summary>
  /// Interaction logic for App.xaml
  /// </summary>
  public partial class App
  {
    static App()
    {
      Trace.Listeners.Add(new Log4NetTraceListener());
    }
    private static readonly ILog Log = LogManager.GetLogger(typeof(App));
    public App()
    {


      AppDomain.CurrentDomain.UnhandledException +=
        CurrentDomain_UnhandledException;
      // WPF
      Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;

      // Set the unhandled exception mode to force all Windows Forms errors to go through our handler.
      System.Windows.Forms.Application.ThreadException += Application_ThreadException;

      // Set the unhandled exception mode to force all Windows Forms errors to go through our handler.
      System.Windows.Forms.Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

      // Add the event handler for handling non-UI thread exceptions to the event. 
      AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
    }
    protected override void OnStartup(StartupEventArgs e)
    {
      var bootstrapper = new Bootstrapper();
      bootstrapper.Run();
    }


    public void Current_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
      HandleException();
      e.Handled = true;
    }

    public void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
    {
      HandleException();
    }
    static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
      HandleException();
    }

    private static readonly object HandleExceptionLock = new object();

    private static void HandleException()
    {
      lock (HandleExceptionLock)
      {
        DialogResult res;
        try
        {
          var dumpFileName = MiniDump.MiniDumpWriteDump();

          res = MessageBox.Show(string.Format(@"Whoops! An Unhanled exception has been detected in the application.
Exception information is saved in {0} Please contact the developers with the dump, Continue or exit ?", dumpFileName),
                 @"Fatal Error Try continue ?", MessageBoxButtons.YesNo, MessageBoxIcon.Stop);
        }
        catch (Exception error)
        {
          Log.Error(error);
          res = DialogResult.No;
        }
        if (res != DialogResult.Yes)
        {
          Current.Shutdown();
        }
      }
    }
  }

}
