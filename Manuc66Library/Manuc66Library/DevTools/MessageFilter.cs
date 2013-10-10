using System;
using System.Runtime.InteropServices;

namespace Be.Manuc66.Library.DevTools
{
  public class MessageFilter : IOleMessageFilter
  {
    //
    // Class containing the IOleMessageFilter
    // thread error-handling functions.

    // Start the filter.
    public static void Register()
    {
      IOleMessageFilter newFilter = new MessageFilter();
      IOleMessageFilter oldFilter;
      CoRegisterMessageFilter(newFilter, out oldFilter);
    }

    // Done with the filter, close it.
    public static void Revoke()
    {
      IOleMessageFilter oldFilter;
      CoRegisterMessageFilter(null, out oldFilter);
    }

    //
    // IOleMessageFilter functions.
    // Handle incoming thread requests.
    int IOleMessageFilter.HandleInComingCall(int dwCallType_, IntPtr hTaskCaller_, int dwTickCount_, IntPtr lpInterfaceInfo_)
    {
      //Return the flag SERVERCALL_ISHANDLED.
      return 0;
    }

    // Thread call was rejected, so try again.
    int IOleMessageFilter.RetryRejectedCall(IntPtr hTaskCallee_, int dwTickCount_, int dwRejectType_)
    {
      if (dwRejectType_ == 2)
      // flag = SERVERCALL_RETRYLATER.
      {
        // Retry the thread call immediately if return >=0 & 
        // <100.
        return 99;
      }
      // Too busy; cancel call.
      return -1;
    }

    int IOleMessageFilter.MessagePending(IntPtr hTaskCallee_, int dwTickCount_, int dwPendingType_)
    {
      //Return the flag PENDINGMSG_WAITDEFPROCESS.
      return 2;
    }

    // Implement the IOleMessageFilter interface.
    [DllImport("Ole32.dll")]
    private static extern int CoRegisterMessageFilter(IOleMessageFilter newFilter_, out IOleMessageFilter oldFilter_);
  }

  [ComImport, Guid("00000016-0000-0000-C000-000000000046"),
  InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
  interface IOleMessageFilter
  {
    [PreserveSig]
    int HandleInComingCall(
        int dwCallType_,
        IntPtr hTaskCaller_,
        int dwTickCount_,
        IntPtr lpInterfaceInfo_);

    [PreserveSig]
    int RetryRejectedCall(
        IntPtr hTaskCallee_,
        int dwTickCount_,
        int dwRejectType_);

    [PreserveSig]
    int MessagePending(
        IntPtr hTaskCallee_,
        int dwTickCount_,
        int dwPendingType_);
  }
}

