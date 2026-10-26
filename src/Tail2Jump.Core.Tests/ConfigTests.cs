using System;
using System.IO;
using NUnit.Framework;
using Tail2Jump.Config.Objects;

namespace Tail2Jump.Core.Tests
{
  [TestFixture]
  public class AppConfigTests
  {
    private string _configPath = null!;

    [SetUp]
    public void SetUp()
    {
      _configPath = Path.Combine(Path.GetTempPath(), "tail2jump-tests", Guid.NewGuid() + ".json");
      CFG.ConfigFilePath = _configPath;
      CFG.Conf = new AppConfig();
    }

    [TearDown]
    public void TearDown()
    {
      if (File.Exists(_configPath)) File.Delete(_configPath);
    }

    [Test]
    public void SaveAndLoad_RoundTripsTheConfiguration()
    {
      CFG.Conf.DirectoryHistory.Add("/var/log");
      CFG.Conf.FilterHistory.Add("*.log");
      CFG.Conf.Search.Add(new CriterianSearch
      {
        PathDirectory = "/var/log",
        FileExtension = "*.log",
        Recursive = true
      });

      Assert.That(CFG.Save(), Is.True);

      CFG.Conf = new AppConfig();
      Assert.That(CFG.Load(), Is.True);

      Assert.That(CFG.Conf.DirectoryHistory, Is.EqualTo(new[] { "/var/log" }));
      Assert.That(CFG.Conf.FilterHistory, Is.EqualTo(new[] { "*.log" }));
      Assert.That(CFG.Conf.Search, Has.Count.EqualTo(1));
      Assert.That(CFG.Conf.Search[0].Recursive, Is.True);
    }

    [Test]
    public void AddCriterianSearch_AddsUniqueEntries()
    {
      CFG.AddCriterianSearch("/var/log", "*.log");
      CFG.AddCriterianSearch("/var/log", "*.log");

      Assert.That(CFG.Conf.DirectoryHistory, Is.EqualTo(new[] { "/var/log" }));
      Assert.That(CFG.Conf.FilterHistory, Is.EqualTo(new[] { "*.log" }));
    }

    [Test]
    public void RememberCriterion_AddsTheSearchCriterionOnce()
    {
      CFG.RememberCriterion("/var/log", "*.log", recursive: false);
      CFG.RememberCriterion("/var/log", "*.log", recursive: false);

      Assert.That(CFG.Conf.Search, Has.Count.EqualTo(1));
      Assert.That(CFG.Conf.Search[0].PathDirectory, Is.EqualTo("/var/log"));
      Assert.That(CFG.Conf.DirectoryHistory, Is.EqualTo(new[] { "/var/log" }));
      Assert.That(CFG.Conf.FilterHistory, Is.EqualTo(new[] { "*.log" }));
    }

    [Test]
    public void RememberCriterion_KeepsRecursiveAndNonRecursiveVariants()
    {
      CFG.RememberCriterion("/var/log", "*.log", recursive: false);
      CFG.RememberCriterion("/var/log", "*.log", recursive: true);

      Assert.That(CFG.Conf.Search, Has.Count.EqualTo(2));
    }

    [Test]
    public void Load_WithNullCollections_NormalizesThem()
    {
      Directory.CreateDirectory(Path.GetDirectoryName(_configPath)!);
      File.WriteAllText(_configPath, "{\"Search\":null,\"DirectoryHistory\":null,\"FilterHistory\":null}");

      Assert.That(CFG.Load(), Is.True);
      Assert.That(CFG.Conf.Search, Is.Not.Null);
      Assert.That(CFG.Conf.DirectoryHistory, Is.Not.Null);
      Assert.That(CFG.Conf.FilterHistory, Is.Not.Null);
      Assert.DoesNotThrow(() => CFG.RememberCriterion("/var/log", "*.log", false));
    }

    [Test]
    public void Save_DoesNotLeaveATemporaryFile()
    {
      Directory.CreateDirectory(Path.GetDirectoryName(_configPath)!);

      Assert.That(CFG.Save(), Is.True);
      Assert.That(File.Exists(_configPath), Is.True);
      Assert.That(File.Exists(_configPath + ".tmp"), Is.False);
    }

    [Test]
    public void Save_WithAnInvalidPath_ReturnsFalseInsteadOfThrowing()
    {
      CFG.ConfigFilePath = string.Empty;

      Assert.That(CFG.Save(), Is.False);
    }

    [Test]
    public void Load_WithMissingFile_InitializesAnEmptyConfiguration()
    {
      Assert.That(CFG.Load(), Is.False);
      Assert.That(CFG.Conf, Is.Not.Null);
      Assert.That(CFG.Conf.DirectoryHistory, Is.Empty);
    }
  }
}
