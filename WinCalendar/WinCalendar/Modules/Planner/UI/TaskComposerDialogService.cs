using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using WinCalendar.Modules.Planner.Presentation;

namespace WinCalendar.Modules.Planner.UI;

internal static class TaskComposerDialogService
{
    public static async Task ShowAddTaskAsync(
        XamlRoot xamlRoot,
        PlannerStateStore plannerStateStore,
        DateOnly initialDate)
    {
        TaskComposerDialog dialog = new(xamlRoot, initialDate);
        TaskComposerResult? result = await dialog.ShowForResultAsync();
        if (result is null)
            return;

        await plannerStateStore.AddTaskAsync(result.Title, result.Date, result.Time, result.DurationMinutes);
    }
}
