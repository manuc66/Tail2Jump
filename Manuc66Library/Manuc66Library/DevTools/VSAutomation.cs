using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using EnvDTE;
using log4net;

namespace Be.Manuc66.Library.DevTools
{
  public class VsAutomation : IVsAutomation
  {
    readonly ILog _log = LogManager.GetLogger(typeof(VsAutomation));
    public const string UnknownSolution = "unknow solution";

    [DllImport("ole32.dll", ExactSpelling = true, PreserveSig = false)]
    private static extern IRunningObjectTable GetRunningObjectTable(int reserved);
    [DllImport("ole32.dll", ExactSpelling = true, PreserveSig = false)]
    public static extern IBindCtx CreateBindCtx(int reserved);

    [STAThread]
    public List<VSInfo> GetAllVs()
    {
      var result = new List<VSInfo>();
      try
      {
        IEnumMoniker pMonkEnum;
        var monikers = new IMoniker[1];

        IRunningObjectTable prot = GetRunningObjectTable(0);
        prot.EnumRunning(out pMonkEnum);
        pMonkEnum.Reset();          // Churn through enumeration.       
        var numFetched = (IntPtr)0;
        while (pMonkEnum.Next(1, monikers, numFetched) == 0)
        {
          try
          {
            IBindCtx pCtx = CreateBindCtx(0);
            string displayName;
            monikers[0].GetDisplayName(pCtx, null, out displayName);
            if (displayName.StartsWith("!VisualStudio.DTE"))
            {
              object objReturnObject;
              prot.GetObject(monikers[0], out objReturnObject);
              var idee = objReturnObject as _DTE;
              if (idee != null)
              {
                string solName = UnknownSolution;
                bool indebug = false;

                //#if (DEBUG)

                //#else
                try
                {
                  solName = idee.Solution.FullName;
                  indebug = idee.Debugger.CurrentMode != dbgDebugMode.dbgDesignMode;
                }
                catch (COMException error)
                {
                  _log.Debug(error);
                }
                //#endif

                result.Add(new VSInfo(solName, idee, displayName, indebug));
              }
            }
          }
          catch (COMException error)
          {
            _log.Debug(error);
          }
        }
      }
      catch (Exception error)
      {
        _log.Error(error);
        return null;
      }
      return result;
    }
  }
}
