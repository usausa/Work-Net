using System.Diagnostics;

namespace WorkModel;

using System.ComponentModel;

using Smart.Mvvm;
using Smart.Mvvm.Internal;

internal static class Program
{
    public static void Main()
    {
        TestAdd();
        TestClearByKey();

        TestAddRange();
        TestAddRangeZero();
        TestAddRangeZeroExist();

        TestUpdate();

        TestUpdateRange();
        TestUpdateRangeClear();
    }

    public static void TestUpdateRangeClear()
    {
        using var errorInfo = new ErrorInfo();
        var observer = new EventObserver();
        errorInfo.PropertyChanged += observer.OnChanged;

        errorInfo.AddError("Key1", "Error 1-1");

        // Update(Clear)
        observer.Reset();
        errorInfo.UpdateErrors("Key1", []);
        Debug.Assert(observer.ItemsChanged == 1);
        Debug.Assert(observer.HasErrorChanged == 1);
        Debug.Assert(!errorInfo.HasError);
        Debug.Assert(!errorInfo.GetAllErrors().Any());

        // Update(Clear)
        errorInfo.AddError("Key1", "Error 1-1");
        errorInfo.AddError("Key2", "Error 2-1");
        Debug.Assert(errorInfo.HasError);

        observer.Reset();
        errorInfo.UpdateErrors("Key1", []);
        Debug.Assert(observer.ItemsChanged == 1);
        Debug.Assert(observer.HasErrorChanged == 0);
        Debug.Assert(errorInfo.HasError);
        Debug.Assert(errorInfo.GetAllErrors().Count() == 1);
    }

    public static void TestUpdateRange()
    {
        using var errorInfo = new ErrorInfo();
        var observer = new EventObserver();
        errorInfo.PropertyChanged += observer.OnChanged;

        errorInfo.AddError("Key1", "Error 1-1");

        // Update
        observer.Reset();
        errorInfo.UpdateErrors("Key1", ["Error 1-2", "Error 1-3"]);
        Debug.Assert(observer.ItemsChanged == 1);
        Debug.Assert(observer.HasErrorChanged == 0);
        Debug.Assert(errorInfo.HasError);
        Debug.Assert(errorInfo.GetAllErrors().Count() == 2);

        // Update(Add)
        errorInfo.ClearAllErrors();
        Debug.Assert(!errorInfo.HasError);

        observer.Reset();
        errorInfo.UpdateErrors("Key1", ["Error 1-2", "Error 1-3"]);
        Debug.Assert(observer.ItemsChanged == 1);
        Debug.Assert(observer.HasErrorChanged == 1);
        Debug.Assert(errorInfo.HasError);
        Debug.Assert(errorInfo.GetAllErrors().Count() == 2);
    }

    public static void TestUpdate()
    {
        using var errorInfo = new ErrorInfo();
        var observer = new EventObserver();
        errorInfo.PropertyChanged += observer.OnChanged;

        errorInfo.AddError("Key1", "Error 1-1");

        // Update
        observer.Reset();
        errorInfo.UpdateError("Key1", "Error 1-2");
        Debug.Assert(observer.ItemsChanged == 1);
        Debug.Assert(observer.HasErrorChanged == 0);
        Debug.Assert(errorInfo.HasError);
        Debug.Assert(errorInfo.GetAllErrors().Count() == 1);

        // Update(Add)
        errorInfo.ClearAllErrors();
        Debug.Assert(!errorInfo.HasError);

        observer.Reset();
        errorInfo.UpdateError("Key1", "Error 1-2");
        Debug.Assert(observer.ItemsChanged == 1);
        Debug.Assert(observer.HasErrorChanged == 1);
        Debug.Assert(errorInfo.HasError);
        Debug.Assert(errorInfo.GetAllErrors().Count() == 1);
    }

    private static void TestAddRangeZeroExist()
    {
        using var errorInfo = new ErrorInfo();
        var observer = new EventObserver();
        errorInfo.PropertyChanged += observer.OnChanged;

        errorInfo.AddError("Key1", "Error 1-1");

        // Add
        observer.Reset();
        errorInfo.AddErrors("Key1", []);
        Debug.Assert(observer.ItemsChanged == 0);
        Debug.Assert(observer.HasErrorChanged == 0);
        Debug.Assert(errorInfo.HasError);
        Debug.Assert(errorInfo.GetAllErrors().Count() == 1);
    }

    private static void TestAddRangeZero()
    {
        using var errorInfo = new ErrorInfo();
        var observer = new EventObserver();
        errorInfo.PropertyChanged += observer.OnChanged;

        // Add
        observer.Reset();
        errorInfo.AddErrors("Key1", []);
        Debug.Assert(observer.ItemsChanged == 0);
        Debug.Assert(observer.HasErrorChanged == 0);
        Debug.Assert(!errorInfo.HasError);
        Debug.Assert(!errorInfo.GetAllErrors().Any());
    }

