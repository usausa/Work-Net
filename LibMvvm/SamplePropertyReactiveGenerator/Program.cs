namespace SamplePropertyReactiveGenerator;

using ReactiveGenerator;

// https://github.com/wieslawsoltes/ReactiveGenerator
internal class Program
{
    public static void Main()
    {
    }
}

[Reactive]
public partial class UserViewModel
{
    // Simple properties - automatically implements INotifyPropertyChanged
    public partial string FirstName { get; set; }
    public partial string LastName { get; set; }

    // Computed property with ObservableAsPropertyHelper
    [ObservableAsProperty]
    public partial string FullName { get; }
}
