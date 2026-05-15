// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace WorkGauge.WinUI;

using Microsoft.UI.Xaml;
using Windows.Graphics;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : MauiWinUIApplication
{
    /// <summary>
    /// Initializes the singleton application object.  This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        this.InitializeComponent();


        var windowWidth = 480;
        var windowHeight = 960;
        Microsoft.Maui.Handlers.WindowHandler.Mapper.AppendToMapping(nameof(IWindow), (handler, view) =>
        {
            var nativeWindow = handler.PlatformView;
            nativeWindow.Activate();
            var windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(nativeWindow);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(windowHandle);
            var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
            appWindow.Resize(new SizeInt32(windowWidth, windowHeight));
        });
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}

