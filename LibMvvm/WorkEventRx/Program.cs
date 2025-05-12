namespace WorkEventRx;

using System.Diagnostics;
using System.Reactive.Linq;

internal class Program
{
    public static void Main()
    {
        var component = new EventComponent();

        //var subscription = component.OnEvent(x =>
        //{
        //    Debug.WriteLine(x);
        //});

        var subscription = component
            .OnEventAsObservable()
            .Subscribe(x => Debug.WriteLine(x));

        component.Trigger();

        subscription.Dispose();

        component.Trigger();
    }
}

public static class Extensions
{
    public static IObservable<T> AsObservable<T>(this Func<Action<T>, IDisposable> func) =>
        Observable.Create<T>(observer => func(observer.OnNext));
}

public static class EventComponentExtensions
{
    public static IObservable<int> OnEventAsObservable(this EventComponent component) =>
        ((Func<Action<int>, IDisposable>)component.OnEvent).AsObservable();

    //    public static IObservable<int> OnEventAsObservable(this EventComponent component) =>
    //        Observable.Create<int>(observer => component.OnEvent(observer.OnNext));
}

public class EventComponent
{
    private readonly List<Action<int>> handlers = [];

    private int counter;

    public void Trigger()
    {
        counter++;
        foreach (var handler in handlers)
        {
            handler(counter);
        }
    }

    public IDisposable OnEvent(Action<int> callback)
    {
        handlers.Add(callback);
        return new EventComponentSubscribe(this, callback);
    }

    private sealed class EventComponentSubscribe : IDisposable
    {
        private readonly EventComponent parent;

        private readonly Action<int> callback;

        public EventComponentSubscribe(EventComponent parent, Action<int> callback)
        {
            this.parent = parent;
            this.callback = callback;
        }

        public void Dispose()
        {
            parent.handlers.Remove(callback);
        }
    }
}
