namespace WorkGauge;

using WorkGauge.ViewModels;

public partial class MainPage : ContentPage
{
    public MainPage(MainPageViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;

        WindGauge.LabelFormatter = v => v switch
        {
            0 or 360 => "N",
            90       => "E",
            180      => "S",
            270      => "W",
            _        => string.Empty,
        };
    }
}
