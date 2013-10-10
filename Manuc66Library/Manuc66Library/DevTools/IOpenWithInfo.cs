using System;

namespace Be.Manuc66.Library.DevTools
{
  public interface IOpenWithInfo : IComparable<IOpenWithInfo>, IEquatable<IOpenWithInfo>
  {
    int TypeSortOrder { get; }
    string DisplayName { get; }
    bool OpenFileAtLine(FilePathWithLineInfo filePathWithLineInfo);
  }
}
