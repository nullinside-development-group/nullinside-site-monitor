using System;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

using Avalonia.Controls;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Renci.SshNet;

using SiteMonitor.Models;

namespace SiteMonitor.ViewModels;

/// <summary>
///   The view model for the main UI.
/// </summary>
public partial class MainWindowViewModel : ViewModelBase {
  private DateTime? _apiDownSince;
  [ObservableProperty] private bool _apiUp = true;

  [ObservableProperty] private string? _chatTimestamp;

  [ObservableProperty] private bool _isDisplayingAdvancedCommands;

  [ObservableProperty] private bool _isMinimized;

  [ObservableProperty] private bool _nullUp = true;

  private string? _serverAddress;

  [ObservableProperty] private bool _serverUp = true;

  private string? _sshPassword;
  private string? _sshUsername;

  [ObservableProperty] private bool _websiteUp = true;

  private WindowState _windowState;

  /// <summary>
  ///   Initializes a new instance of the <see cref="MainWindowViewModel" /> class.
  /// </summary>
  public MainWindowViewModel() {
    Task.Factory.StartNew(PingServer);
    Task.Factory.StartNew(PingSite);
    ServerAddress = Configuration.Instance.ServerAddress;
    SshUsername = Configuration.Instance.ServerUsername;
    SshPassword = Configuration.Instance.ServerPassword;
  }

  /// <summary>
  ///   The address of the server.
  /// </summary>
  public string? ServerAddress {
    get => _serverAddress;
    set {
      SetProperty(ref _serverAddress, value);

      try {
        Configuration.Instance.ServerAddress = value;
        Configuration.WriteConfiguration();
      }
      catch { }
    }
  }

  /// <summary>
  ///   Sets the window to normal, minimized, maximized, etc.
  /// </summary>
  public WindowState WindowState {
    get => _windowState;
    set {
      SetProperty(ref _windowState, value);
      IsMinimized = _windowState == WindowState.Minimized;
    }
  }

  /// <summary>
  ///   The username to use for the SSH session for commands.
  /// </summary>
  public string? SshUsername {
    get => _sshUsername;
    set {
      Configuration.Instance.ServerUsername = value;
      SetProperty(ref _sshUsername, value);
      try {
        Configuration.WriteConfiguration();
      }
      catch { }
    }
  }

  /// <summary>
  ///   The password to use for the SSH session for commands.
  /// </summary>
  public string? SshPassword {
    get => _sshPassword;
    set {
      Configuration.Instance.ServerPassword = value;
      SetProperty(ref _sshPassword, value);
      try {
        Configuration.WriteConfiguration();
      }
      catch { }
    }
  }

  /// <summary>
  ///   Restarts the remote machine.
  /// </summary>
  [RelayCommand]
  private async Task OnRestart() {
    using SshClient client = new(_serverAddress!, _sshUsername!, _sshPassword!);
    await client.ConnectAsync(CancellationToken.None).ConfigureAwait(false);
    string command = "shutdown -r now";
    using SshCommand? ssh = client.RunCommand($"echo {_sshPassword} | sudo -S {command}");
  }

  /// <summary>
  ///   Restarts the docker images.
  /// </summary>
  [RelayCommand]
  private async Task OnRestartImages() {
    await Task.Run(async () => {
      using SshClient client = new(_serverAddress!, _sshUsername!, _sshPassword!);
      await client.ConnectAsync(CancellationToken.None).ConfigureAwait(false);
      string[] command = [
        "docker compose -p nullinside-ui restart",
        "docker compose -p nullinside-api restart",
        "docker compose -p nullinside-api-null restart",
        "docker compose -p nullinside-api-twitch-bot restart"
      ];

      foreach (string line in command) {
        using SshCommand? ssh = client.RunCommand($"echo {_sshPassword} | sudo -S {line}");
      }
    }).ConfigureAwait(false);
  }

  /// <summary>
  ///   Handles showing the server commands.
  /// </summary>
  [RelayCommand]
  private void OnShowCommands() {
    IsDisplayingAdvancedCommands = true;
  }

