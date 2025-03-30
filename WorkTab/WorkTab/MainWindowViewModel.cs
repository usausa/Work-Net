namespace WorkTab;

using CommunityToolkit.Mvvm.ComponentModel;

public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string Title { get; set; } = "Hello World!";
}
