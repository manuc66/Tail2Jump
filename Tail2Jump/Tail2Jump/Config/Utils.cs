using System.Collections.Generic;
using System.Linq;

namespace Tail2Jump
{
  public class Utils
  {
    #region Methods
    public static void AddUnique<T>(IEnumerable<T> @from, ICollection<T> to)
    {
      foreach (var item in @from.Where(item => !to.Contains(item)))
      {
        to.Add(item);
      }
    }

    public static void MergeList(List<string> oldConf, IList<string> current, List<string> newConf)
    {
      AddUnique(current, newConf);
      AddUnique(oldConf, newConf);
    }
    #endregion // Methods
  }
}
