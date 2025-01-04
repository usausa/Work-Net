namespace SamplePropertyCommunityToolkit;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

// https://zenn.dev/andrewkeepcodin/articles/007-partial-observable-property
// https://zenn.dev/tnagata012/articles/play-with-partialprop--7c638681b71825
internal class Program
{
    public static void Main()
    {
    }
}

internal partial class ViewModel : ObservableObject
{
    [ObservableProperty]
    private string name = default!;

    [RelayCommand]
    private void Greet(string text)
    {
        Console.WriteLine($"Hello {text}!");
    }
}
