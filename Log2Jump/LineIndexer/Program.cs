using System;
using Be.Manuc66.Library;
using Be.Manuc66.Library.FileIO;

namespace LineIndexer
{
  class Program
  {
    static void Main(string[] args)
    {
      var test = new RandomLineFileAccessor(@"C:\Logs\Demo\service3.log");
      var t0 = DateTime.Now;
      test.IndexFile();
      Console.WriteLine("Total line indexed: " + test.LineIndexed);
      var t1 = DateTime.Now;
      foreach (var item in test.ShowLines(10, 20))
      {
        Console.Write(item);
      }
      Console.WriteLine("Done!");
      Console.ReadLine();

    }

  }
}
