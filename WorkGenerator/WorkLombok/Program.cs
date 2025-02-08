namespace WorkLombok;

using Lombok.NET;

internal class Program
{
    static void Main(string[] args)
    {
    }
}

// Constructor, With Method, ToString

[AllArgsConstructor]
[With]
[ToString]
public partial class Person1
{
    private string name;

    private int age;
}

[AllArgsConstructor(MemberType = MemberType.Property, AccessTypes = AccessTypes.Public)]
//[ToString]
public partial class Person2
{
    public string Name { get; set; }

    public int Age { get; set; }
}

// Property

public partial class MyViewModel
{

    [Property]
    private int result;
}

// Observable

[NotifyPropertyChanged]
public partial class CustomViewModel
{
    [Property(PropertyChangeType = PropertyChangeType.PropertyChanged)]
    private int result;
}

// Freezable

[Freezable]
partial class Person3
{
    [Freezable]
    private string name;

    private int age;
}

// AsyncOverloads

[AsyncOverloads]
public partial interface IRepository<T>
{
    T GetById(int id);

    void Save(T entity);
}

// Async

public partial class MyViewModel
{

    [Async]
    public int Square(int i)
    {
        return i * i;
    }
}

// Singleton

[Singleton]
public partial class PersonRepository
{
}

// Lazy

[Lazy]
public partial class HeavyInitialization
{
    private HeavyInitialization()
    {
        Thread.Sleep(1000);
    }
}

// Decorator

[Decorator]
public interface IVehicle
{
    void Drive();
    int GetNumberOfWheels();
}
