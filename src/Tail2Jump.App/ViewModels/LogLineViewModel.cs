using System.Collections.Generic;
using Tail2Jump.Mvvm;
using Tail2Jump.Rendering;

namespace Tail2Jump.ViewModels
{
  public class LogLineViewModel : ObservableObject
  {
    private readonly string _fileName;
    private readonly string _content;
    private IReadOnlyList<LogLineSegment>? _segments;
    private string? _searchText;
    private bool _ignoreCase = true;
    private string? _rawLine;

    public LogLineViewModel(string rawLine)
    {
      _fileName = string.Empty;
      _content = rawLine;
      _rawLine = rawLine;
    }

    public LogLineViewModel(string fileName, string content)
    {
      _fileName = fileName;
      _content = content;
    }

    /// <summary>
    /// "file|content", built on demand: hidden lines only keep two references
    /// (the file name is shared by every line of that file).
    /// </summary>
    public string RawLine =>
      _rawLine ??= _fileName.Length == 0 ? _content : string.Concat(_fileName, "|", _content);

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

      // Nothing is bound yet: the segments will be parsed with the new search
      // state when the line is realized. Avoid 50k useless notifications.
      if (_segments == null) return;

      _segments = null;
      OnPropertyChanged(nameof(Segments));
    }
  }
}
