using System;
using System.IO;
using System.Text;
using System.Threading;

namespace ProduceLogs
{
  class Program
  {
    static void Main(string[] args)
    {
      Random r = new Random();

      Directory.CreateDirectory(@"C:\Logs\Demo\");
      string[] fileToProcess = new string[] { @"C:\Logs\Demo\service1.log", @"C:\Logs\Demo\service2.log", @"C:\Logs\Demo\service3.log", };

      int i = 0;
      int j = 0;
      while (true)
      {
        Encoding encoding = Encoding.GetEncoding("UTF-8");
        foreach (String filePath in fileToProcess)
        {
          j++;
          Directory.CreateDirectory(@"C:\Logs\Demo\");
          StreamWriter streamWriter = System.IO.File.AppendText(filePath);
          try
          {
            i = i % 3;
            if (i == 0)
            {
              streamWriter.WriteLine(j + " " + Environment.StackTrace.ToString());
            }
            else
            {
              streamWriter.WriteLine(j + " " + new ApplicationException().ToString());
            }
            i++;
          }
          finally
          {
            try { streamWriter.Flush(); }
            catch { }
            try { streamWriter.Close(); }
            catch { }
            try { streamWriter.Dispose(); }
            catch { }
          }

          Thread.Sleep(/*7*/5 * ((r.Next() % 5) + 1));
        }


        Thread.Sleep(350 * ((r.Next() % 7) + 1));
      }
    }
  }
}
