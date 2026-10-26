using System.Collections.Generic;
using Tail2Jump.Rendering;

namespace Tail2Jump.ViewModels
{
  public class LogLineViewModel
  {
    public LogLineViewModel(string rawLine)
    {
      RawLine = rawLine;
      Segments = LogLineParser.Parse(rawLine);
    }

    public string RawLine { get; }

    public IReadOnlyList<LogLineSegment> Segments { get; }
  }
}
