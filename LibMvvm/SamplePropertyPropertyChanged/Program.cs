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
    [Notify] private string _firstName;
    [Notify] private string _lastName;
    public string FullName => $"{FirstName} {LastName}";
}
