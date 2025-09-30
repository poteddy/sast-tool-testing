using CommunityToolkit.Mvvm.Input;
using ToolTester.Presentation.Models;

namespace ToolTester.Presentation.PageModels
{
    public interface IProjectTaskPageModel
    {
        IAsyncRelayCommand<ProjectTask> NavigateToTaskCommand { get; }
        bool IsBusy { get; }
    }
}