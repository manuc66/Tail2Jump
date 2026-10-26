using System;
using System.IO;
using Be.Manuc66.Library.DevTools;
using NUnit.Framework;

namespace Tail2Jump.Core.Tests
{
  [TestFixture]
  public class FileLocationParserTests
  {
    private string _file = null!;

    [SetUp]
    public void SetUp()
    {
      _file = Path.Combine(Path.GetTempPath(), "tail2jump-tests-" + Guid.NewGuid().ToString("N") + ".cs");
      File.WriteAllText(_file, "// test");
    }

    [TearDown]
    public void TearDown()
    {
      if (File.Exists(_file)) File.Delete(_file);
    }

    [Test]
    public void Parse_ColonStyle_ReturnsPathAndLine()
    {
      var result = FileLocationParser.Parse($"{_file}:42");

      Assert.That(result, Is.Not.Null);
      Assert.That(result!.FilePath, Is.EqualTo(_file));
      Assert.That(result!.LineNumber, Is.EqualTo(42));
    }

    [Test]
    public void Parse_LineKeywordStyle_ReturnsPathAndLine()
    {
      var result = FileLocationParser.Parse($"{_file}:line 42");

      Assert.That(result, Is.Not.Null);
      Assert.That(result!.LineNumber, Is.EqualTo(42));
    }

    [Test]
    public void Parse_FrenchLineKeywordStyle_ReturnsPathAndLine()
    {
      var result = FileLocationParser.Parse($"{_file}:ligne 42");

      Assert.That(result, Is.Not.Null);
      Assert.That(result!.LineNumber, Is.EqualTo(42));
    }

    [Test]
    public void Parse_DotNetStyle_ReturnsPathAndLine()
    {
      var result = FileLocationParser.Parse($"{_file}(42,7)");

      Assert.That(result, Is.Not.Null);
      Assert.That(result!.LineNumber, Is.EqualTo(42));
    }

    [Test]
    public void Parse_WithoutLineNumber_ReturnsNull()
    {
      Assert.That(FileLocationParser.Parse(_file), Is.Null);
    }

    [Test]
    public void DetectEditors_AlwaysProvidesASystemDefault()
    {
      var editors = new EditorTools().DetectEditors();

      Assert.That(editors, Is.Not.Empty);
      Assert.That(editors, Has.Some.InstanceOf<SystemDefaultOpenWith>());
    }
  }
}