    private static void TestAddRange()
    {
        using var errorInfo = new ErrorInfo();
        var observer = new EventObserver();
        errorInfo.PropertyChanged += observer.OnChanged;

        // Add
        observer.Reset();
        errorInfo.AddErrors("Key1", ["Error 1-1", "Error 1-2"]);
        Debug.Assert(observer.ItemsChanged == 1);
        Debug.Assert(observer.HasErrorChanged == 1);
        Debug.Assert(errorInfo.HasError);
        Debug.Assert(errorInfo.GetAllErrors().Count() == 2);
    }

    private static void TestClearByKey()
    {
        using var errorInfo = new ErrorInfo();
        var observer = new EventObserver();
        errorInfo.PropertyChanged += observer.OnChanged;

        errorInfo.AddError("Key1", "Error 1-1");
        errorInfo.AddError("Key1", "Error 1-2");
        errorInfo.AddError("Key2", "Error 2-1");

        // Clear
        observer.Reset();
        errorInfo.ClearErrors("Key2");
        Debug.Assert(observer.ItemsChanged == 1);
        Debug.Assert(observer.HasErrorChanged == 0);
        Debug.Assert(errorInfo.HasError);
        Debug.Assert(errorInfo.GetAllErrors().Count() == 2);

        // Clear
        observer.Reset();
        errorInfo.ClearErrors("Key1");
        Debug.Assert(observer.ItemsChanged == 1);
        Debug.Assert(observer.HasErrorChanged == 1);
        Debug.Assert(!errorInfo.HasError);
        Debug.Assert(!errorInfo.GetAllErrors().Any());
    }

    private static void TestAdd()
    {
        using var errorInfo = new ErrorInfo();
        var observer = new EventObserver();
        errorInfo.PropertyChanged += observer.OnChanged;

        // Add
        observer.Reset();
        errorInfo.AddError("Key1", "Error 1-1");
        Debug.Assert(observer.ItemsChanged == 1);
        Debug.Assert(observer.HasErrorChanged == 1);
        Debug.Assert(errorInfo.HasError);
        Debug.Assert(errorInfo.GetAllErrors().Count() == 1);

        // Add same
        observer.Reset();
        errorInfo.AddError("Key1", "Error 1-2");
        Debug.Assert(observer.ItemsChanged == 1);
        Debug.Assert(observer.HasErrorChanged == 0);
        Debug.Assert(errorInfo.HasError);
        Debug.Assert(errorInfo.GetAllErrors().Count() == 2);

        // Add other
        observer.Reset();
        errorInfo.AddError("Key2", "Error 2-1");
        Debug.Assert(observer.ItemsChanged == 1);
        Debug.Assert(observer.HasErrorChanged == 0);
        Debug.Assert(errorInfo.HasError);
        Debug.Assert(errorInfo.GetAllErrors().Count() == 3);

        // Clear
        observer.Reset();
        errorInfo.ClearAllErrors();
        Debug.Assert(observer.ItemsChanged == 1);
        Debug.Assert(observer.HasErrorChanged == 1);
        Debug.Assert(!errorInfo.HasError);
        Debug.Assert(!errorInfo.GetAllErrors().Any());
    }

    private static void TestDisposables()
    {
        {
            using var disposables = new Disposables();

            MakeDisposable(() => Console.WriteLine("Disposable 1 disposed")).AddTo(disposables);
            MakeDisposable(() => Console.WriteLine("Disposable 2 disposed")).AddTo(disposables);

            //disposables.Dispose();
        }
    }

    private static IDisposable MakeDisposable(Action action)
    {
        return new DelegateDisposable(action);
    }
}

public class EventObserver
{
    public int ItemsChanged { get; private set; }

    public int HasErrorChanged { get; private set; }

    public void OnChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == "Item[]")
        {
            ItemsChanged++;
        }
        else if (e.PropertyName == "HasError")
        {
            HasErrorChanged++;
        }
    }

    public void Reset()
    {
        ItemsChanged = 0;
        HasErrorChanged = 0;
    }
}

//--------------------------------------------------------------------------------
// ErrorInfo
//--------------------------------------------------------------------------------

public sealed class ErrorInfo : ObservableObject, IDisposable
{
    private const int DefaultCapacity = 16;

    private static readonly PropertyChangedEventArgs ItemsChangedEventArgs = new("Item[]");
    private static readonly PropertyChangedEventArgs HasErrorChangedEventArgs = new(nameof(HasError));

    private Dictionary<string, PooledList<string>>? errors;

    private bool hasError;

    // ReSharper disable once ConvertToAutoProperty
    public bool HasError => hasError;

    public string? this[string key] =>
        (errors is not null) && errors.TryGetValue(key, out var values) && (values.Count > 0) ? values[0] : null;

    public void Dispose()
    {
        if (errors is not null)
        {
            foreach (var kvp in errors)
            {
                kvp.Value.Dispose();
            }

            errors.Clear();
        }
    }

    public bool Contains(string key) =>
        (errors is not null) && errors.TryGetValue(key, out var values) && (values.Count > 0);

    public IEnumerable<string> GetKeys() =>
        hasError ? GetKeysInternal() : [];

