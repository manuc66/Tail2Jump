using System;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Threading;
using Be.Manuc66.Library.Debug;
using Be.Manuc66.Library.FileIO;
using Be.Manuc66.Library.Log;
using Microsoft.Practices.Prism.Regions;
using Microsoft.Practices.ServiceLocation;
using log4net;
using Microsoft.Practices.Unity;

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
      System.Windows.Application.Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;

      // Set the unhandled exception mode to force all Windows Forms errors to go through our handler.
      System.Windows.Forms.Application.ThreadException += Application_ThreadException;

      // Set the unhandled exception mode to force all Windows Forms errors to go through our handler.
      System.Windows.Forms.Application.SetUnhandledExceptionMode(System.Windows.Forms.UnhandledExceptionMode.CatchException);

      // Add the event handler for handling non-UI thread exceptions to the event. 
      AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
    }
    protected override void OnStartup(StartupEventArgs e_)
    {
      var bootstrapper = new Bootstrapper();
      bootstrapper.Run();
    }


    public void Current_DispatcherUnhandledException(object sender_, DispatcherUnhandledExceptionEventArgs e_)
    {
      HandleException(e_.Exception);
      e_.Handled = true;
    }

    public void Application_ThreadException(object sender_, ThreadExceptionEventArgs e_)
    {
      HandleException(e_.Exception);
    }
    static void CurrentDomain_UnhandledException(object sender_, UnhandledExceptionEventArgs e_)
    {
      var ex = (Exception)e_.ExceptionObject;
      HandleException(ex);
    }

    private static readonly object HandleExceptionLock = new object();

    private static void HandleException(Exception ex_)
    {
      lock (HandleExceptionLock)
      {
        DialogResult res;
        try
        {
          var dumpFileName = MiniDump.MiniDumpWriteDump();

          res = System.Windows.Forms.MessageBox.Show(string.Format(@"Whoops! An Unhanled exception has been detected in the application.
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