  /// <summary>
  ///   Pings the web resources.
  /// </summary>
  private async Task PingSite() {
    while (true) {
      WebsiteUp = await SendHeadRequest("https://nullinside.com").ConfigureAwait(false);
      ApiUp = await SendHeadRequest("https://nullinside.com/api/v1/featureToggle").ConfigureAwait(false);
      NullUp = await SendHeadRequest("https://nullinside.com/null/v1/database/migration").ConfigureAwait(false);
      (HttpStatusCode, string?) chat = await SendGetRequest("https://nullinside.com/twitch-bot/v1/bot/chat/timestamp").ConfigureAwait(false);
      bool chatNotUpdating = false;
      if (HttpStatusCode.OK == chat.Item1 && null != chat.Item2) {
        ChatTimestamp = chat.Item2;
        string parsed = ChatTimestamp.Trim('"');
        if (DateTime.TryParse(parsed, out DateTime time)) {
          string timestamp = time.ToLocalTime().ToString(CultureInfo.InvariantCulture);
          TimeSpan diff = DateTime.Now - time.ToLocalTime();
          timestamp = $"{timestamp} ({diff.Hours}h {diff.Minutes}m {diff.Seconds}s ago)";
          ChatTimestamp = timestamp;
          chatNotUpdating = diff > Constants.MAX_TIME_WITHOUT_CHATS;
        }
      }
      else {
        ChatTimestamp = null;
        chatNotUpdating = true;
      }

      _apiDownSince = !WebsiteUp || !ApiUp || !NullUp ? _apiDownSince ?? DateTime.Now : null;
      bool apiNotComingUp = DateTime.Now - _apiDownSince > Constants.MAX_TIME_SERVICE_IS_DOWN;
      if ((apiNotComingUp || chatNotUpdating) && IsMinimized) {
        WindowState = WindowState.Normal;
      }

      await Task.Delay(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
    }
  }

  /// <summary>
  ///   Sends a head request to ensure a URL is online.
  /// </summary>
  /// <param name="address">The address to send the request to.</param>
  /// <returns>True if successful, false otherwise.</returns>
  private async Task<bool> SendHeadRequest(string address) {
    try {
      var handler = new HttpClientHandler();
      handler.AutomaticDecompression = ~DecompressionMethods.None;
      using var httpClient = new HttpClient(handler);
      using var request = new HttpRequestMessage(HttpMethod.Get, address);
      request.Headers.TryAddWithoutValidation("user-agent", Nullinside.Api.Common.Constants.FAKE_USER_AGENT);
      HttpResponseMessage response = await httpClient.SendAsync(request).ConfigureAwait(false);
      return response.IsSuccessStatusCode;
    }
    catch {
      return false;
    }
  }

  /// <summary>
  ///   Sends a head request to ensure a URL is online.
  /// </summary>
  /// <param name="address">The address to send the request to.</param>
  /// <returns>The content of the response.</returns>
  private async Task<(HttpStatusCode, string?)> SendGetRequest(string address) {
    try {
      var handler = new HttpClientHandler();
      handler.AutomaticDecompression = ~DecompressionMethods.None;
      using var httpClient = new HttpClient(handler);
      using var request = new HttpRequestMessage(HttpMethod.Get, address);
      request.Headers.TryAddWithoutValidation("user-agent", Nullinside.Api.Common.Constants.FAKE_USER_AGENT);
      HttpResponseMessage response = await httpClient.SendAsync(request).ConfigureAwait(false);
      return (response.StatusCode, await response.Content.ReadAsStringAsync().ConfigureAwait(false));
    }
    catch {
      return (HttpStatusCode.InternalServerError, null);
    }
  }

  private async Task PingServer() {
    while (true) {
      try {
        if (string.IsNullOrWhiteSpace(ServerAddress)) {
          continue;
        }

        using var pingSender = new Ping();
        PingReply reply = pingSender.Send(ServerAddress);
        ServerUp = reply.Status == IPStatus.Success;
      }
      catch { }
      finally {
        await Task.Delay(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
      }
    }
  }
}