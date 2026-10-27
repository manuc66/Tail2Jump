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
      var temporaryPath = ConfigFilePath + ".tmp";

      try
      {
        var directory = Path.GetDirectoryName(ConfigFilePath);
        if (!string.IsNullOrEmpty(directory))
        {
          Directory.CreateDirectory(directory);
        }

        Conf ??= new AppConfig();

        var json = JsonSerializer.Serialize(Conf, new JsonSerializerOptions { WriteIndented = true });

        // Write to a temporary file first so an interrupted save cannot corrupt
        // the configuration.
        File.WriteAllText(temporaryPath, json);
        File.Move(temporaryPath, ConfigFilePath, overwrite: true);
        return true;
      }
      catch (Exception e) when (e is IOException || e is UnauthorizedAccessException ||
                                e is JsonException || e is ArgumentException || e is NotSupportedException)
      {
        TryDelete(temporaryPath);
        Log.LogError(e, "Fail to save config to {Path}", ConfigFilePath);
        return false;
      }
    }

    private static void Normalize(AppConfig config)
    {
      config.Search ??= new ListCriterianSearch();
      config.DirectoryHistory ??= new SerialStringList();
      config.FilterHistory ??= new SerialStringList();
    }

    private static void TryDelete(string path)
    {
      try
      {
        if (File.Exists(path)) File.Delete(path);
      }
      catch (Exception)
      {
        // Best effort only.
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
        Normalize(Conf);
        return true;
      }
      catch (Exception e) when (e is IOException || e is UnauthorizedAccessException ||
                                e is JsonException || e is ArgumentException || e is NotSupportedException)
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

    /// <summary>
    /// Remembers a watcher (history + search criteria) without duplicating it.
    /// </summary>
    public static void RememberCriterion(string path, string filter, bool recursive)
    {
      AddCriterianSearch(path, filter);

      var criterion = new CriterianSearch
      {
        PathDirectory = path ?? string.Empty,
        FileExtension = filter ?? string.Empty,
        Recursive = recursive
      };

      bool alreadyKnown = Conf.Search.Any(existing =>
        string.Equals(existing.PathDirectory, criterion.PathDirectory, StringComparison.Ordinal) &&
        string.Equals(existing.FileExtension, criterion.FileExtension, StringComparison.Ordinal) &&
        existing.Recursive == criterion.Recursive);

      if (!alreadyKnown)
      {
        Conf.Search.Add(criterion);
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

  }
}
