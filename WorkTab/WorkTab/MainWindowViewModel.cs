namespace WorkTab;

using HardwareInfo.Disk;

using WorkTab.Models;

public sealed class MainWindowViewModel
{
    public ObservableCollection<DiskInfoModel> Disks { get; } = new();

    public MainWindowViewModel()
    {
        foreach (var disk in DiskInfo.GetInformation())
        {
            Disks.Add(new DiskInfoModel(disk));
        }
    }
}
