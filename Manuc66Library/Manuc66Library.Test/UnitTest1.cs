using Be.Manuc66.Library;
using Be.Manuc66.Library.DevTools;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Manuc66Library.Test
{
  [TestClass]
  public class UnitTest1
  {

    [TestMethod]
    public void TestMethod1()
    {
      var csproj = @"C:\Workspace\Demo\Controler\ControlCom\ControlCom\ControlCom\ControlCom.csproj";
      string destValue = @"C:\Workspace\Demo\Application\AppDemo\AppDemo\bin\Debug\DemoApp.exe";
      UserCsproj userCsproj = new UserCsproj();

      userCsproj.GenerateUserCsprojDebugWith(csproj, destValue);
      Assert.AreEqual(userCsproj.GetUserCsprojDebugWith(csproj), destValue);

      userCsproj.GenerateUserCsprojDebugWith(csproj, destValue + ".");
      Assert.AreNotEqual(userCsproj.GetUserCsprojDebugWith(csproj), destValue);

      userCsproj.GenerateUserCsprojDebugWith(csproj, destValue);
      Assert.AreEqual(userCsproj.GetUserCsprojDebugWith(csproj), destValue);

      Assert.IsTrue(userCsproj.IsCSharpNotTestProject(csproj));
      Assert.IsFalse(userCsproj.IsCSharpNotTestProject(@"C:\Workspace\Demo\Logic\PollingService\PollingService.Test\PollingService.Test.csproj"));
    }
  }
}
