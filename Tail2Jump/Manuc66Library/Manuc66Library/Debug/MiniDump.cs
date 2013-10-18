// -----------------------------------------------------------------------
// <copyright file="Debug.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Be.Manuc66.Library.Debug
{
  /// <summary>
  /// TODO: Update summary.
  /// </summary>
  public class MiniDump
  {
    // Taken almost verbatim from http://blog.kalmbach-software.de/2008/12/13/writing-minidumps-in-c/

    [Flags]

    public enum Option : uint
    {

      // From dbghelp.h:

      Normal = 0x00000000,
      WithDataSegs = 0x00000001,
      WithFullMemory = 0x00000002,
      WithHandleData = 0x00000004,
      FilterMemory = 0x00000008,
      ScanMemory = 0x00000010,
      WithUnloadedModules = 0x00000020,
      WithIndirectlyReferencedMemory = 0x00000040,
      FilterModulePaths = 0x00000080,
      WithProcessThreadData = 0x00000100,
      WithPrivateReadWriteMemory = 0x00000200,
      WithoutOptionalData = 0x00000400,
      WithFullMemoryInfo = 0x00000800,
      WithThreadInfo = 0x00001000,
      WithCodeSegs = 0x00002000,
      WithoutAuxiliaryState = 0x00004000,
      WithFullAuxiliaryState = 0x00008000,
      WithPrivateWriteCopyMemory = 0x00010000,
      IgnoreInaccessibleMemory = 0x00020000,
      ValidTypeFlags = 0x0003ffff,
    };



    public enum ExceptionInfo
    {

      None,

      Present

    }



    //typedef struct _MINIDUMP_EXCEPTION_INFORMATION {

    //    DWORD ThreadId;

    //    PEXCEPTION_POINTERS ExceptionPointers;

    //    BOOL ClientPointers;

    //} MINIDUMP_EXCEPTION_INFORMATION, *PMINIDUMP_EXCEPTION_INFORMATION;

    [StructLayout(LayoutKind.Sequential, Pack = 4)]  // Pack=4 is important! So it works also for x64!

    public struct MiniDumpExceptionInformation
    {

      public uint ThreadId;

      public IntPtr ExceptionPointers;

      [MarshalAs(UnmanagedType.Bool)]

      public bool ClientPointers;

    }




    [DllImport("dbghelp.dll", EntryPoint = "MiniDumpWriteDump", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
    static extern bool MiniDumpWriteDump(IntPtr hProcess_, uint processId_, SafeHandle hFile_, uint dumpType_, ref MiniDumpExceptionInformation expParam_, IntPtr userStreamParam_, IntPtr callbackParam_);

    // Overload supporting MiniDumpExceptionInformation == NULL

    [DllImport("dbghelp.dll", EntryPoint = "MiniDumpWriteDump", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
    static extern bool MiniDumpWriteDump(IntPtr hProcess_, uint processId_, SafeHandle hFile_, uint dumpType_, IntPtr expParam_, IntPtr userStreamParam_, IntPtr callbackParam_);

    [DllImport("kernel32.dll", EntryPoint = "GetCurrentThreadId", ExactSpelling = true)]
    static extern uint GetCurrentThreadId();

    public static string MiniDumpWriteDump()
    {
      string assemblyPath = Assembly.GetEntryAssembly().Location;
      string dumpFileName = assemblyPath + "_" + DateTime.Now.ToString("dd.MM.yyyy.HH.mm.ss") + ".dmp";




      using (var file = new FileStream(dumpFileName, FileMode.Create))
      {
        Process currentProcess = Process.GetCurrentProcess();

        IntPtr currentProcessHandle = currentProcess.Handle;

        var currentProcessId = (uint)currentProcess.Id;

        MiniDumpExceptionInformation exp;

        exp.ThreadId = GetCurrentThreadId();

        exp.ClientPointers = false;

        exp.ExceptionPointers = IntPtr.Zero;

        //if (exceptionInfo == ExceptionInfo.Present)
        //{

        exp.ExceptionPointers = Marshal.GetExceptionPointers();

        //}


        const Option op = ((Option.ValidTypeFlags ^ Option.FilterMemory) ^ Option.FilterModulePaths) ^ Option.IgnoreInaccessibleMemory;
        if (exp.ExceptionPointers == IntPtr.Zero)
        {
          MiniDumpWriteDump(currentProcessHandle, currentProcessId, file.SafeFileHandle, (uint)op, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
        }
        else
        {
          MiniDumpWriteDump(currentProcessHandle, currentProcessId, file.SafeFileHandle, (uint)op, ref exp, IntPtr.Zero, IntPtr.Zero);
        }
      }
      return dumpFileName;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct MinidumpExceptionInformation
    {
      public uint ThreadId;
      public IntPtr ExceptionPointers;
      public int ClientPointers;
    }
  }
}
