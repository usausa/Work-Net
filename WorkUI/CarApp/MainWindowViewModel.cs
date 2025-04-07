namespace CarApp;

using CommunityToolkit.Mvvm.ComponentModel;

public sealed partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string Message { get; set; } = "Hello world.";
}
