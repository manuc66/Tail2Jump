using System;
using System.Collections.Generic;

namespace Be.Manuc66.Library.Events
{
  /// <summary>
  /// Lightweight, Prism-compatible event aggregator.
  /// </summary>
  public interface IEventAggregator
  {
    TEvent GetEvent<TEvent>() where TEvent : EventBase, new();
  }

  public class EventAggregator : IEventAggregator
  {
    private readonly object _lock = new object();
    private readonly Dictionary<Type, EventBase> _events = new Dictionary<Type, EventBase>();

    public TEvent GetEvent<TEvent>() where TEvent : EventBase, new()
    {
      lock (_lock)
      {
        if (_events.TryGetValue(typeof(TEvent), out EventBase existing))
        {
          return (TEvent)existing;
        }

        var created = new TEvent();
        _events[typeof(TEvent)] = created;
        return created;
      }
    }
  }

  /// <summary>
  /// Marker base class for events resolved through <see cref="IEventAggregator"/>.
  /// </summary>
  public abstract class EventBase
  {
  }

  /// <summary>
  /// Simple publish/subscribe event. Handlers are invoked synchronously on the
  /// publishing thread; UI code is responsible for dispatching to the UI thread.
  /// </summary>
  public class PubSubEvent<TPayload> : EventBase
  {
    private readonly object _lock = new object();
    private readonly List<Action<TPayload>> _subscriptions = new List<Action<TPayload>>();

    public void Subscribe(Action<TPayload> action)
    {
      if (action == null) throw new ArgumentNullException(nameof(action));

      lock (_lock)
      {
        _subscriptions.Add(action);
      }
    }

    public void Unsubscribe(Action<TPayload> action)
    {
      if (action == null) return;

      lock (_lock)
      {
        _subscriptions.Remove(action);
      }
    }

    public void Publish(TPayload payload)
    {
      Action<TPayload>[] snapshot;
      lock (_lock)
      {
        snapshot = _subscriptions.ToArray();
      }

      foreach (var subscription in snapshot)
      {
        subscription(payload);
      }
    }
  }
}
