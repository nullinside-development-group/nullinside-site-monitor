using System;
using System.IO;

using Newtonsoft.Json;

namespace SiteMonitor.Models;

/// <summary>
///   The configuration of the application.
/// </summary>
public class Configuration {
  private static readonly string S_CONFIG_LOCATION =
    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "nullinside",
      "site-monitor", "config.json");

  private static Configuration? s_instance;

  /// <summary>
  ///   The server address.
  /// </summary>
  public string? ServerAddress { get; set; }

  /// <summary>
  ///   The singleton instance of the class.
  /// </summary>
  public static Configuration Instance {
    get {
      if (null == s_instance) {
        s_instance = ReadConfiguration() ?? new Configuration();
      }

      return s_instance;
    }
  }

  private static Configuration? ReadConfiguration() {
    try {
      string json = File.ReadAllText(S_CONFIG_LOCATION);
      return JsonConvert.DeserializeObject<Configuration>(json);
    }
    catch { return null; }
  }

  /// <summary>
  ///   Writes the configuration file to disk.
  /// </summary>
  /// <returns>True if successful, false otherwise.</returns>
  public static bool WriteConfiguration() {
    try {
      string json = JsonConvert.SerializeObject(Instance);
      File.WriteAllText(S_CONFIG_LOCATION, json);
      return true;
    }
    catch {
      return false;
    }
  }
}