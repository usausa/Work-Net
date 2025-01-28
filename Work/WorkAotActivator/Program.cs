namespace WorkAotActivator;

internal class Program
{
    public static void Main()
    {
        var holder = Make(123);
        Console.WriteLine(holder.Value);
    }

    private static IValueHolder<T> Make<T>(T value)
    {
        //var holder = new MyClass<T>();
        //holder.Value = value;
        var type = typeof(MyClass<>).MakeGenericType(typeof(T));
        var holder = (IValueHolder<T>)Activator.CreateInstance(type)!;
        holder.Value = value;
        return holder;
    }
}

public interface IValueHolder<T>
{
    T Value { get; set; }
}

public class MyClass<T> : IValueHolder<T>
{
    public T Value { get; set; } = default!;
}
