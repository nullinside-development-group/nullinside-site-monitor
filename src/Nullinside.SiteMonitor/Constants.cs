using System;
using System.Reflection;

namespace Nullinside.SiteMonitor;

/// <summary>
///   Constants used throughout the file.
/// </summary>
public class Constants {
  /// <summary>
  ///   The maximum amount of time before alerting the user that we aren't getting chat messages.
  /// </summary>
  public static readonly TimeSpan MAX_TIME_WITHOUT_CHATS = TimeSpan.FromMinutes(30);

  /// <summary>
  ///   The maximum amount of time before alerting the user that the APIs are down and not automatically coming back up.
  /// </summary>
  public static readonly TimeSpan MAX_TIME_SERVICE_IS_DOWN = TimeSpan.FromMinutes(5);

  /// <summary>
  ///   The version of the application being run right now.
  /// </summary>
  public static readonly string? APP_VERSION = Assembly.GetEntryAssembly()?.GetName().Version?.ToString()[..^2];
}