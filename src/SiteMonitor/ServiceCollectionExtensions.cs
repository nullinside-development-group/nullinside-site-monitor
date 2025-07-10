using Microsoft.Extensions.DependencyInjection;

using SiteMonitor.ViewModels;

namespace SiteMonitor;

/// <summary>
///   A wrapper that contains the registered services.
/// </summary>
public static class ServiceCollectionExtensions {
  /// <summary>
  ///   Adds the services used throughout the application.
  /// </summary>
  /// <param name="collection">The services collection to initialize.</param>
  public static void AddCommonServices(this IServiceCollection collection) {
    // View models
    collection.AddTransient<MainWindowViewModel>();
    collection.AddTransient<NewVersionWindowViewModel>();
  }
}