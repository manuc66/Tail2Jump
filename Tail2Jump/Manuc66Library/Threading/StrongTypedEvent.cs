using System;
using System.Collections.Generic;
using System.Threading;
using log4net;

namespace Be.Manuc66.Library
{
  /// <summary>
  /// Gestion des évenements typés
  /// </summary>
  /// <typeparam name="T">Type du sender</typeparam>
  /// <typeparam name="U">Type du paramètre de l'évènement</typeparam>
  public class StrongTypedEvent<T, U>
  {
    private class InternalDataTypedEvent
    {
      public readonly T Sender;
      public readonly U Data;

      public InternalDataTypedEvent(T sender_, U data_)
      {
        Data = data_;
        Sender = sender_;
      }
    }

    #region Events
    public delegate void EventEventHandler(T sender_, U data_);
    private readonly List<EventEventHandler> _registered = new List<EventEventHandler>();
    public event EventEventHandler eEvent
    {
      add
      {
        lock (_lookObject)
        {
          // to prevent an event handler hooked twice
          // http://stackoverflow.com/questions/937181/c-pattern-to-prevent-an-event-handler-hooked-twice
          if (!_registered.Contains(value))
          {
            _registered.Add(value);
          }
        }
      }
      remove
      {
        lock (_lookObject)
        {
          _registered.Remove(value);
        }
      }
    }
    #endregion // Events

    #region Methods
    public bool IsSubscriber { get { return _registered.Count > 0; } }
    #endregion // Methods

    #region Members Variables

    readonly ILog _log = LogManager.GetLogger(typeof(StrongTypedEvent<T, U>));
    #region Private Members Variables
    private readonly bool _threated;
    private readonly object _lookObject = new object();

    #endregion // Private Members Variables
    #endregion // Members Variables

    #region Constructor
    public StrongTypedEvent(bool threated_)
    {
      _threated = threated_;
    }
    #endregion // Constructor

    #region Methods
    public bool IsSomeoneListening()
    {
      lock (_lookObject)
      {
        return _registered.Count > 0;
      }
    }
    public void RaiseEvent(T sender_, U data_)
    {
      if (_threated)
      {
        ThreadPool.QueueUserWorkItem(PrivateRaiseEvent, new InternalDataTypedEvent(sender_, data_));
      }
      else
      {
        PrivateRaiseEvent(new InternalDataTypedEvent(sender_, data_));
      }
    }
    private void PrivateRaiseEvent(object data_)
    {
      var internalStruct = data_ as InternalDataTypedEvent;

      if (internalStruct != null)
      {
        if (_registered.Count > 0)
        {
          List<EventEventHandler> delegates;
          lock (_lookObject)
          {
            delegates = new List<EventEventHandler>(_registered);
          }

          foreach (EventEventHandler oneDelegate in delegates)
          {
            try
            {
              oneDelegate(internalStruct.Sender, internalStruct.Data);
            }
            catch (Exception error)
            {
              _log.Error(error);
            }
          }
        }
      }
    }
    #endregion // Methods
  }
}