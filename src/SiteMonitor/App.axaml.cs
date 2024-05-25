using System;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

using SiteMonitor.ViewModels;
using SiteMonitor.Views;

namespace SiteMonitor;

/// <summary>
///   Main entry point of the application.
/// </summary>
public class App : Application {
  /// <summary>
  ///   Initializes the GUI.
  /// </summary>
  public override void Initialize() {
    AvaloniaXamlLoader.Load(this);
  }

  /// <summary>
  ///   Launches the main application window.
  /// </summary>
  public override void OnFrameworkInitializationCompleted() {
    if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
      desktop.MainWindow = new MainWindow {
        DataContext = new MainWindowViewModel()
      };
    }

    base.OnFrameworkInitializationCompleted();
  }

  private void ShowSettings_OnClick(object? sender, EventArgs e) {
    if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
      if (null == desktop.MainWindow) {
        return;
      }

      desktop.MainWindow.WindowState = WindowState.Normal;
    }
  }

  private void Close_OnClick(object? sender, EventArgs e) {
    Environment.Exit(0);
  }
}