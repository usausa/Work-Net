namespace WorkHost2;

using System.Windows;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private readonly MainWindow window;

    public App(MainWindow window)
    {
        InitializeComponent();

        this.window = window;
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        MainWindow = window;
        MainWindow.Show();
    }
}
