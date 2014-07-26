using System;
using Microsoft.Practices.Prism.Mvvm;
using Tail2Jump.Enums;

namespace Tail2Jump.Objects
{
  public class LogInstance : BindableBase
  {
    #region Members

    private string _text;
    private LogType _type;
    private Exception _exception;
    private DateTime _datetime;
    private string _displayedDatetime;

    #endregion // Members

    #region Properties
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

    #endregion // properties

    #region Constructor
    public LogInstance()
    {

    }

    public LogInstance(string text_, LogType type_)
    {
      _text = text_;
      _type = type_;
      _datetime = DateTime.Now;
    }
    #endregion // Constructor

    #region Methods
    #endregion // Methods

  }
}
