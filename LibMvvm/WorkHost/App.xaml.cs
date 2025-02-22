namespace WorkHost;

using System.Windows;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private readonly MainWindow window;

    public App(ILogger<App> log, MainWindow window, IOptions<Settings> settings)
    {
        InitializeComponent();

        this.window = window;

        log.LogInformation($"App construct. value={settings.Value.Name}, address={settings.Value.Address}");
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        MainWindow = window;
        MainWindow.Show();
    }
}

