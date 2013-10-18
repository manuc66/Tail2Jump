using System.Collections.Generic;
using System.Linq;

namespace Tail2Jump
{
  public class Utils
  {
    #region Methods
    public static void AddUnique<T>(IEnumerable<T> from_, ICollection<T> to_)
    {
      foreach (var item in from_.Where(item_ => !to_.Contains(item_)))
      {
        to_.Add(item);
      }
    }

    public static void MergeList(List<string> oldConf_, IList<string> current_, List<string> newConf_)
    {
      AddUnique(current_, newConf_);
      AddUnique(oldConf_, newConf_);
    }
    #endregion // Methods
  }
}
