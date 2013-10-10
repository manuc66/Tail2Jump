using System;
using System.Reflection;

namespace Be.Manuc66.Library.General
{
  public class WeakEvent<T, U>
  {
    WeakReference Target { get; set; }
    MethodInfo Method { get; set; }
    public WeakEvent(StrongTypedEvent<T, U>.EventEventHandler handler_)
    {
      if (handler_ == null)
      {
        throw new ArgumentNullException("handler_");
      }
      Target = new WeakReference(handler_.Target);
      Method = handler_.Method;
    }

    public void Invoke(T sender_, U data_)
    {
      object target = Target.Target;
      if (target != null)
      {
        Delegate realDelegate = Delegate.CreateDelegate(typeof(StrongTypedEvent<T, U>.EventEventHandler), target, Method);
        var eventHandler = (StrongTypedEvent<T, U>.EventEventHandler)realDelegate;
        eventHandler(sender_, data_);
      }
    }

    public bool Equals(StrongTypedEvent<T, U>.EventEventHandler handler_)
    {
      return handler_ != null && ReferenceEquals(Target, handler_.Target) && Method.Equals(handler_.Method);
    }
  }
}
