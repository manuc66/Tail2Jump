using System;
using Tail2Jump.Mvvm;
using Tail2Jump.Enums;

namespace Tail2Jump.Objects
{
  public class LogInstance : ObservableObject
  {
    private string? _text;
    private LogType _type;
    private Exception? _exception;
    private DateTime _datetime;
    private string? _displayedDatetime;

    public string? Text
    {
      get { return _text; }
      set { SetProperty(ref _text, value); }
    }

    public LogType Type
    {
      get { return _type; }
      set
      {
        if (SetProperty(ref _type, value))
        {
          OnPropertyChanged(nameof(IsValid));
          OnPropertyChanged(nameof(IsError));
          OnPropertyChanged(nameof(IsException));
          OnPropertyChanged(nameof(IsCritical));
        }
      }
    }

    public Exception? Exception
    {
      get { return _exception; }
      set
      {
        if (SetProperty(ref _exception, value))
        {
          OnPropertyChanged(nameof(HasException));
          OnPropertyChanged(nameof(ExceptionText));
        }
      }
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

    public string? DisplayedDatetime
    {
      get { return _displayedDatetime; }
      set { SetProperty(ref _displayedDatetime, value); }
    }

    public bool IsError => Type == LogType.Error;
    public bool IsCritical => Type == LogType.Critical;
    public bool IsException => Type == LogType.Exception;
    public bool IsValid => Type == LogType.Valid;

    public bool HasException => Exception != null;
    public string? ExceptionText => Exception?.ToString();

    public LogInstance()
    {
    }

    public LogInstance(string? text, LogType type)
    {
      _text = text;
      _type = type;
      Datetime = DateTime.Now;
    }
  }
}
