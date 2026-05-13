namespace WorkSkiaGitMaui;

using WorkSkiaGitMaui.ViewModels;

public partial class MainPage : ContentPage
{
    public MainPage(MainPageViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object? sender, EventArgs e)
    {
        await ((MainPageViewModel)BindingContext).LoadAsync().ConfigureAwait(true);
    }
}