    private IEnumerable<string> GetKeysInternal()
    {
        if (errors is not null)
        {
            foreach (var kvp in errors)
            {
                if (kvp.Value.Count == 0)
                {
                    continue;
                }

                yield return kvp.Key;
            }
        }
    }

    public IReadOnlyList<string> GetErrors(string key) =>
        (errors is not null) && errors.TryGetValue(key, out var values) && (values.Count > 0) ? values : [];

    public IEnumerable<string> GetAllErrors() =>
        hasError ? GetAllErrorsInternal() : [];

    private IEnumerable<string> GetAllErrorsInternal()
    {
        if (errors is not null)
        {
            foreach (var kvp in errors)
            {
                if (kvp.Value.Count == 0)
                {
                    continue;
                }

                foreach (var value in kvp.Value)
                {
                    yield return value;
                }
            }
        }
    }

    private PooledList<string> PrepareList(string key)
    {
        errors ??= new Dictionary<string, PooledList<string>>();

        if (!errors.TryGetValue(key, out var values))
        {
            values = new PooledList<string>(DefaultCapacity);
            errors.Add(key, values);
        }

        return values;
    }

    public void AddError(string key, string message)
    {
        var values = PrepareList(key);

        values.Add(message);

        RaisePropertyChanged(ItemsChangedEventArgs);

        var previousError = hasError;
        hasError = true;
        if (previousError != hasError)
        {
            RaisePropertyChanged(HasErrorChangedEventArgs);
        }
    }

    public void AddErrors(string key, IEnumerable<string> messages)
    {
        var values = default(PooledList<string>);
        var added = false;
        foreach (var message in messages)
        {
            if (values is null)
            {
                values = PrepareList(key);
                added = true;
            }

            values.Add(message);
        }

        if (added)
        {
            RaisePropertyChanged(ItemsChangedEventArgs);

            var previousError = hasError;
            hasError = true;
            if (previousError != hasError)
            {
                RaisePropertyChanged(HasErrorChangedEventArgs);
            }
        }
    }

    public void UpdateError(string key, string message)
    {
        var values = PrepareList(key);

        values.Clear();
        values.Add(message);

        RaisePropertyChanged(ItemsChangedEventArgs);

        var previousError = hasError;
        hasError = true;
        if (previousError != hasError)
        {
            RaisePropertyChanged(HasErrorChangedEventArgs);
        }
    }

    public void UpdateErrors(string key, IEnumerable<string> messages)
    {
        var values = default(PooledList<string>);
        var errorExist = false;
        foreach (var message in messages)
        {
            if (values is null)
            {
                values = PrepareList(key);
                values.Clear();
                errorExist = true;
            }

            values.Add(message);
        }

        if (!errorExist && (errors is not null))
        {
            if (errors.TryGetValue(key, out values))
            {
                values.Clear();
            }

            foreach (var kvp in errors)
            {
                if (kvp.Value.Count > 0)
                {
                    errorExist = true;
                    break;
                }
            }
        }

        RaisePropertyChanged(ItemsChangedEventArgs);

        var previousError = hasError;
        hasError = errorExist;
        if (previousError != hasError)
        {
            RaisePropertyChanged(HasErrorChangedEventArgs);
        }
    }

    public void ClearErrors(string key)
    {
        if ((errors is null) || !errors.TryGetValue(key, out var values))
        {
            return;
        }

        values.Clear();

        var errorExist = false;
        foreach (var kvp in errors)
        {
            if (kvp.Value.Count > 0)
            {
                errorExist = true;
                break;
            }
        }

        RaisePropertyChanged(ItemsChangedEventArgs);

        var previousError = hasError;
        hasError = errorExist;
        if (previousError != hasError)
        {
            RaisePropertyChanged(HasErrorChangedEventArgs);
        }
    }

    public void ClearAllErrors()
    {
        if (errors is null)
        {
            return;
        }

        foreach (var kvp in errors)
        {
            kvp.Value.Clear();
        }

        RaisePropertyChanged(ItemsChangedEventArgs);

        var previousError = hasError;
        hasError = false;
        if (previousError != hasError)
        {
            RaisePropertyChanged(HasErrorChangedEventArgs);
        }
    }
}

//--------------------------------------------------------------------------------
// Disposables
//--------------------------------------------------------------------------------

public sealed class DelegateDisposable : IDisposable
{
    private readonly Action? action;

    public DelegateDisposable(Action? action)
    {
        this.action = action;
    }

    public void Dispose()
    {
        action?.Invoke();
    }
}

public sealed class Disposables : IDisposable
{
    private const int DefaultCapacity = 32;

    private PooledList<IDisposable>? disposables;

    public void Dispose()
    {
        if (disposables is not null)
        {
            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < disposables.Count; i++)
            {
                disposables[i].Dispose();
            }

            disposables.Dispose();
            disposables = null;
        }
    }

    public void Add(IDisposable disposable)
    {
        disposables ??= new PooledList<IDisposable>(DefaultCapacity);
        disposables.Add(disposable);
    }
}

public static class DisposablesExtensions
{
    public static T AddTo<T>(this T disposable, Disposables disposables)
        where T : IDisposable
    {
        disposables.Add(disposable);
        return disposable;
    }
}
