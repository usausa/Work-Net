namespace SamplePropertyPropertyChanged;

using PropertyChanged.SourceGenerator;

// https://github.com/canton7/PropertyChanged.SourceGenerator
internal class Program
{
    public static void Main()
    {
    }
}

public partial class MyViewModel
{
    [Notify] private string firstName = default!;
    [Notify] private string lastName = default!;
    public string FullName => $"{FirstName} {LastName}";
}
