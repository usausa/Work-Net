namespace WorkGauge.ViewModels;

using System.ComponentModel;
using System.Runtime.CompilerServices;

public sealed class MainPageViewModel : INotifyPropertyChanged
{
    private double pressure = 1013;
    private double humidity = 50;
    private double temperature = 22;
    private double windDirection = 90;
    private double speedKmh = 80;
    private double rpm = 3.5;

    public event PropertyChangedEventHandler? PropertyChanged;

    public double Pressure
    {
        get => pressure;
        set => Set(ref pressure, value);
    }

    public double Humidity
    {
        get => humidity;
        set => Set(ref humidity, value);
    }

    public double Temperature
    {
        get => temperature;
        set => Set(ref temperature, value);
    }

    public double WindDirection
    {
        get => windDirection;
        set => Set(ref windDirection, value);
    }

    public double SpeedKmh
    {
        get => speedKmh;
        set => Set(ref speedKmh, value);
    }

    public double Rpm
    {
        get => rpm;
        set => Set(ref rpm, value);
    }

    private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
