using System;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

using Avalonia.Controls;

using ReactiveUI;

using Renci.SshNet;

using SiteMonitor.Models;

namespace SiteMonitor.ViewModels;

/// <summary>
///   The view model for the main UI.
/// </summary>
public class MainWindowViewModel : ViewModelBase {
  private bool _apiUp = true;
  private string? _chatTimestamp;
  private bool _isDisplayingAdvancedCommands;
  private bool _isMinimized;
  private bool _nullUp = true;
  private string? _serverAddress;
  private bool _serverUp = true;
  private string? _sshPassword;
  private string? _sshUsername;
  private bool _websiteUp = true;
  private WindowState _windowState;

  /// <summary>
  ///   Initializes a new instance of the <see cref="MainWindowViewModel" /> class.
  /// </summary>
  public MainWindowViewModel() {
    OnShowCommandsCommand = ReactiveCommand.Create(OnShowCommands);
    OnRestartCommand = ReactiveCommand.Create(OnRestart);
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
      this.RaiseAndSetIfChanged(ref _serverAddress, value);

      try {
        Configuration.Instance.ServerAddress = value;
        Configuration.WriteConfiguration();
      }
      catch { }
    }
  }

  /// <summary>
  ///   The timestamp of the last chat message that was received.
  /// </summary>
  public string? ChatTimestamp {
    get => _chatTimestamp;
    set => this.RaiseAndSetIfChanged(ref _chatTimestamp, value);
  }

  /// <summary>
  ///   True if the server is online.
  /// </summary>
  public bool ServerUp {
    get => _serverUp;
    set => this.RaiseAndSetIfChanged(ref _serverUp, value);
  }

  /// <summary>
  ///   True if the website is returning a 200.
  /// </summary>
  public bool WebsiteUp {
    get => _websiteUp;
    set => this.RaiseAndSetIfChanged(ref _websiteUp, value);
  }

  /// <summary>
  ///   True if the API is online.
  /// </summary>
  public bool ApiUp {
    get => _apiUp;
    set => this.RaiseAndSetIfChanged(ref _apiUp, value);
  }

  /// <summary>
  ///   True if the null API is online.
  /// </summary>
  public bool NullUp {
    get => _nullUp;
    set => this.RaiseAndSetIfChanged(ref _nullUp, value);
  }

  /// <summary>
  ///   Sets the window to normal, minimized, maximized, etc.
  /// </summary>
  public WindowState WindowState {
    get => _windowState;
    set {
      this.RaiseAndSetIfChanged(ref _windowState, value);
      IsMinimized = _windowState == WindowState.Minimized;
    }
  }

  /// <summary>
  ///   True if the application is minimized, false otherwise.
  /// </summary>
  public bool IsMinimized {
    get => _isMinimized;
    set => this.RaiseAndSetIfChanged(ref _isMinimized, value);
  }

  /// <summary>
  ///   Shows the commands in the UI.
  /// </summary>
  public ICommand OnShowCommandsCommand { get; set; }

  /// <summary>
  ///   True if displaying advanced commands, false otherwise.
  /// </summary>
  public bool IsDisplayingAdvancedCommands {
    get => _isDisplayingAdvancedCommands;
    set => this.RaiseAndSetIfChanged(ref _isDisplayingAdvancedCommands, value);
  }

  /// <summary>
  ///   The username to use for the SSH session for commands.
  /// </summary>
  public string? SshUsername {
    get => _sshUsername;
    set {
      Configuration.Instance.ServerUsername = value;
      this.RaiseAndSetIfChanged(ref _sshUsername, value);
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
      this.RaiseAndSetIfChanged(ref _sshPassword, value);
      try {
        Configuration.WriteConfiguration();
      }
      catch { }
    }
  }

  /// <summary>
  ///   Restarts the remote machine.
  /// </summary>
  public ICommand OnRestartCommand { get; set; }

  /// <summary>
  ///   Restarts the remote machine.
  /// </summary>
  private async Task OnRestart() {
    using SshClient client = new(_serverAddress!, _sshUsername!, _sshPassword!);
    await client.ConnectAsync(CancellationToken.None);
    string command = "shutdown -r now";
    using SshCommand? ssh = client.RunCommand($"echo {_sshPassword} | sudo -S {command}");
  }

  /// <summary>
  ///   Handles showing the server commands.
  /// </summary>
  private void OnShowCommands() {
    IsDisplayingAdvancedCommands = true;
  }

  /// <summary>
  ///   Pings the web resources.
  /// </summary>
  private async Task PingSite() {
    while (true) {
      WebsiteUp = await SendHeadRequest("https://nullinside.com");
      ApiUp = await SendHeadRequest("https://nullinside.com/api/v1/featureToggle");
      NullUp = await SendHeadRequest("https://nullinside.com/null/v1/database/migration");
      (HttpStatusCode, string?) chat = await SendGetRequest("https://nullinside.com/twitch-bot/v1/bot/chat/timestamp");
      bool chatNotUpdating = false;
      if (HttpStatusCode.OK == chat.Item1 && null != chat.Item2) {
        ChatTimestamp = chat.Item2;
        string parsed = ChatTimestamp.Trim('"');
        if (DateTime.TryParse(parsed, out DateTime time)) {
          string timestamp = time.ToLocalTime().ToString(CultureInfo.InvariantCulture);
          TimeSpan diff = DateTime.Now - time.ToLocalTime();
          timestamp = $"{timestamp} ({diff.Hours}h {diff.Minutes}m {diff.Seconds}s ago)";
          ChatTimestamp = timestamp;
          chatNotUpdating = diff > TimeSpan.FromMinutes(5);
        }
      }
      else {
        ChatTimestamp = null;
        chatNotUpdating = true;
      }

      if ((!WebsiteUp || !ApiUp || !NullUp || chatNotUpdating) && IsMinimized) {
        WindowState = WindowState.Normal;
      }

      await Task.Delay(TimeSpan.FromSeconds(10));
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
      HttpResponseMessage response = await httpClient.SendAsync(request);
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
      HttpResponseMessage response = await httpClient.SendAsync(request);
      return (response.StatusCode, await response.Content.ReadAsStringAsync());
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
        await Task.Delay(TimeSpan.FromSeconds(10));
      }
    }
  }
}