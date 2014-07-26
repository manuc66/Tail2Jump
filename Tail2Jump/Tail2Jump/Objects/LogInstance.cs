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
    #endregion // Members

    #region Properties
    public string Text
    {
      get { return _text; }
      set
      {
        if (_text != value)
        {
          _text = value;
          OnPropertyChanged("Text");
        }
      }
    }
    public LogType Type
    {
      get { return _type; }
      set
      {
        if (_type != value)
        {
          _type = value;
          OnPropertyChanged("Exception");
        }
      }
    }
    public Exception Exception
    {
      get { return _exception; }
      set
      {
        if (_exception != value)
        {
          _exception = value;
          OnPropertyChanged("Exception");
        }
      }
    }
    public DateTime Datetime
    {
      get { return _datetime; }
      set
      {
        if (_datetime != value)
        {
          _datetime = value;
          OnPropertyChanged("Datetime");
          OnPropertyChanged("DisplayedDatetime");
        }
      }
    }
    public string DisplayedDatetime
    {
      get { return _datetime.ToLongTimeString(); }
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
