using System;
using System.Linq;
using System.Threading.Tasks;

using Avalonia.Controls;
using Avalonia.Interactivity;

using Microsoft.Extensions.DependencyInjection;

using Nullinside.Api.Common.Desktop;
#if !DEBUG
using Avalonia.Threading;

using SiteMonitor.ViewModels;

#else
using Avalonia;
#endif


namespace SiteMonitor.Views;

/// <summary>
///   The main application window.
/// </summary>
public partial class MainWindow : Window {
  /// <summary>
  ///   Initializes a new instance of the <see cref="MainWindow" /> class.
  /// </summary>
  public MainWindow() {
    InitializeComponent();
#if DEBUG
    this.AttachDevTools();
#endif
  }

  /// <summary>
  ///   The service provider for DI.
  /// </summary>
  public IServiceProvider? ServiceProvider { get; set; }

  /// <summary>
  ///   Checks for a new version number of the application.
  /// </summary>
  protected override void OnInitialized() {
    base.OnInitialized();

    // Register all the services needed for the application to run
    var collection = new ServiceCollection();
    collection.AddCommonServices();
    ServiceProvider = collection.BuildServiceProvider();

    // handle the command line arguments for updating the application if applicable.
    string[] args = Environment.GetCommandLineArgs();
    if (args.Contains("--update")) {
      _ = GitHubUpdateManager.PerformUpdateAndRestart("nullinside-development-group", "nullinside-site-monitor", args[2], "windows-x64.zip");
      return;
    }

    if (args.Contains("--justUpdated")) {
      _ = GitHubUpdateManager.CleanupUpdate();
    }

    // check for a new version of the application.
    Task.Factory.StartNew(async () => {
      GithubLatestReleaseJson? serverVersion =
        await GitHubUpdateManager.GetLatestVersion("nullinside-development-group", "nullinside-site-monitor").ConfigureAwait(false);
      if (null == serverVersion || string.IsNullOrWhiteSpace(serverVersion.name)) {
        return;
      }

      if (serverVersion.name?.Equals(Constants.APP_VERSION, StringComparison.InvariantCultureIgnoreCase) ?? true) {
// Had to add this because code clean up tools were removing the "redundant" return statement.
// which was causing the check to always be ignored.
#if !DEBUG
        return;
#endif
      }

#if !DEBUG
      var vm = ServiceProvider?.GetRequiredService<NewVersionWindowViewModel>();
      if (null == vm) {
        return;
      }

      vm.LocalVersion = Constants.APP_VERSION;
      Dispatcher.UIThread.Post(async void () => {
        try {
          var versionWindow = new NewVersionWindow {
            DataContext = vm
          };

          await versionWindow.ShowDialog(this).ConfigureAwait(false);
        }
        catch {
          // do nothing, don't crash
        }
      });
#endif
    });
  }

  private void HideWindowOnLoaded(object? sender, RoutedEventArgs e) {
    var window = (Window)sender!;
    window.WindowState = WindowState.Minimized;
  }
}