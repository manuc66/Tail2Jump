using System;
using System.IO;
using System.Text;
using Be.Manuc66.Library.FileIO;
using NUnit.Framework;

namespace Tail2Jump.Core.Tests
{
  [TestFixture]
  public class RandomLineFileAccessorTests
  {
    private string _file = null!;

    [SetUp]
    public void SetUp()
    {
      _file = Path.Combine(Path.GetTempPath(), "tail2jump-tests", Guid.NewGuid().ToString("N") + ".log");
      Directory.CreateDirectory(Path.GetDirectoryName(_file)!);
    }

    [TearDown]
    public void TearDown()
    {
      if (File.Exists(_file)) File.Delete(_file);
    }

    [Test]
    public void IndexAndReadLines()
    {
      File.WriteAllText(_file, "line0\nline1\nline2\nline3\n");
      var accessor = new RandomLineFileAccessor(_file);

      accessor.IndexFile();

      Assert.That(accessor.LineIndexed, Is.EqualTo(4));
      Assert.That(accessor.GetLines(0, 2), Is.EqualTo(new[] { "line0", "line1" }));
      Assert.That(accessor.GetLines(2, 10), Is.EqualTo(new[] { "line2", "line3" }));
      Assert.That(accessor.GetLines(4, 1), Is.Empty);
    }

    [Test]
    public void IndexesTheLastLineWithoutTrailingNewLine()
    {
      File.WriteAllText(_file, "first\nlast");
      var accessor = new RandomLineFileAccessor(_file);

      accessor.IndexFile();

      Assert.That(accessor.LineIndexed, Is.EqualTo(2));
      Assert.That(accessor.GetLines(1, 1), Is.EqualTo(new[] { "last" }));
    }

    [Test]
    public void StripsCarriageReturns()
    {
      File.WriteAllText(_file, "one\r\ntwo\r\n");
      var accessor = new RandomLineFileAccessor(_file);

      accessor.IndexFile();

      Assert.That(accessor.GetLines(0, 2), Is.EqualTo(new[] { "one", "two" }));
    }

    [Test]
    public void DecodesMultiByteCharacters()
    {
      File.WriteAllText(_file, "héllo\nwörld\n", Encoding.UTF8);
      var accessor = new RandomLineFileAccessor(_file);

      accessor.IndexFile();

      Assert.That(accessor.GetLines(0, 2), Is.EqualTo(new[] { "héllo", "wörld" }));
    }

    [Test]
    public void HandlesEmptyFiles()
    {
      File.WriteAllText(_file, string.Empty);
      var accessor = new RandomLineFileAccessor(_file);

      accessor.IndexFile();

      Assert.That(accessor.LineIndexed, Is.Zero);
      Assert.That(accessor.GetLines(0, 1), Is.Empty);
    }
  }
}
