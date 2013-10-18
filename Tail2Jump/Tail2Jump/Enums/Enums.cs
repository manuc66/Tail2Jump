namespace Tail2Jump
{


  // A développer
  public enum FormReturnCode : byte
  { 
    None = 0,
    Start = 1,
    OK = 2,
    NOT_OK = 3,
    Cancel = 4,
  }

  public enum LogType : byte
  { 
    Base,
    Valid,
    Error,
    Exception,
    Critical
  }

  public enum GraphicsType : byte
  { 
    Memory,
    CpuUsage,
  }
}
