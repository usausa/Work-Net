namespace WorkValidation;

using System.Collections;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

using Smart.Mvvm;

internal class MainWindowViewModel : ObservableObject, INotifyDataErrorInfo, IDataErrorInfo
{
    public ErrorState Errors { get; } = new();

    [Required(ErrorMessage = "名前は必須です。")]
    [StringLength(50, ErrorMessage = "名前は50文字以内で入力してください。")]
    public string Name
    {
        get => field;
        set
        {
            if (field != value)
            {
                field = value;
                RaisePropertyChanged(nameof(Name));
                ValidateProperty(nameof(Name), value);
            }
        }
    } = default!;

    [Range(0, 120, ErrorMessage = "年齢は0以上120以下である必要があります。")]
    public int Age
    {
        get => field;
        set
        {
            if (field != value)
            {
                field = value;
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
            Errors.UpdateErrors(propertyName, validationResults.Select(vr => vr.ErrorMessage!));
        }
        else
        {
            Errors.ClearError(propertyName);
        }

        ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
    }

    // INotifyDataErrorInfo

    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    public bool HasErrors => Errors.HasError;

    public IEnumerable GetErrors(string? propertyName) =>
        !System.String.IsNullOrEmpty(propertyName) ? Errors.GetValues(propertyName) : [];

    // IDataErrorInfo

    public string this[string columnName] => Errors[columnName] ?? string.Empty;

    public string Error => String.Join(Environment.NewLine, Errors.GetAllErrors());
}
