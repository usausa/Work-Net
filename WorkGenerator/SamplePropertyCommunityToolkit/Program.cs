namespace SamplePropertyCommunityToolkit;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using System.Collections.ObjectModel;

internal class Program
{
    public static void Main()
    {
    }
}

internal partial class ViewModel : ObservableObject
{
    [ObservableProperty]
    private partial string Text { get; set; }
}

internal partial class ViewModel2 : ObservableObject
{
    [ObservableProperty]
    public partial ObservableCollection<string> Items { get; set; } = [];
}

internal partial class ViewModel3 : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Rotation))]
    public partial int Count { get; set; }

    public double Rotation => Count % 360;

    [RelayCommand]
    private void Increment()
    {
        Count += 1;
    }

    [RelayCommand]
    private void Clear()
    {
        Count = 0;
    }
}
