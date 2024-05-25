using System.ComponentModel;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia.Threading;
using Nullinside.Api.Common.Desktop;
using ReactiveUI;

namespace AvaloniaApplication1.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private string serverVersion = string.Empty;
    public MainWindowViewModel()
    {
        Task.Factory.StartNew(async () =>
        {
            var version =
                await GitHubUpdateManager.GetLatestVersion("nullinside-development-group", "nullinside-site-monitor");

            if (null == version)
            {
                return;
            }

            Dispatcher.UIThread.Post(() => ServerVersion = version.name ?? string.Empty);
        });
    }
#pragma warning disable CA1822 // Mark members as static
    public string Greeting => Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? string.Empty;
#pragma warning restore CA1822 // Mark members as static
    public string ServerVersion
    {
        get => serverVersion;
        set => this.RaiseAndSetIfChanged(ref serverVersion, value);
    }
}