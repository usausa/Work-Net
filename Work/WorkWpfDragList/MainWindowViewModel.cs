namespace WorkWpfDragList;

using System.Collections.ObjectModel;
using System.Windows.Input;
using Smart.Windows.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    public ObservableCollection<string> Items { get; set; }
    public ICommand DropCommand { get; set; }

    public MainWindowViewModel()
    {
        Items = ["Item 1", "Item 2", "Item 3", "Item 4"];
        DropCommand = MakeDelegateCommand<Tuple<string, int>>(OnDrop);
    }

    private void OnDrop(Tuple<string, int> parameter)
    {
        var item = parameter.Item1;
        var index = parameter.Item2;
        Items.Remove(item);
        Items.Insert(index, item);
    }
}
