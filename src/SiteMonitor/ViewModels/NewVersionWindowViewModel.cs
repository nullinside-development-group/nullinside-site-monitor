using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Input;

using Avalonia.Controls;
using Avalonia.Threading;

using Nullinside.Api.Common.Desktop;

using ReactiveUI;

using SiteMonitor.Views;

namespace SiteMonitor.ViewModels;

/// <summary>
///   The view model for the <seealso cref="NewVersionWindow" /> class.
/// </summary>
public class NewVersionWindowViewModel : ViewModelBase {
  /// <summary>
  ///   True if updating the application currently, false otherwise.
  /// </summary>
  private bool _isUpdating;

  /// <summary>
  ///   The local version of the software.
  /// </summary>
  private string? _localVersion;

  /// <summary>
  ///   The url for the new version's assets on GitHub.
  /// </summary>
  private string? _newVersionUrl;

  /// <summary>
  ///   The version of the application on the GitHub server.
  /// </summary>
  private string? _serverVersion;

  /// <summary>
  ///   Initializes a new instance of the <see cref="NewVersionWindowViewModel" /> class.
  /// </summary>
  public NewVersionWindowViewModel() {
    UpdateSoftware = ReactiveCommand.Create(StartUpdateSoftware);
    CloseWindow = ReactiveCommand.Create<Window>(CloseWindowCommand);

    // asynchronously determine the current version number.
    Task.Factory.StartNew(async () => {
      GithubLatestReleaseJson? version =
        await GitHubUpdateManager.GetLatestVersion("nullinside-development-group", "nullinside-site-monitor");

      if (null == version) {
        return;
      }

      _newVersionUrl = version.html_url;
      Dispatcher.UIThread.Post(() => ServerVersion = version.name ?? string.Empty);
    });
  }

  /// <summary>
  ///   The local version of the software.
  /// </summary>
  public string? LocalVersion {
    get => _localVersion;
    set => _localVersion = value;
  }

  /// <summary>
  ///   The version of the software on the GitHub server.
  /// </summary>
  public string? ServerVersion {
    get => _serverVersion;
    set => this.RaiseAndSetIfChanged(ref _serverVersion, value);
  }

  /// <summary>
  ///   True if updating the application currently, false otherwise.
  /// </summary>
  public bool IsUpdating {
    get => _isUpdating;
    set => this.RaiseAndSetIfChanged(ref _isUpdating, value);
  }

  /// <summary>
  ///   A command to update the software.
  /// </summary>
  public ICommand UpdateSoftware { get; }

  /// <summary>
  ///   A command to close the current window.
  /// </summary>
  public ICommand CloseWindow { get; }

  /// <summary>
  ///   A command to close the current window.
  /// </summary>
  /// <param name="self">The reference to our own window.</param>
  private void CloseWindowCommand(Window self) {
    self.Close();
  }

  /// <summary>
  ///   Launches the web browser at the new release page.
  /// </summary>
  private void StartUpdateSoftware() {
    IsUpdating = true;
    GitHubUpdateManager.PrepareUpdate()
      .ContinueWith(_ => {
        if (string.IsNullOrWhiteSpace(_newVersionUrl)) {
          return;
        }

        Process.Start("explorer", _newVersionUrl);
        GitHubUpdateManager.ExitApplicationToUpdate();
      }).ConfigureAwait(false);
  }
}