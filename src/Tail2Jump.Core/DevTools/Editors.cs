using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Tail2Jump.DevTools
{
  public class FilePathWithLineInfo
  {
    public string FilePath { get; set; } = string.Empty;
    public int LineNumber { get; set; }
  }

  public interface IOpenWithInfo : IComparable<IOpenWithInfo>, IEquatable<IOpenWithInfo>
  {
    int TypeSortOrder { get; }
    string DisplayName { get; }
    bool IsAvailable { get; }
    bool OpenFileAtLine(FilePathWithLineInfo filePathWithLineInfo);
  }

  /// <summary>
  /// Parses a path with line information, e.g. "/src/File.cs:42" or "C:\src\File.cs(42,7)".
  /// </summary>
  public static class FileLocationParser
  {
    public static FilePathWithLineInfo? Parse(string originalString)
    {
      if (string.IsNullOrWhiteSpace(originalString)) return null;

      originalString = NormalizeLineKeyword(originalString.Trim());

      if (TryParseDotNetStyle(originalString, out var dotNetResult)) return dotNetResult;
      if (TryParseColonStyle(originalString, out var colonResult)) return colonResult;

      return null;
    }

    private static string NormalizeLineKeyword(string text)
    {
      // "File.cs:line 12" / "File.cs:ligne 12" -> "File.cs:12"
      var match = System.Text.RegularExpressions.Regex.Match(
        text, @":(?:line|ligne)\s+(\d+)$",
        System.Text.RegularExpressions.RegexOptions.IgnoreCase);

      return match.Success
        ? text.Substring(0, match.Index) + ":" + match.Groups[1].Value
        : text;
    }

    private static bool TryParseDotNetStyle(string text, out FilePathWithLineInfo? result)
    {
      result = null;

      int parenthesis = text.LastIndexOf('(');
      int comma = text.LastIndexOf(',');
      int closing = text.LastIndexOf(')');
      if (parenthesis <= 0 || comma <= parenthesis || closing <= comma) return false;

      var lineText = text.Substring(parenthesis + 1, comma - parenthesis - 1);
      if (!int.TryParse(lineText, out int lineNumber) || lineNumber <= 0) return false;

      var path = text.Substring(0, parenthesis);
      if (!File.Exists(path)) return false;

      result = new FilePathWithLineInfo { FilePath = path, LineNumber = lineNumber };
      return true;
    }

    private static bool TryParseColonStyle(string text, out FilePathWithLineInfo? result)
    {
      result = null;

      int index = text.LastIndexOf(':');
      if (index <= 0 || index >= text.Length - 1) return false;

      var lineText = text.Substring(index + 1);
      if (!int.TryParse(lineText, out int lineNumber) || lineNumber <= 0) return false;

      var path = text.Substring(0, index);
      if (!File.Exists(path)) return false;

      result = new FilePathWithLineInfo { FilePath = path, LineNumber = lineNumber };
      return true;
    }
  }

  /// <summary>
  /// Opens a file at a given line using an external editor.
  /// </summary>
  public class EditorOpenWith : IOpenWithInfo
  {
    private readonly string _executable;
    private readonly string _argumentsTemplate;
    private readonly int _typeSortOrder;

    public EditorOpenWith(string displayName, string executable, string argumentsTemplate, int typeSortOrder = 1)
    {
      DisplayName = displayName;
      _executable = executable;
      _argumentsTemplate = argumentsTemplate;
      _typeSortOrder = typeSortOrder;
    }

    public string DisplayName { get; }

    public int TypeSortOrder => _typeSortOrder;

    public bool IsAvailable => ExecutableLocator.Find(_executable) != null;

    public bool OpenFileAtLine(FilePathWithLineInfo filePathWithLineInfo)
    {
      if (filePathWithLineInfo == null) return false;

      var executablePath = ExecutableLocator.Find(_executable);
      if (executablePath == null) return false;

      var arguments = _argumentsTemplate
        .Replace("{file}", filePathWithLineInfo.FilePath)
        .Replace("{line}", filePathWithLineInfo.LineNumber.ToString());

      try
      {
        Process.Start(new ProcessStartInfo(executablePath, arguments)
        {
          UseShellExecute = false
        });
        return true;
      }
      catch (Exception)
      {
        return false;
      }
    }

    public int CompareTo(IOpenWithInfo? other)
    {
      if (other == null) return -1;

      int result = TypeSortOrder.CompareTo(other.TypeSortOrder);
      if (result != 0) return result;
      return string.Compare(DisplayName, other.DisplayName, StringComparison.Ordinal);
    }

    public bool Equals(IOpenWithInfo? other)
    {
      return other != null && TypeSortOrder == other.TypeSortOrder &&
             string.Equals(DisplayName, other.DisplayName, StringComparison.Ordinal);
    }

    public override bool Equals(object? obj)
    {
      return Equals(obj as IOpenWithInfo);
    }

    public override int GetHashCode()
    {
      return DisplayName?.GetHashCode() ?? 0;
    }
  }

  /// <summary>
  /// Opens the file with the desktop default application (no line navigation).
  /// </summary>
  public class SystemDefaultOpenWith : IOpenWithInfo
  {
    public string DisplayName => "System default application";

    public int TypeSortOrder => 99;

    public bool IsAvailable => true;

    public bool OpenFileAtLine(FilePathWithLineInfo filePathWithLineInfo)
    {
      if (filePathWithLineInfo == null) return false;

      string command;
      if (OperatingSystem.IsWindows()) command = "explorer.exe";
      else if (OperatingSystem.IsMacOS()) command = "open";
      else command = "xdg-open";

      try
      {
        Process.Start(new ProcessStartInfo(command, $"\"{filePathWithLineInfo.FilePath}\"")
        {
          UseShellExecute = false
        });
        return true;
      }
      catch (Exception)
      {
        return false;
      }
    }

    public int CompareTo(IOpenWithInfo? other)
    {
      if (other == null) return -1;
      return TypeSortOrder.CompareTo(other.TypeSortOrder);
    }

    public bool Equals(IOpenWithInfo? other) => other is SystemDefaultOpenWith;

    public override bool Equals(object? obj) => obj is SystemDefaultOpenWith;

    public override int GetHashCode() => typeof(SystemDefaultOpenWith).GetHashCode();
  }

  public static class ExecutableLocator
  {
    public static string? Find(string executable)
    {
      if (string.IsNullOrWhiteSpace(executable)) return null;

      if (Path.IsPathRooted(executable))
      {
        return File.Exists(executable) ? executable : null;
      }

      var path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
      foreach (var directory in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
      {
        foreach (var candidate in Candidates(directory, executable))
        {
          if (File.Exists(candidate)) return candidate;
        }
      }

      return null;
    }

    private static IEnumerable<string> Candidates(string directory, string executable)
    {
      yield return Path.Combine(directory, executable);

      if (OperatingSystem.IsWindows())
      {
        yield return Path.Combine(directory, executable + ".exe");
        yield return Path.Combine(directory, executable + ".cmd");
        yield return Path.Combine(directory, executable + ".bat");
      }
    }
  }

  /// <summary>
  /// Detects the editors available on the current machine.
  /// Replaces the Visual Studio COM automation used on Windows.
  /// </summary>
  public class EditorTools
  {
    private static readonly (string DisplayName, string Executable, string Arguments)[] KnownEditors =
    {
      ("Visual Studio Code", "code", "--goto \"{file}:{line}\""),
      ("Visual Studio Code Insiders", "code-insiders", "--goto \"{file}:{line}\""),
      ("Cursor", "cursor", "--goto \"{file}:{line}\""),
      ("JetBrains Rider", "rider", "--line {line} \"{file}\""),
      ("Sublime Text", "subl", "\"{file}:{line}\""),
      ("Notepad++", "notepad++", "-n{line} \"{file}\""),
    };

    public FilePathWithLineInfo? GetFilePathWithLineNumber(string originalString)
    {
      return FileLocationParser.Parse(originalString);
    }

    public IReadOnlyList<IOpenWithInfo> DetectEditors()
    {
      var editors = new List<IOpenWithInfo>();

      foreach (var (displayName, executable, arguments) in KnownEditors)
      {
        var editor = new EditorOpenWith(displayName, executable, arguments);
        if (editor.IsAvailable)
        {
          editors.Add(editor);
        }
      }

      editors.Add(new SystemDefaultOpenWith());
      editors.Sort();
      return editors;
    }

    public void RefreshEditors(IList<IOpenWithInfo> editors, ref IOpenWithInfo? openIn)
    {
      if (editors == null) throw new ArgumentNullException(nameof(editors));

      var detected = DetectEditors();

      editors.Clear();
      foreach (var editor in detected)
      {
        editors.Add(editor);
      }

      if (openIn == null || !editors.Contains(openIn))
      {
        openIn = editors.FirstOrDefault();
      }
    }
  }
}
