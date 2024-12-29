using CommunityToolkit.Mvvm.Input;
using MauiSampleNet9.Models;

namespace MauiSampleNet9.PageModels
{
    public interface IProjectTaskPageModel
    {
        IAsyncRelayCommand<ProjectTask> NavigateToTaskCommand { get; }
        bool IsBusy { get; }
    }
}