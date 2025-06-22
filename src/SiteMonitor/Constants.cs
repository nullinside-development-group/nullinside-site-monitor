using System.Reflection;

namespace SiteMonitor;

/// <summary>
///   Constants used throughout the file.
/// </summary>
public class Constants {
  /// <summary>
  ///   The version of the application being run right now.
  /// </summary>
  public static readonly string? APP_VERSION = Assembly.GetEntryAssembly()?.GetName().Version?.ToString()[..^2];
}