using CommunityToolkit.Mvvm.Input;
using MobileApp.Models;

namespace MobileApp.PageModels
{
    public interface IProjectTaskPageModel
    {
        IAsyncRelayCommand<ProjectTask> NavigateToTaskCommand { get; }
        bool IsBusy { get; }
    }
}