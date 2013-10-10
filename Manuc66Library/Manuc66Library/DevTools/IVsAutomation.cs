using System;
using System.Collections.Generic;
namespace Be.Manuc66.Library.DevTools
{
  public interface IVsAutomation
  {
    List<VSInfo> GetAllVs();
  }
}
