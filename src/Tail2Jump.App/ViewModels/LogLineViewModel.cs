using System.Collections.Generic;
using Be.Manuc66.Library.Mvvm;
using Tail2Jump.Rendering;

namespace Tail2Jump.ViewModels
{
  public class LogLineViewModel : ObservableObject
  {
    private IReadOnlyList<LogLineSegment>? _segments;
    private string? _searchText;
    private bool _ignoreCase = true;

    public LogLineViewModel(string rawLine)
    {
      RawLine = rawLine;
    }

    public string RawLine { get; }

    /// <summary>
    /// Segments are parsed lazily: the list box only realizes visible rows, so
    /// a huge log does not pay for parsing lines that are never displayed.
    /// </summary>
    public IReadOnlyList<LogLineSegment> Segments =>
      _segments ??= LogLineParser.Parse(RawLine, _searchText, _ignoreCase);

    public void ApplySearch(string? searchText, bool ignoreCase)
    {
      if (_searchText == searchText && _ignoreCase == ignoreCase) return;

      _searchText = searchText;
      _ignoreCase = ignoreCase;
      _segments = null;

      OnPropertyChanged(nameof(Segments));
    }
  }
}
