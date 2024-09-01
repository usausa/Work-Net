namespace WorkMvvmCanvas;

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

public sealed class ShapeCanvas : Canvas
{
    public static readonly DependencyProperty ShapesProperty = DependencyProperty.Register(
        nameof(Shapes),
        typeof(ObservableCollection<ShapeModel>),
        typeof(ShapeCanvas),
        new PropertyMetadata(null, OnShapeChanged));

    public ObservableCollection<ShapeModel> Shapes
    {
        get => (ObservableCollection<ShapeModel>)GetValue(ShapesProperty);
        set => SetValue(ShapesProperty, value);
    }

    private static void OnShapeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var canvas = (ShapeCanvas)d;

        if (e.OldValue is ObservableCollection<ShapeModel> oldShapes)
        {
            oldShapes.CollectionChanged -= canvas.OnShapesCollectionChanged;
        }

        if (e.NewValue is ObservableCollection<ShapeModel> newShapes)
        {
            newShapes.CollectionChanged += canvas.OnShapesCollectionChanged;
            canvas.UpdateChildren();
        }

    }

    private void OnShapesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        UpdateChildren();
    }

    private void UpdateChildren()
    {
        Children.Clear();

        foreach (var shape in Shapes)
        {
            var rectangle = new Rectangle
            {
                Width = shape.Width * 10,
                Height = shape.Height * 10,
                Fill = new SolidColorBrush(shape.Color)
            };
            SetLeft(rectangle, shape.X * 10);
            SetTop(rectangle, shape.Y * 10);
            Children.Add(rectangle);
        }
    }
}
