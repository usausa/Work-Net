namespace WorkSkiaGit01;

using System.Diagnostics;
using System.Windows;

using SkiaSharp.Views.Desktop;

using WorkSkiaGit01.Graph;

public partial class MainWindow : Window
{
    private const string RepositoryPath = @"C:\Users\machi\Desktop\Works\Git\docker-gitlab";
    private const int MaxCommits = 500;

    private GraphRenderer? renderer;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Surface.PaintSurface += OnPaintSurface;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            var sw = Stopwatch.StartNew();
            var layout = GraphBuilder.Build(RepositoryPath, MaxCommits);
            sw.Stop();

            renderer = new GraphRenderer(layout);
            Surface.Width = renderer.ContentWidth;
            Surface.Height = renderer.ContentHeight;
            HeaderText.Text =
                $"{RepositoryPath}    Commits: {layout.RowCount}    Lanes: {layout.LaneCount}    Build: {sw.ElapsedMilliseconds} ms";
            Surface.InvalidateVisual();
        }
        catch (Exception ex)
        {
            HeaderText.Text = $"Failed to load repository: {ex.Message}";
        }
    }

    private void OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        if (renderer is null)
        {
            e.Surface.Canvas.Clear();
            return;
        }

        renderer.Render(e.Surface.Canvas);
    }
}
