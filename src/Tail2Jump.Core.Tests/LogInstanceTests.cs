using NUnit.Framework;
using Tail2Jump.Enums;
using Tail2Jump.Objects;

namespace Tail2Jump.Core.Tests
{
  [TestFixture]
  public class LogInstanceTests
  {
    [Test]
    public void Constructor_SetsTheDisplayedTime()
    {
      var log = new LogInstance("hello", LogType.Valid);

      Assert.That(log.DisplayedDatetime, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public void SeverityFlags_MatchTheLogType()
    {
      Assert.That(new LogInstance("v", LogType.Valid).IsValid, Is.True);
      Assert.That(new LogInstance("e", LogType.Error).IsError, Is.True);
      Assert.That(new LogInstance("x", LogType.Exception).IsException, Is.True);
      Assert.That(new LogInstance("c", LogType.Critical).IsCritical, Is.True);
    }

    [Test]
    public void HasException_IsTrueOnlyWhenAnExceptionIsProvided()
    {
      var log = new LogInstance("boom", LogType.Exception) { Exception = new System.Exception("boom") };

      Assert.That(log.HasException, Is.True);
      Assert.That(log.ExceptionText, Does.Contain("boom"));
    }
  }
}
