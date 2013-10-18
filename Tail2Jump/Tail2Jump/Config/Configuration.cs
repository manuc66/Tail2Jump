using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Tail2Jump
{
  public class Configuration
  {
    public static List<CriterianSearch> CriterianSearchList { get; set; }

    public Configuration()
    {
      try
      {
        CriterianSearchList = new List<CriterianSearch>();
      }
      catch (Exception)
      {
        // EXCEPTION
      }
    }

    public static bool LoadConfiguration()
    {
      bool result = false;
      try
      {
        #region Load des critères de recherche.

        #endregion // Load des critères de recherche.
      }
      catch (Exception)
      {
        // EXCEPTION
      }
      return result;
    }
  }
}
