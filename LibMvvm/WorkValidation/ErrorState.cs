namespace WorkValidation;

using System.ComponentModel;

using Smart.Mvvm;

#pragma warning disable CA1024
public sealed class ErrorState : ObservableObject
{
    private static readonly PropertyChangedEventArgs ItemsChangedEventArgs = new("Item[]");
    private static readonly PropertyChangedEventArgs HasErrorChangedEventArgs = new(nameof(HasError));

    private Dictionary<string, List<string>>? errors;

    public bool HasError => (errors is not null) && (errors.Count > 0);

    public string? this[string key] =>
        (errors is not null) && errors.TryGetValue(key, out var values) ? values[0] : null;

    public bool Contains(string key) =>
        (errors is not null) && errors.ContainsKey(key);

    public IReadOnlyCollection<string> GetKeys() =>
        (errors is not null) ? errors.Keys : [];

    public IReadOnlyList<string> GetValues(string key) =>
        (errors is not null) && errors.TryGetValue(key, out var values) ? values : [];

    public IEnumerable<string> GetAllErrors()
    {
        if (errors is not null)
        {
            foreach (var kvp in errors)
            {
                foreach (var value in kvp.Value)
                {
                    yield return value;
                }
            }
        }
    }

    public void AddError(string key, string message)
    {
        errors ??= new Dictionary<string, List<string>>();
        if (!errors.TryGetValue(key, out var values))
        {
            values = [];
            errors.Add(key, values);
        }

        values.Add(message);
        RaisePropertyChanged(ItemsChangedEventArgs);
        RaisePropertyChanged(HasErrorChangedEventArgs);
    }

    public void AddErrors(string key, IEnumerable<string> messages)
    {
        errors ??= new Dictionary<string, List<string>>();
        if (!errors.TryGetValue(key, out var values))
        {
            values = [];
            errors.Add(key, values);
        }

        values.AddRange(messages);
        RaisePropertyChanged(ItemsChangedEventArgs);
        RaisePropertyChanged(HasErrorChangedEventArgs);
    }

    public void UpdateError(string key, string message)
    {
        errors ??= new Dictionary<string, List<string>>();
        if (!errors.TryGetValue(key, out var values))
        {
            values = [];
            errors.Add(key, values);
        }
        else
        {
            values.Clear();
        }

        values.Add(message);
        RaisePropertyChanged(ItemsChangedEventArgs);
        RaisePropertyChanged(HasErrorChangedEventArgs);
    }

    public void UpdateErrors(string key, IEnumerable<string> messages)
    {
        errors ??= new Dictionary<string, List<string>>();
        if (!errors.TryGetValue(key, out var values))
        {
            values = [];
            errors.Add(key, values);
        }
        else
        {
            values.Clear();
        }

        values.AddRange(messages);
        RaisePropertyChanged(ItemsChangedEventArgs);
        RaisePropertyChanged(HasErrorChangedEventArgs);
    }

    public void ClearErrors(string key)
    {
        if ((errors is not null) && errors.Remove(key))
        {
            RaisePropertyChanged(ItemsChangedEventArgs);
            RaisePropertyChanged(HasErrorChangedEventArgs);
        }
    }

    public void ClearAllErrors()
    {
        if (errors is not null)
        {
            errors.Clear();
            RaisePropertyChanged(ItemsChangedEventArgs);
            RaisePropertyChanged(HasErrorChangedEventArgs);
        }
    }
}
#pragma warning restore CA1024
