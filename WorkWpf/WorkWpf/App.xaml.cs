namespace WorkWpf;

using System;
using System.Windows;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    [STAThread]
    public static void Main()
    {
        App app = new();
        app.InitializeComponent();
        app.Run();
    }
}
