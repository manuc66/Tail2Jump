using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Soap;
using log4net;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Tail2Jump
{
  [Serializable]
  public class CFG
  {
    public static Config Conf { get; set; }

    private static readonly ILog Log = LogManager.GetLogger(typeof(CFG));

    #region Methods
    public static bool Save()
    {
      var result = false;

      try
      {
        using (var fs = new FileStream("SaveConfig.data", FileMode.Create))
        {
          var sf = new SoapFormatter();

          if (Conf == null)
          {
            Conf = new Config();
          }

          sf.Serialize(fs, Conf);
        }
        result = true;
      }
      catch (IOException e)
      {
        Log.Error("Fail to save config", e);
      }
      return result;
    }

    public static bool Load()
    {
      var result = false;

      try
      {
        const string saveConfigFile = "SaveConfig.data";
        if (File.Exists(saveConfigFile))
        {
          using (var fs = new FileStream(saveConfigFile, FileMode.Open))
          {
            var sf = new SoapFormatter();
            var odeserialised = sf.Deserialize(fs);
            Conf = (Config) odeserialised;
            result = true;
          }
        }
      }
      catch (SerializationException e)
      {
        Conf = new Config();

        Log.Error("Fail to load config", e);
      }
      catch (IOException e)
      {
        Conf = new Config();

        Log.Error("Fail to load config", e);
      }
      return result;
    }

    public static void AddCriterianSearch(string path, string filter)
    {
      if (path != string.Empty)
      {
        Utils.AddUnique(Conf.DirectoryHistory, new List<string> { path });
      }

      if (filter != string.Empty)
      {
        Utils.AddUnique(Conf.FilterHistory, new List<string> { filter });
      }
    }
    #endregion // Methods

    #region Constructor
    public CFG()
    {
      Conf = new Config();
    }
    #endregion // Constructor
  }
}
