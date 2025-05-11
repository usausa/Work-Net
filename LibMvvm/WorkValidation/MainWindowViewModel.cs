using Smart.Mvvm;

namespace WorkValidation;

using Smart.Mvvm.ViewModels;

internal partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string Value { get; set; } = string.Empty;
}
