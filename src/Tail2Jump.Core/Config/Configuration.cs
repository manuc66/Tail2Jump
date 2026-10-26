using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Tail2Jump.Config.Objects
{
  public class CriterianSearch
  {
    public string PathDirectory { get; set; } = string.Empty;
    public string FileExtension { get; set; } = string.Empty;
    public bool Recursive { get; set; }
  }

  public class ListCriterianSearch : List<CriterianSearch>
  {
  }

  public class SerialStringList : List<string>
  {
  }

  public class AppConfig
  {
    public ListCriterianSearch Search { get; set; } = new ListCriterianSearch();
    public SerialStringList DirectoryHistory { get; set; } = new SerialStringList();
    public SerialStringList FilterHistory { get; set; } = new SerialStringList();
  }

  /// <summary>
  /// Configuration persistence. The original SOAP file (SaveConfig.data) is
  /// replaced by JSON stored in the per-user configuration directory.
  /// </summary>
  public class CFG
  {
    private static readonly ILogger Log = Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;

    public static AppConfig Conf { get; set; } = new AppConfig();

    public static string ConfigFilePath { get; set; } = DefaultConfigFilePath();

    private static string DefaultConfigFilePath()
    {
      var configDirectory = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
      if (string.IsNullOrWhiteSpace(configDirectory))
      {
        configDirectory = Path.Combine(
          Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Tail2Jump");
      }
      else
      {
        configDirectory = Path.Combine(configDirectory, "Tail2Jump");
      }

      var overridePath = Environment.GetEnvironmentVariable("TAIL2JUMP_CONFIG");
      return string.IsNullOrWhiteSpace(overridePath)
        ? Path.Combine(configDirectory, "SaveConfig.json")
        : overridePath;
    }

    public static bool Save()
    {
      try
      {
        var directory = Path.GetDirectoryName(ConfigFilePath);
        if (!string.IsNullOrEmpty(directory))
        {
          Directory.CreateDirectory(directory);
        }

        Conf ??= new AppConfig();

        var json = JsonSerializer.Serialize(Conf, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(ConfigFilePath, json);
        return true;
      }
      catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is JsonException)
      {
        Log.LogError(e, "Fail to save config to {Path}", ConfigFilePath);
        return false;
      }
    }

    public static bool Load()
    {
      try
      {
        if (!File.Exists(ConfigFilePath))
        {
          Conf = new AppConfig();
          return false;
        }

        var json = File.ReadAllText(ConfigFilePath);
        var deserialized = JsonSerializer.Deserialize<AppConfig>(json);
        Conf = deserialized ?? new AppConfig();
        return true;
      }
      catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is JsonException)
      {
        Conf = new AppConfig();
        Log.LogError(e, "Fail to load config from {Path}", ConfigFilePath);
        return false;
      }
    }

    public static void AddCriterianSearch(string path, string filter)
    {
      Conf ??= new AppConfig();

      if (!string.IsNullOrEmpty(path))
      {
        Utils.AddUnique(new[] { path }, Conf.DirectoryHistory);
      }

      if (!string.IsNullOrEmpty(filter))
      {
        Utils.AddUnique(new[] { filter }, Conf.FilterHistory);
      }
    }
  }

  public static class Utils
  {
    public static void AddUnique<T>(IEnumerable<T> from, ICollection<T> to)
    {
      foreach (var item in from.Where(item => !to.Contains(item)))
      {
        to.Add(item);
      }
    }

    public static void MergeList(IEnumerable<string> oldConf, IEnumerable<string> current, IList<string> newConf)
    {
      AddUnique(current, newConf);
      AddUnique(oldConf, newConf);
    }
  }
}
