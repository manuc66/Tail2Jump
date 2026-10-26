using System;

namespace Tail2Jump.ViewModels
{
  public class NewWatcherInput
  {
    public string Path { get; set; } = string.Empty;
    public string Filter { get; set; } = "*.log";
    public bool IncludeSubdirectories { get; set; }
  }
}
