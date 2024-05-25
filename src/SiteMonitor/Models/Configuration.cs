using System.IO;
using System.Reflection;

using Newtonsoft.Json;

namespace SiteMonitor.Models;

/// <summary>
///   The configuration of the application.
/// </summary>
public class Configuration {
  private static readonly string s_configLocation =
    Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) ?? string.Empty, "config.json");

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
      string json = File.ReadAllText(s_configLocation);
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
      File.WriteAllText(s_configLocation, json);
      return true;
    }
    catch {
      return false;
    }
  }
}