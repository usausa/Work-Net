using System.Diagnostics;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace WorkObservableMessenger;

public static class Program
{
    public static void Main()
    {
        var subscribe = ReactiveMediator.Default.Observe<string>()
            .Subscribe(x => Debug.WriteLine($"Received: {x}"));

        ReactiveMediator.Default.Send("Hello, World!");

        var subscribe2 = ReactiveMediator.Default.Observe<string>()
            .Subscribe(x => Debug.WriteLine($"Received2: {x}"));

        ReactiveMediator.Default.Send("Usa");

        subscribe.Dispose();

        ReactiveMediator.Default.Send("Hoge");

        subscribe2.Dispose();

        for (var i = 0; i < 100_0000; i++)
        {
            using var s = ReactiveMediator.Default.Observe<string>().Subscribe(_ => { });
            ReactiveMediator.Default.Send("Usa");
        }

        Debug.WriteLine("*");

        for (var i = 0; i < 100_0000; i++)
        {
            using var s = ReactiveMediator.Default.Observe<string>().Subscribe(_ => { });
            ReactiveMediator.Default.Send("Usa");
        }
    }
}

public interface IReactiveMeditator
{
    IObservable<TMessage> Observe<TMessage>();

    void Send<TMessage>(TMessage message);

    bool HasObservers<TMessage>();
}

public class ReactiveMediator : IReactiveMeditator
{
    public static ReactiveMediator Default { get; } = new();

    private static class SubjectHolder<T>
    {
        public static readonly Subject<T> Subject = new();
    }

    private ReactiveMediator()
    {
    }

    public IObservable<TMessage> Observe<TMessage>()
    {
        var subject = SubjectHolder<TMessage>.Subject;
        return subject.AsObservable();
    }

    public void Send<TMessage>(TMessage message)
    {
        var subject = SubjectHolder<TMessage>.Subject;
        subject.OnNext(message);
    }

    public bool HasObservers<TMessage>()
    {
        var subject = SubjectHolder<TMessage>.Subject;
        return subject.HasObservers;
    }
}
