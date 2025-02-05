namespace WorkMessenger;

public sealed class Messanger<T>
{
    public static Messanger<T> Default { get; } = new();

    private ISubscriber<T>[] subscribers = new ISubscriber<T>[4];
}

public interface ISubscriber<in T>
{
    void Execute(T parameter);

    ValueTask ExecuteAsync(T parameter);
}

internal sealed class ActionSubscriber<T>(Action<T> action) : ISubscriber<T>
{
    public void Execute(T parameter) => action(parameter);

    public ValueTask ExecuteAsync(T parameter)
    {
        action(parameter);
        return ValueTask.CompletedTask;
    }
}

internal sealed class TaskSubscriber<T>(Func<T, Task> func) : ISubscriber<T>
{
    // ReSharper disable once AsyncVoidMethod
    public async void Execute(T parameter) => await func(parameter);

    public async ValueTask ExecuteAsync(T parameter) => await func(parameter);
}

internal sealed class ValueTaskSubscriber<T>(Func<T, ValueTask> func) : ISubscriber<T>
{
    // ReSharper disable once AsyncVoidMethod
    public async void Execute(T parameter) => await func(parameter);

    public async ValueTask ExecuteAsync(T parameter) => await func(parameter);
}
