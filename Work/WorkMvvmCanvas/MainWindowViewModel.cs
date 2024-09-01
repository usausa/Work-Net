namespace WorkMvvmCanvas;

using System.Collections.ObjectModel;
using System.Windows.Media;

public class MainWindowViewModel
{
    public ObservableCollection<ShapeModel> Shapes { get; } = new();

    public MainWindowViewModel()
    {
        Shapes.Add(new ShapeModel { X = 1, Y = 1, Width = 10, Height = 10, Color = Color.FromRgb(0xEE, 0x37, 0x6C) });
        Shapes.Add(new ShapeModel { X = 15, Y = 5, Width = 20, Height = 15, Color = Color.FromRgb(0x56, 0x77, 0xCB) });
        Shapes.Add(new ShapeModel { X = 10, Y = 20, Width = 5, Height = 5, Color = Color.FromRgb(0x51, 0xC6, 0xBF) });
        Shapes.Add(new ShapeModel { X = 40, Y = 8, Width = 2, Height = 8, Color = Color.FromRgb(0xEE, 0xB6, 0x11) });
    }
}
