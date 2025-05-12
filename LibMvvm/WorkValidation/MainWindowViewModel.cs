using System.Collections;
using Smart.Mvvm;

namespace WorkValidation;

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

internal partial class MainWindowViewModel : ObservableObject, INotifyDataErrorInfo
{
    private string name;

    private int age;

    private readonly Dictionary<string, List<string>> errors = new();

    [Required(ErrorMessage = "名前は必須です。")]
    [StringLength(50, ErrorMessage = "名前は50文字以内で入力してください。")]
    public string Name
    {
        get => name;
        set
        {
            if (name != value)
            {
                name = value;
                RaisePropertyChanged(nameof(Name));
                ValidateProperty(nameof(Name), value);
            }
        }
    }

    [Range(0, 120, ErrorMessage = "年齢は0以上120以下である必要があります。")]
    public int Age
    {
        get => age;
        set
        {
            if (age != value)
            {
                age = value;
                RaisePropertyChanged(nameof(Age));
                ValidateProperty(nameof(Age), value);
            }
        }
    }

    private void ValidateProperty(string propertyName, object value)
    {
        var validationContext = new ValidationContext(this) { MemberName = propertyName };
        var validationResults = new List<ValidationResult>();

        if (!Validator.TryValidateProperty(value, validationContext, validationResults))
        {
            errors[propertyName] = validationResults.Select(vr => vr.ErrorMessage!).ToList();
        }
        else
        {
            errors.Remove(propertyName);
        }

        ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
    }

    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    public bool HasErrors =>
        errors.Any();

    public IEnumerable GetErrors(string? propertyName) =>
        !System.String.IsNullOrEmpty(propertyName) && errors.TryGetValue(propertyName, out var values) ? values : [];
}
