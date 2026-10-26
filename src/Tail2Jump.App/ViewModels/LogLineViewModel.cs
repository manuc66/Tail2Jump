using System.Collections.Generic;
using Be.Manuc66.Library.Mvvm;
using Tail2Jump.Rendering;

namespace Tail2Jump.ViewModels
{
  public class LogLineViewModel : ObservableObject
  {
    private IReadOnlyList<LogLineSegment> _segments;

    public LogLineViewModel(string rawLine)
    {
      RawLine = rawLine;
      _segments = LogLineParser.Parse(rawLine);
    }

    public string RawLine { get; }

    public IReadOnlyList<LogLineSegment> Segments
    {
      get { return _segments; }
      private set { SetProperty(ref _segments, value); }
    }

    public void ApplySearch(string searchText, bool ignoreCase)
    {
      Segments = LogLineParser.Parse(RawLine, searchText, ignoreCase);
    }
  }
}
