﻿using System;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

using Avalonia.Controls;

using Nullinside.Api.Common;

using ReactiveUI;

using SiteMonitor.Models;

namespace SiteMonitor.ViewModels;

/// <summary>
///   The view model for the main UI.
/// </summary>
public class MainWindowViewModel : ViewModelBase {
  private bool _apiUp;
  private bool _isMinimized;
  private bool _nullUp;
  private string? _serverAddress;
  private bool _serverUp;
  private bool _websiteUp;
  private WindowState _windowState;

  /// <summary>
  ///   Initializes a new instance of the <see cref="MainWindowViewModel" /> class.
  /// </summary>
  public MainWindowViewModel() {
    Task.Factory.StartNew(PingServer);
    Task.Factory.StartNew(PingSite);
    ServerAddress = Configuration.Instance.ServerAddress;
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
  ///   Pings the web resources.
  /// </summary>
  private async Task PingSite() {
    while (true) {
      WebsiteUp = await SendHeadRequest("https://nullinside.com");
      ApiUp = await SendHeadRequest("https://nullinside.com/api/v1/featureToggle");
      NullUp = await SendHeadRequest("https://nullinside.com/null/v1/database/migration");
      if ((!WebsiteUp || !ApiUp || !NullUp) && IsMinimized) {
        WindowState = WindowState.Normal;
      }
      
      await Task.Delay(TimeSpan.FromSeconds(10));
    }
  }

  /// <summary>
  ///   Sends a head request to ensure a URL is online.
  /// </summary>
  /// <param name="address"></param>
  /// <returns></returns>
  private async Task<bool> SendHeadRequest(string address) {
    try {
      var handler = new HttpClientHandler();
      handler.AutomaticDecompression = ~DecompressionMethods.None;
      using var httpClient = new HttpClient(handler);
      using var request = new HttpRequestMessage(HttpMethod.Get, address);
      request.Headers.TryAddWithoutValidation("user-agent", Constants.FAKE_USER_AGENT);
      HttpResponseMessage response = await httpClient.SendAsync(request);
      return response.IsSuccessStatusCode;
    }
    catch {
      return false;
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