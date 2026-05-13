namespace WorkSkiaGitMaui.ViewModels;

using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using WorkSkiaGitMaui.Data;
using WorkSkiaGitMaui.Graph;

public sealed class MainPageViewModel : INotifyPropertyChanged
{
    private string _headerText = "Loading...";
    private IReadOnlyList<GraphRow> _rows = [];

    public event PropertyChangedEventHandler? PropertyChanged;

    public string HeaderText
    {
        get => _headerText;
        private set => SetField(ref _headerText, value);
    }

    public IReadOnlyList<GraphRow> Rows
    {
        get => _rows;
        private set => SetField(ref _rows, value);
    }

    public async Task LoadAsync()
    {
        try
        {
            var sw = Stopwatch.StartNew();
            var (commits, refs) = await PseudoRepository.LoadAsync().ConfigureAwait(false);
            var data = GraphBuilder.Build(commits, refs);
            sw.Stop();

            Rows = data.Rows;
            HeaderText = $"Commits: {data.Rows.Count}    Lanes: {data.LaneCount}    Build: {sw.ElapsedMilliseconds} ms";
        }
        catch (Exception ex)
        {
            HeaderText = $"Failed to build graph: {ex.Message}";
        }
    }

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
