using System;
using Be.Manuc66.Library.Mvvm;
using Tail2Jump.Enums;

namespace Tail2Jump.Objects
{
  public class LogInstance : ObservableObject
  {
    private string _text;
    private LogType _type;
    private Exception _exception;
    private DateTime _datetime;
    private string _displayedDatetime;

    public string Text
    {
      get { return _text; }
      set { SetProperty(ref _text, value); }
    }

    public LogType Type
    {
      get { return _type; }
      set { SetProperty(ref _type, value); }
    }

    public Exception Exception
    {
      get { return _exception; }
      set { SetProperty(ref _exception, value); }
    }

    public DateTime Datetime
    {
      get { return _datetime; }
      set
      {
        SetProperty(ref _datetime, value);
        DisplayedDatetime = _datetime.ToLongTimeString();
      }
    }

    public string DisplayedDatetime
    {
      get { return _displayedDatetime; }
      set { SetProperty(ref _displayedDatetime, value); }
    }

    public bool IsError => Type == LogType.Error || Type == LogType.Critical;
    public bool IsException => Type == LogType.Exception;
    public bool IsValid => Type == LogType.Valid;

    public LogInstance()
    {
    }

    public LogInstance(string text, LogType type)
    {
      _text = text;
      _type = type;
      _datetime = DateTime.Now;
    }
  }
}
