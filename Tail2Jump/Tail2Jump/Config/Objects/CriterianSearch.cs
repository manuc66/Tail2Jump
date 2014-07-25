using System;

namespace Tail2Jump.Config.Objects
{
  [Serializable()]
  public class CriterianSearch
  {
    #region Properties

    public string PathDirectory { get; set; }

    public string FileExtension { get; set; }

    public bool Recursive { get; set; }

    #endregion // Properties

    #region Members

    #endregion // Members

    #region Constructor

    public CriterianSearch()
    {
      Recursive = false;
      FileExtension = string.Empty;
      PathDirectory = string.Empty;
    }

    #endregion // Constructor
  }
}
