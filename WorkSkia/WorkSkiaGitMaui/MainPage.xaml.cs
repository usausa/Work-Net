namespace WorkSkiaGitMaui;

using System;
using System.Diagnostics;

using WorkSkiaGitMaui.Data;
using WorkSkiaGitMaui.Graph;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object? sender, EventArgs e)
    {
        try
        {
            var sw = Stopwatch.StartNew();
            var (commits, refs) = await PseudoRepository.LoadAsync().ConfigureAwait(true);
            var data = GraphBuilder.Build(commits, refs);
            sw.Stop();

            RowList.ItemsSource = data.Rows;
            HeaderLabel.Text =
                $"Commits: {data.Rows.Count}    Lanes: {data.LaneCount}    Build: {sw.ElapsedMilliseconds} ms";
        }
        catch (Exception ex)
        {
            HeaderLabel.Text = $"Failed to build graph: {ex.Message}";
        }
    }
}
