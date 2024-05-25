using System;

using Avalonia.Controls;
using Avalonia.Controls.Templates;

using SiteMonitor.ViewModels;

namespace SiteMonitor;

/// <summary>
///   Pre-generated for us.
/// </summary>
public class ViewLocator : IDataTemplate {
  /// <summary>
  ///   Pre-generated for us.
  /// </summary>
  /// <param name="data">Couldn't tell you.</param>
  /// <returns>Stuff I guess.</returns>
  public Control? Build(object? data) {
    if (data is null) {
      return null;
    }

    string name = data.GetType().FullName!.Replace("ViewModel", "View", StringComparison.Ordinal);
    var type = Type.GetType(name);

    if (type != null) {
      var control = (Control)Activator.CreateInstance(type)!;
      control.DataContext = data;
      return control;
    }

    return new TextBlock { Text = "Not Found: " + name };
  }

  /// <summary>
  ///   Pre-generated for us.
  /// </summary>
  /// <param name="data">No idea</param>
  /// <returns>Stuff and things.</returns>
  public bool Match(object? data) {
    return data is ViewModelBase;
  }
}