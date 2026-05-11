namespace WorkSkiaGit02;

using System.Diagnostics;
using System.Windows;

using WorkSkiaGit02.Graph;

public partial class MainWindow : Window
{
    private const string RepositoryPath = @"C:\Users\machi\Desktop\Works\Git\docker-gitlab";
    private const int MaxCommits = 500;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            var sw = Stopwatch.StartNew();
            var data = GraphBuilder.Build(RepositoryPath, MaxCommits);
            sw.Stop();

            RowList.ItemsSource = data.Rows;
            HeaderText.Text =
                $"{RepositoryPath}    Commits: {data.Rows.Count}    Lanes: {data.LaneCount}    Build: {sw.ElapsedMilliseconds} ms";
        }
        catch (Exception ex)
        {
            HeaderText.Text = $"Failed to load repository: {ex.Message}";
        }
    }
}
