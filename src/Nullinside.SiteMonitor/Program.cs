using System;
using System.IO;

using Avalonia;
using Avalonia.ReactiveUI;

using log4net;
using log4net.Config;

namespace Nullinside.SiteMonitor;

internal sealed class Program {
  /// <summary>
  ///   The logger.
  /// </summary>
  private static readonly ILog LOG = LogManager.GetLogger(typeof(Program));

  // Initialization code. Don't use any Avalonia, third-party APIs or any
  // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
  // yet and stuff might break.
  [STAThread]
  public static void Main(string[] args) {
#if DEBUG
    XmlConfigurator.Configure(new FileInfo("log4net.debug.config"));
#else
      XmlConfigurator.Configure(new FileInfo("log4net.config"));
#endif

    LOG.Info("Started application");

    AppDomain.CurrentDomain.UnhandledException += (_, exceptArgs) => {
      LOG.Fatal("Unhandled exception", exceptArgs.ExceptionObject as Exception);
    };

    BuildAvaloniaApp()
      .StartWithClassicDesktopLifetime(args);
  }

  // Avalonia configuration, don't remove; also used by visual designer.
  public static AppBuilder BuildAvaloniaApp() {
    return AppBuilder.Configure<App>()
      .UsePlatformDetect()
      .WithInterFont()
      .LogToTrace()
      .UseReactiveUI();
  }
}