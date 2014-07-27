using System;

namespace PartialView.TextAppenders
{
  internal class TextMatcher
  {
    private readonly string _textSearch;
    private readonly StringComparison _comparisonType;

    public TextMatcher(string textSearch, StringComparison comparisonType)
    {
      _comparisonType = comparisonType;
      _textSearch = textSearch;
    }

    public int PatternLenght
    {
      get { return _textSearch.Length; }
    }

    public bool Match(string part)
    {
      return part.Equals(_textSearch, _comparisonType);
    }

    public int GetFirstMatchStartIndex(string all)
    {
      return all.IndexOf(_textSearch.ToLower(), _comparisonType);
    }
  }
}