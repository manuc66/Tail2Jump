
using System;
using System.Diagnostics;
using System.Text;
namespace ConsoleApplication1
{
  class Program
  {
    public static void Main(string[] args)
    {
      StringBuilder sb = new StringBuilder();
      EventLog log = new EventLog("Security");

      DateTime dt = DateTime.Now.AddDays(-1.00);
      //Console.WriteLine(dt.ToString());


      DateTime lastLogin = DateTime.MinValue;
      DateTime lastLogOff = DateTime.MinValue;
      int state = 0;
      for (int i = 0; i < log.Entries.Count; ++i)
      {
        EventLogEntry entry = log.Entries[i]; if (entry.TimeGenerated < dt)
        {
          continue;
        }

        string tutu = entry.Category;
        if (entry.CategoryNumber == 12545 && entry.Message.Contains("Fermeture") && entry.Message.Contains("Nom du compte :\t\t" + Environment.UserName + "\r\n\t"))
        {

          var elapsed = entry.TimeGenerated - lastLogOff;
          lastLogOff = entry.TimeGenerated;
          if (elapsed > TimeSpan.FromSeconds(1))
          {
            if (state == 2)
            {
            }
            state = 2;
            Console.WriteLine("Logoff at: " + entry.TimeGenerated.ToString());
          }
        }
        else if (entry.CategoryNumber == 12544 && entry.Message.Contains("L’ouverture de session d’un compte s’est correctement déroulée.") && entry.Message.Contains("Nom du compte :\t\t" + Environment.UserName + "\r\n\t"))
        {
          var elapsed = entry.TimeGenerated - lastLogin;
          lastLogin = entry.TimeGenerated;
          if (elapsed > TimeSpan.FromSeconds(1))
          {
            state = 1;
            Console.WriteLine("Login at: " + entry.TimeGenerated.ToString());
          }
        }
      }

      Console.WriteLine("Done!");
      Console.ReadLine();
    }
  }
}
