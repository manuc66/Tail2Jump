
using System;

namespace Be.Manuc66.Library.General
{
  public interface IEmpty : IEquatable<IEmpty>
  {
  }

  /// <summary>
  /// Specifies a return value type for a method that does not return a value (or no argument)
  /// (similary to System.Void but this can't be used in C#).
  /// </summary>
  public sealed class Empty : IEmpty, IEquatable<Empty>
  {
    public static readonly Empty Instance = new Empty();
    private Empty() { }

    public bool Equals(IEmpty other_)
    {
      return true;
    }
    public bool Equals(Empty other_)
    {
      return true;
    }
  }
}
