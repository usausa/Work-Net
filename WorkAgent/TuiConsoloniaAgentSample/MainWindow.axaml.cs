namespace TuiConsoloniaAgentSample;

using System;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;

internal sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
        {
            // 新規メッセージのたびに最下部へスクロールする。
            viewModel.Messages.CollectionChanged += (_, _) =>
                Dispatcher.UIThread.Post(() => Transcript.Offset = new Vector(0, Transcript.Extent.Height));
        }
    }
}
