using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using WinCalendar.Core.Abstractions;
using WinCalendar.Core.Time;

namespace WinCalendar.Modules.Planner.Presentation;

public sealed class PlannerAgendaDayViewModel : ObservableObject
{
    private bool _isExpanded;

    public PlannerAgendaDayViewModel(
        DateOnly date,
        IEnumerable<PlannerTaskViewModel> tasks,
        bool isExpanded)
    {
        Date = date;
        _isExpanded = isExpanded;
        Tasks = [];

        foreach (PlannerTaskViewModel task in tasks)
            Tasks.Add(task);
    }

    public DateOnly Date { get; }

    public ObservableCollection<PlannerTaskViewModel> Tasks { get; }

    public string HeaderText => PlannerDateTimeFormatter.FormatShortDay(Date);

    public string CompactHeaderText => $"{CompactHeaderPrefix} {PlannerDateTimeFormatter.FormatAgendaHeader(Date)}";

    public string CompactHeaderDetails => $"{HeaderText} - {SummaryText}";

    public string SummaryText => Tasks.Count == 0 ? "No tasks" : $"{Tasks.Count} task{(Tasks.Count == 1 ? string.Empty : "s")}";

    public string EmptyTasksText => "No tasks scheduled.";

    public string CompactHeaderPrefix => _isExpanded ? "|>" : "->";

    public Visibility ContentVisibility => _isExpanded ? Visibility.Visible : Visibility.Collapsed;

    public Visibility TasksVisibility => Tasks.Count == 0 ? Visibility.Collapsed : Visibility.Visible;

    public Visibility EmptyTasksVisibility => Tasks.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (!SetProperty(ref _isExpanded, value))
                return;

            OnPropertyChanged(nameof(ContentVisibility));
            OnPropertyChanged(nameof(CompactHeaderText));
            OnPropertyChanged(nameof(CompactHeaderPrefix));
        }
    }
}
