using System;

namespace Tail2Jump.Config.Objects
{
  [Serializable]
  public class Config //: ISerializable
  {

    #region Properties

    // ***
    // Rajouter ici tout ce qui doit etre sauvegardé et rechargé pour la configuration du logiciel !!
    // Bien faire attention que l'ensemble des objets soient [Serialisable] ou [Serialisable()] (si collection).
    // ***

    public ListCriterianSearch Search { get; private set; }
    public SerialStringList DirectoryHistory { get; private set; }
    public SerialStringList FilterHistory { get; private set; }

    #endregion // Properties

    #region Constructeur

    public Config()
    {
      DirectoryHistory = new SerialStringList();
      FilterHistory = new SerialStringList();
      Search = new ListCriterianSearch();
    }

    #endregion // Constructeur

  }
}
