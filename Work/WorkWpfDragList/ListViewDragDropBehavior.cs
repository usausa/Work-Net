namespace WorkWpfDragList;

using Microsoft.Xaml.Behaviors;

using System;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows;

public class ListViewDragDropBehavior : Behavior<ListView>
{
    private Point startPoint;

    public static readonly DependencyProperty DropCommandProperty = DependencyProperty.Register(
        nameof(DropCommand),
        typeof(ICommand),
        typeof(ListViewDragDropBehavior),
        new PropertyMetadata(null));

    public ICommand? DropCommand
    {
        get => (ICommand)GetValue(DropCommandProperty);
        set => SetValue(DropCommandProperty, value);
    }

    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.PreviewMouseLeftButtonDown += OnPreviewMouseLeftButtonDown;
        AssociatedObject.PreviewMouseMove += OnPreviewMouseMove;
        AssociatedObject.Drop += OnDrop;
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        AssociatedObject.PreviewMouseLeftButtonDown -= OnPreviewMouseLeftButtonDown;
        AssociatedObject.PreviewMouseMove -= OnPreviewMouseMove;
        AssociatedObject.Drop -= OnDrop;
    }

    private void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        startPoint = e.GetPosition(null);
    }

    private void OnPreviewMouseMove(object sender, MouseEventArgs e)
    {
        var listView = (ListView)sender;
        var mousePos = e.GetPosition(null);
        var diff = startPoint - mousePos;

        if ((e.LeftButton == MouseButtonState.Pressed) &&
            ((Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance) ||
             (Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)))
        {
            var listViewItem = FindAncestor<ListViewItem>((DependencyObject)e.OriginalSource);
            if (listViewItem == null)
                return;

            var item = (string)listView.ItemContainerGenerator.ItemFromContainer(listViewItem);
            var dragData = new DataObject("myFormat", item);

            DragDrop.DoDragDrop(listViewItem, dragData, DragDropEffects.Move);
        }
    }

    private void OnDrop(object sender, DragEventArgs e)
    {
        var listView = (ListView)sender;
        if (e.Data.GetDataPresent("myFormat"))
        {
            var item = e.Data.GetData("myFormat") as string;
            if (item == null)
            {
                return;
            }

            var listViewItem = FindAncestor<ListViewItem>((DependencyObject)e.OriginalSource);
            if (listViewItem == null)
            {
                return;
            }

            var target = (string)listView.ItemContainerGenerator.ItemFromContainer(listViewItem);
            var index = listView.Items.IndexOf(target);

            var parameter = new Tuple<string, int>(item, index);
            if ((DropCommand != null) && DropCommand.CanExecute(parameter))
            {
                DropCommand.Execute(parameter);
            }
        }
    }

    private static T? FindAncestor<T>(DependencyObject? current)
        where T : DependencyObject
    {
        while (current != null)
        {
            if (current is T t)
            {
                return t;
            }

            current = VisualTreeHelper.GetParent(current);
        }
        return null;
    }
}
