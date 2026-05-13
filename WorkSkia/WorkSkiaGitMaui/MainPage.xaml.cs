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
        LoadGraph();
    }

    private void LoadGraph()
    {
        try
        {
            var sw = Stopwatch.StartNew();
            var data = GraphBuilder.Build(PseudoRepository.Commits, PseudoRepository.Refs);
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
